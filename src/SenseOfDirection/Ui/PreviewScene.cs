using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SenseOfDirection.CampfireIndicator;
using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using SenseOfDirection.Pings;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// The live preview at the top of <see cref="PreviewMenu"/>: a still
    /// screenshot of a real run with this mod's actual HUD drawn over it,
    /// reacting to every setting as it's changed.
    ///
    /// It is not a mock-up. The widgets are the same classes the game uses
    /// (<see cref="PlayerLabel"/>, <see cref="PingWidget"/>,
    /// <see cref="ItemPingWidget"/>, <see cref="CampfireWidget"/>), positioned by
    /// the same <see cref="IndicatorManager"/>/<see cref="CompassManager"/> code,
    /// reading the same <see cref="PluginConfig"/> - so what you see here is what
    /// the HUD will do, including edge clamping, the off-screen arrows, the
    /// on/off-screen transition, and the anti-overlap nudging. The only thing
    /// swapped out is the camera: a disabled, never-rendering
    /// <see cref="Camera"/> stands in for the player's, and the tracked "world"
    /// is a handful of points placed *behind* the screenshot so they project
    /// onto the right pixels of it.
    ///
    /// Two consequences of that trick are worth knowing:
    ///
    /// <list type="bullet">
    /// <item>The stage is a full 1920x1080 rect shown scaled down inside the
    /// panel's frame, rather than a small canvas in its own right. Font sizes,
    /// edge margins and the compass's pixel width are all absolute, so a stage
    /// that was literally 1000px wide would render everything ~2x too big
    /// relative to a real screen. At a logical 1920x1080 the preview is a
    /// faithful miniature - and, since the stage really is screen-sized, the
    /// magnifier (<see cref="BuildLoupe"/>) can show any patch of it at exactly
    /// the size it will be in game.</item>
    /// <item>Anchors sit at world distances of
    /// <c>meters / CharacterStats.unitsToMeters</c>, i.e. in real world units,
    /// because the shared code converts back the same way. The distances on the
    /// labels are therefore the ones written in <see cref="Cast"/> below.</item>
    /// </list>
    /// </summary>
    internal class PreviewScene : MonoBehaviour
    {
        /// <summary>Matches the real 1920x1080 reference the mod's own canvases use, so everything in the preview is sized exactly as it would be on a real screen.</summary>
        private static readonly Vector2 StageSize = new Vector2(1920f, 1080f);

        /// <summary>Roughly PEAK's own default view - only used to decide how the scene's world points spread out behind the screenshot, so it needn't match the shot's own FOV exactly.</summary>
        private const float FieldOfView = 66f;

        /// <summary>Matches the settings panel's own rounding, so the two read as one menu.</summary>
        private const float FrameCornerRadius = 18f;

        /// <summary>
        /// The magnifier's window, in the menu's own design pixels - and therefore
        /// also, exactly, the patch of the 1920x1080 stage it shows: the whole
        /// point of it is that its contents are at 1:1 scale, so one design pixel
        /// of lens is one design pixel of stage. See <see cref="UpdateLoupe"/>.
        /// </summary>
        private static readonly Vector2 LoupeSize = new Vector2(300f, 300f);

        private const float LoupeCornerRadius = 14f;
        private const float LoupeBorderThickness = 5f;

        /// <summary>Short enough that the lens feels attached to the cursor rather than chasing it, long enough that crossing the frame's edge isn't a hard pop.</summary>
        private const float LoupeFadeDuration = 0.12f;

        /// <summary>
        /// The stage's off-screen home. Anywhere the game's own world isn't, since
        /// the render camera (<see cref="BuildStageCanvas"/>) points at this spot and
        /// would otherwise have PEAK's terrain in the shot behind the preview.
        /// </summary>
        private static readonly Vector3 StageWorldOrigin = new Vector3(0f, 100000f, 0f);

        private RectTransform _frame;
        private Vector2 _frameSize;
        private RectTransform _stage;

        /// <summary>The stage's own canvas/camera/target - the preview is rendered here and only *displayed* in the menu. See <see cref="BuildStageCanvas"/>.</summary>
        private GameObject _stageCanvasObject;
        private Camera _renderCamera;
        private RenderTexture _renderTexture;

        private RawImage _surface;
        private RectTransform _loupeRoot;
        private RawImage _loupeContent;
        private CanvasGroup _loupeGroup;
        private float _loupeAlpha;

        private Camera _camera;
        private GameObject _cameraObject;
        private IndicatorManager _indicators;
        private CompassManager _compass;

        private readonly List<PlayerLabel> _playerLabels = new List<PlayerLabel>();
        private readonly List<ItemPingWidget> _itemPings = new List<ItemPingWidget>();
        private readonly List<ItemEntry> _itemEntries = new List<ItemEntry>();
        private CampfireWidget _campfire;
        private PingWidget _ping;

        /// <summary>The live 3D hand the ping's widgets are drawn around - see <see cref="PreviewPingMarker"/>. Null if the game couldn't supply the prefab, in which case the preview simply has no hand in it.</summary>
        private PreviewPingMarker _pingMarker;

        /// <summary>
        /// The screenshot. Shown twice, and only ever one of them at a time: as the
        /// stage's own <c>Background</c> image, and (when there's a hand to render)
        /// hung behind that hand in its own world, where the two get composited into
        /// one opaque picture that is then laid over the first. See
        /// <see cref="PreviewPingMarker.BuildBackdrop"/> for why the hand can't just
        /// be blended over the UI copy.
        /// </summary>
        private Sprite _background;

        /// <summary>The structural config values the scene is currently built for - see <see cref="RebuildIfNeeded"/>.</summary>
        private StructuralState _builtFor;

        /// <summary>
        /// The cast of the preview, in screenshot-relative coordinates: (u, v) is
        /// a position in the shot itself (0,0 = bottom-left, 1,1 = top-right), and
        /// anything outside 0..1 is deliberately *off* the shot, which is exactly
        /// how the off-screen (edge-clamped, arrowed) indicators get demonstrated
        /// without needing to appear in the image at all.
        ///
        /// Two of the players are placed at nearly the same bearing just off the
        /// left edge on purpose: their labels collide there, which is what makes
        /// General/enable-label-overlap-avoidance visibly do something.
        /// </summary>
        private static class Cast
        {
            // Every (u, v) below is measured off the actual screenshot: the
            // teammate's head, the campfire, the vanilla ping hand, and the props
            // on the ground. The shot was framed with clear space above each of
            // them, so a label lands on open sky/sand rather than on top of
            // whatever it's naming.

            /// <summary>
            /// The teammate standing under the tree, left of centre. The color is
            /// sampled straight off their skin in the screenshot itself (#57449A) -
            /// their label, their ping and their compass marker all have to be the
            /// character color the picture actually shows, or the preview is lying
            /// about what use-character-color does.
            /// </summary>
            internal static readonly PlayerSpec PlayerOnScreen = new PlayerSpec(
                "MAYA", u: 0.347f, v: 0.585f, meters: 18f,
                color: new Color(87f / 255f, 68f / 255f, 154f / 255f), isHost: true, isDead: false, isUnconscious: false);

            // The two off the left edge sit at almost the same bearing on purpose:
            // their labels land on top of each other there, which is the whole
            // demonstration of General/enable-label-overlap-avoidance. Nothing of
            // them needs to be in the image - an off-screen indicator never shows
            // the thing itself, only an arrow towards it.
            internal static readonly PlayerSpec PlayerOffLeftA = new PlayerSpec(
                "OLLIE", u: -0.22f, v: 0.52f, meters: 84f,
                color: new Color(0.93f, 0.60f, 0.23f), isHost: false, isDead: false, isUnconscious: true);

            internal static readonly PlayerSpec PlayerOffLeftB = new PlayerSpec(
                "RIVER", u: -0.30f, v: 0.47f, meters: 156f,
                color: new Color(0.72f, 0.47f, 0.85f), isHost: false, isDead: true, isUnconscious: false);

            /// <summary>
            /// Off the right edge, alone: the uncrowded case, next to the
            /// deliberately crowded left one. Placed slightly *below* screen
            /// centre (not slightly above, as an earlier version had it) -
            /// this preview is framed at an unlit campfire, about as high a
            /// spot as exists, so a player this far off at roughly the same
            /// height reads as being below, never above, the local camera.
            /// </summary>
            internal static readonly PlayerSpec PlayerOffRight = new PlayerSpec(
                "SAM", u: 1.26f, v: 0.47f, meters: 240f,
                color: new Color(0.45f, 0.80f, 0.40f), isHost: false, isDead: false, isUnconscious: false);

            // The three off-screen distances above are deliberately spread far
            // apart (84 / 156 / 240m): Player-Labels/max-distance-meters bottoms
            // out at 50m, so with everyone bunched together at similar ranges that
            // slider would either hide all of them at once or none of them. Spread
            // out, dragging it down culls them one at a time, which is what the
            // setting actually does in a run.

            /// <summary>The campfire with the smoke plume, mid-right.</summary>
            internal const float CampfireU = 0.631f;
            internal const float CampfireV = 0.428f;
            internal const float CampfireMeters = 42f;

            /// <summary>
            /// The ping. The hand there is the game's own <c>PointPing</c>, rendered
            /// live (<see cref="PreviewPingMarker"/>) rather than baked into the
            /// screenshot; the distance label and off-screen arrow around it are ours.
            /// Sits a little left of where the baked-in hand used to: that one was
            /// drawn from its fingertip, this one grows out of its bottom-right corner,
            /// so its body hangs to the left of the anchor rather than around it.
            /// </summary>
            internal const float PingU = 0.785f;

            /// <summary>
            /// Sits low against the hand's own base rather than centred on its
            /// body - the mod's own distance label always renders a fixed 22px
            /// below its tracked point (see <see cref="Pings.PingWidget"/>), which
            /// is a small offset relative to how tall the hand is in the shot, so
            /// getting the label to actually read as sitting at the hand's foot
            /// (rather than floating over its middle) means anchoring low, not
            /// just nudging slightly.
            /// </summary>
            internal const float PingV = 0.300f;
            internal const float PingMeters = 24f;

            /// <summary>
            /// The ping hand's own color, sampled off the screenshot itself - it's
            /// orange there regardless of who threw it, so the ping's label/
            /// highlight color has to be its own constant rather than reusing
            /// <see cref="PlayerOnScreen"/>'s purple (which would otherwise make it
            /// look like the mod mismatched the ping to the wrong player's color).
            /// </summary>
            internal static readonly Color PingColor = new Color(0.95f, 0.50f, 0.12f);

            /// <summary>The pair of coconuts - the grouping demo (one "2x COCONUT" highlight, or two separate ones).</summary>
            internal const float CoconutU = 0.528f;
            internal const float CoconutV = 0.283f;
            internal const float CoconutMeters = 9f;

            internal const float CoconutBU = 0.599f;
            internal const float CoconutBV = 0.289f;
            internal const float CoconutBMeters = 10f;

            /// <summary>The backpack on the right - an Item, so it has a real in-game icon.</summary>
            internal const float BackpackU = 0.888f;
            internal const float BackpackV = 0.414f;
            internal const float BackpackMeters = 22f;

            /// <summary>
            /// The luggage bottom-left. Its point isn't that it's another item -
            /// it's that luggage has no icon anywhere in the game, so with
            /// use-native-icons on it visibly keeps the mod's own generic icon
            /// while the coconuts/backpack next to it swap to their real art.
            /// That's exactly what the setting does, shown rather than described.
            /// </summary>
            internal const float LuggageU = 0.113f;
            internal const float LuggageV = 0.272f;
            internal const float LuggageMeters = 14f;

            // Deliberately not used: the bandage in the bottom-right corner. That
            // corner is where edge-clamped off-screen indicators sit, and an item
            // highlight there would just fight them for the same pixels.
        }

        internal struct PlayerSpec
        {
            internal readonly string Name;
            internal readonly float U;
            internal readonly float V;
            internal readonly float Meters;
            internal readonly Color Color;
            internal readonly bool IsHost;
            internal readonly bool IsDead;
            internal readonly bool IsUnconscious;

            internal PlayerSpec(string name, float u, float v, float meters, Color color, bool isHost, bool isDead, bool isUnconscious)
            {
                Name = name;
                U = u;
                V = v;
                Meters = meters;
                Color = color;
                IsHost = isHost;
                IsDead = isDead;
                IsUnconscious = isUnconscious;
            }
        }

        /// <summary>One previewed item-ping highlight: the widget plus what it's meant to say.</summary>
        private class ItemEntry
        {
            internal ItemPingWidget Widget;
            internal string BaseName;
            internal int Count;
            internal float Meters;

            /// <summary>The item's real in-game icon, when the game has one for it - what <c>use-native-icons</c> switches to.</summary>
            internal Sprite NativeIcon;
        }

        /// <summary>
        /// The settings that change the scene's *shape* rather than just its
        /// appearance, and so need widgets rebuilt rather than refreshed: whether
        /// the two coconuts are one grouped highlight or two, and whether the
        /// ping/item widgets were built with an off-screen arrow (which is decided
        /// once, when the widget is bound).
        /// </summary>
        private struct StructuralState : IEquatable<StructuralState>
        {
            internal bool Grouping;
            internal bool PingArrow;
            internal bool ItemPingArrow;

            internal static StructuralState Current()
            {
                PluginConfig cfg = Plugin.Instance.Cfg;
                return new StructuralState
                {
                    Grouping = cfg.EnableItemPingGrouping.Value,
                    PingArrow = cfg.EnablePingOffScreenIndicator.Value,
                    ItemPingArrow = cfg.EnableItemPingOffScreenIndicator.Value,
                };
            }

            public bool Equals(StructuralState other) =>
                Grouping == other.Grouping && PingArrow == other.PingArrow && ItemPingArrow == other.ItemPingArrow;
        }

        internal static PreviewScene Create(RectTransform parent, Vector2 frameSize, Vector2 anchoredPosition)
        {
            // The frame is the window you look through. What it shows is not the
            // stage itself but a *render* of it (see BuildStageCanvas), because the
            // magnifier needs to draw the same scene twice at two different scales -
            // once shrunk to fit the frame, once at 1:1 inside the lens - and a UI
            // subtree can only exist in one place at one size. A RenderTexture can be
            // sampled as many times as we like.
            //
            // A stencil Mask with a rounded sprite rather than a RectMask2D: it
            // gives the preview the same rounded corners as the panel it sits in,
            // which a rect mask can't do. It also clips the lens, so the lens can
            // never spill out over the panel.
            var frameGo = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image), typeof(Mask));
            var frame = (RectTransform)frameGo.transform;
            frame.SetParent(parent, false);
            frame.sizeDelta = frameSize;
            frame.anchoredPosition = anchoredPosition;

            var frameImage = frameGo.GetComponent<Image>();
            frameImage.sprite = PanelChrome.MaskSprite(128, FrameCornerRadius);
            frameImage.type = Image.Type.Sliced;
            frameGo.GetComponent<Mask>().showMaskGraphic = false;

            var scene = frameGo.AddComponent<PreviewScene>();
            scene._frame = frame;
            scene._frameSize = frameSize;

            scene.BuildStageCanvas();
            scene.BuildBackground();
            scene.BuildCamera();

            // Between the screenshot and every widget, in that order - which is the
            // order the stage's children are created in, so this is just where it
            // falls. In game the hand is in the world and the HUD is over it; here
            // the hand is one layer of the picture, and the HUD is over that.
            scene.BuildPingMarker();

            scene._indicators = IndicatorManager.CreateDetached(scene._stage, scene._camera);
            scene._compass = CompassManager.CreateDetached(scene._stage, scene._camera, () => scene._indicators.Anchors);

            scene.BuildWidgets();

            scene.BuildSurface();
            scene.BuildLoupe();

            // The stage lives outside the menu's hierarchy, so it doesn't get
            // switched off with it - OnEnable/OnDisable do that by hand, and this is
            // the initial sync. See SyncStageActive.
            scene.SyncStageActive();
            return scene;
        }

        /// <summary>
        /// The stage and the camera that renders it, both parked far away from
        /// PEAK's own world.
        ///
        /// The stage used to be a child of the frame, scaled down to fit it. It
        /// can't be any more: the magnifier has to show the same widgets at 1:1
        /// while the frame still shows them shrunk, and no UI object can be in two
        /// places at two sizes. So the stage renders itself, once, into a texture,
        /// and both the frame and the lens are just views of that texture - the
        /// frame showing all of it, the lens a 1:1 crop.
        ///
        /// A world-space canvas with a dedicated orthographic camera, rather than a
        /// screen-space one: only a camera can write to a RenderTexture, and an
        /// overlay canvas is drawn straight to the backbuffer where nothing can
        /// sample it. The camera's frustum is a thin box around the canvas and
        /// nothing else - hence <see cref="StageWorldOrigin"/>, which just has to be
        /// somewhere the game's map isn't, or the terrain would render into the
        /// preview behind the screenshot.
        /// </summary>
        private void BuildStageCanvas()
        {
            _stageCanvasObject = new GameObject("SoD.PreviewStage");
            DontDestroyOnLoad(_stageCanvasObject);
            _stageCanvasObject.transform.position = StageWorldOrigin;

            var canvas = _stageCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = (RectTransform)_stageCanvasObject.transform;
            canvasRect.sizeDelta = StageSize;

            var stageGo = new GameObject("Stage", typeof(RectTransform));
            _stage = (RectTransform)stageGo.transform;
            _stage.SetParent(canvasRect, false);

            // Centre-anchored, like the mod's own live overlay canvas. This is not
            // cosmetic: IndicatorManager positions every widget by anchoredPosition
            // measured from the canvas *centre*, and a RectTransform created in code
            // defaults to a bottom-left anchor - which silently shoved every label
            // half a screen down and left.
            _stage.anchorMin = new Vector2(0.5f, 0.5f);
            _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.anchoredPosition = Vector2.zero;
            _stage.sizeDelta = StageSize;

            var cameraGo = new GameObject("SoD.PreviewStageCamera");
            cameraGo.transform.SetParent(_stageCanvasObject.transform, false);

            // Square in front of the canvas, looking back at it. One world unit is
            // one stage pixel (the canvas is left at scale 1), so a half-height of
            // 540 frames exactly the 1080 tall stage.
            cameraGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            cameraGo.transform.localRotation = Quaternion.identity;

            _renderCamera = cameraGo.AddComponent<Camera>();
            _renderCamera.orthographic = true;
            _renderCamera.orthographicSize = StageSize.y * 0.5f;
            _renderCamera.aspect = StageSize.x / StageSize.y;
            _renderCamera.nearClipPlane = 0.1f;
            _renderCamera.farClipPlane = 20f;
            _renderCamera.clearFlags = CameraClearFlags.SolidColor;

            // Opaque, not transparent: alpha-blended UI rendered onto a transparent
            // clear leaves the *alpha* channel of every antialiased edge wrong, and
            // the frame shows this texture over the panel, where that would read as
            // a halo. The screenshot covers the whole stage anyway, so there's
            // nothing for a transparent background to be useful for.
            _renderCamera.backgroundColor = Color.black;

            canvas.worldCamera = _renderCamera;

            EnsureRenderTexture();
        }

        /// <summary>
        /// The stage's render target, sized off the real screen rather than fixed at
        /// 1920x1080: the lens shows a 1:1 crop of it, so its resolution is exactly
        /// how sharp the magnified view is. Matching the screen's own height means a
        /// stage pixel and a screen pixel are the same size, which is the whole
        /// claim the magnifier makes.
        /// </summary>
        private void EnsureRenderTexture()
        {
            int height = Mathf.Clamp(Screen.height, 540, 2160);
            int width = Mathf.RoundToInt(height * StageSize.x / StageSize.y);

            // The hand renders into its own texture at the same resolution - it's a
            // layer of the same picture, so it has to be as sharp as the rest of it
            // under the magnifier.
            if (_pingMarker != null)
            {
                _pingMarker.EnsureTexture(width, height);
            }

            if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
            {
                return;
            }

            if (_renderTexture != null)
            {
                _renderCamera.targetTexture = null;
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            // A depth buffer, even though nothing here is 3D: a world-space canvas's
            // UI shaders ZTest against it, unlike an overlay canvas's.
            _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "SoD.PreviewStage",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            _renderCamera.targetTexture = _renderTexture;

            if (_surface != null)
            {
                _surface.texture = _renderTexture;
            }
            if (_loupeContent != null)
            {
                _loupeContent.texture = _renderTexture;
            }
        }

        /// <summary>The frame's own view of the stage: all of it, shrunk to fit.</summary>
        private void BuildSurface()
        {
            var go = new GameObject("Surface", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_frame, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _surface = go.GetComponent<RawImage>();
            _surface.texture = _renderTexture;

            // The one raycast target in the preview - it's what "is the cursor over
            // the preview" is answered against, and it also keeps a click meant for
            // the picture from falling through to the dim behind the panel.
            _surface.raycastTarget = true;
        }

        /// <summary>
        /// The magnifier: a second view of the same texture, cropped to the patch
        /// under the cursor and drawn at 1:1.
        ///
        /// The frame shows a 1920x1080 stage in ~1040 design pixels, so everything in
        /// it - a label's font, the compass's tick spacing, the gap an off-screen
        /// arrow keeps from the edge - is a little over half the size it will
        /// actually be in game. That's fine for judging a layout and useless for
        /// judging whether a font size is readable, which is exactly what several of
        /// these settings are for. The lens answers that: what's inside it is the
        /// size it will be on the player's own screen.
        /// </summary>
        private void BuildLoupe()
        {
            var rootGo = new GameObject("Loupe", typeof(RectTransform), typeof(CanvasGroup));
            _loupeRoot = (RectTransform)rootGo.transform;
            _loupeRoot.SetParent(_frame, false);
            CentreAnchor(_loupeRoot, LoupeSize);

            _loupeGroup = rootGo.GetComponent<CanvasGroup>();
            _loupeGroup.alpha = 0f;

            // Never a raycast target, any of it: the lens sits directly under the
            // cursor by construction, so anything in it that swallowed a raycast
            // would take the hover away from the surface underneath and the lens
            // would flicker itself out of existence.
            _loupeGroup.blocksRaycasts = false;
            _loupeGroup.interactable = false;

            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            var maskRect = (RectTransform)maskGo.transform;
            maskRect.SetParent(_loupeRoot, false);
            CentreAnchor(maskRect, LoupeSize);

            var maskImage = maskGo.GetComponent<Image>();
            maskImage.sprite = PanelChrome.MaskSprite(64, LoupeCornerRadius);
            maskImage.type = Image.Type.Sliced;
            maskImage.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(RawImage));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.SetParent(maskRect, false);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            _loupeContent = contentGo.GetComponent<RawImage>();
            _loupeContent.texture = _renderTexture;
            _loupeContent.raycastTarget = false;

            // Drawn over the content, last, rather than peeking out from behind it.
            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            var ringRect = (RectTransform)ringGo.transform;
            ringRect.SetParent(_loupeRoot, false);
            CentreAnchor(ringRect, LoupeSize);

            var ringImage = ringGo.GetComponent<Image>();
            ringImage.sprite = RingSprite();
            ringImage.type = Image.Type.Sliced;
            ringImage.raycastTarget = false;
        }

        /// <summary>
        /// The lens's outline, as one hollow rounded-rect sprite.
        ///
        /// The obvious way to draw it - a solid rounded plate behind the lens, a
        /// little bigger on every side, with its margin showing as a ring - is the
        /// way this was first built, and it's subtly wrong at the corners: the ring's
        /// thickness there is the gap between two arcs of *different* radii, which is
        /// narrower than the straight sections' thickness however carefully the two
        /// radii are picked. Measuring thickness inward from the shape's own boundary
        /// instead - the distance field below, one texture, no nesting - makes it the
        /// same all the way round by construction, corners included.
        /// </summary>
        private static Sprite RingSprite()
        {
            if (_ringSprite != null)
            {
                return _ringSprite;
            }

            const int Size = 64;

            var texture = new Texture2D(Size, Size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color color = PanelChrome.PanelBorderColor;
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, LoupeCornerRadius, Size - LoupeCornerRadius);
                    float cy = Mathf.Clamp(fy, LoupeCornerRadius, Size - LoupeCornerRadius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));

                    // Inside the rounded rect, minus the same shape shrunk by the
                    // ring's thickness: what's left is the ring, and its thickness is
                    // measured perpendicular to the boundary everywhere.
                    float outer = Mathf.Clamp01(LoupeCornerRadius - dist + 0.5f);
                    float inner = Mathf.Clamp01(LoupeCornerRadius - dist - LoupeBorderThickness + 0.5f);

                    pixels[y * Size + x] = new Color(color.r, color.g, color.b, outer - inner);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var slice = new Vector4(LoupeCornerRadius, LoupeCornerRadius, LoupeCornerRadius, LoupeCornerRadius);
            _ringSprite = Sprite.Create(
                texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, slice);

            return _ringSprite;
        }

        private static Sprite _ringSprite;

        /// <summary>
        /// Anchors and pivots a rect to its parent's centre, which is the space the
        /// lens is positioned in (see <see cref="UpdateLoupe"/>, which measures the
        /// cursor from the frame's centre). Spelled out rather than left to the
        /// RectTransform's own defaults - the stage was once shoved half a screen
        /// down and left by exactly that assumption. See <see cref="BuildStageCanvas"/>.
        /// </summary>
        private static void CentreAnchor(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private void BuildBackground()
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_stage, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;

            _background = LoadBackground();
            if (_background != null)
            {
                image.sprite = _background;
            }
            else
            {
                // The menu is still perfectly usable without the screenshot - the
                // indicators just float over a flat backdrop instead of a scene.
                image.color = new Color(0.10f, 0.13f, 0.20f);
            }
        }

        /// <summary>
        /// The screenshot, embedded in the DLL (see the .csproj) - no loose asset to
        /// ship or lose. It is the scene with *no ping hand in it*: the hand in the
        /// preview is a real one, rendered live (<see cref="PreviewPingMarker"/>),
        /// and a second one baked into the picture would sit next to it doing
        /// nothing while every ping setting moved the other.
        /// </summary>
        private static Sprite LoadBackground()
        {
            using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SenseOfDirection.Icons.preview-backdrop.jpg");
            if (stream == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(memory.ToArray()))
            {
                return null;
            }
            texture.wrapMode = TextureWrapMode.Clamp;

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// A real <see cref="Camera"/>, but disabled and culling nothing: it never
        /// renders a single pixel. It exists purely so
        /// <see cref="ScreenSpaceTracker"/> and the compass can project against
        /// something - <c>WorldToViewportPoint</c> works off the camera's
        /// projection, which is valid whether or not it's enabled.
        ///
        /// Deliberately a root object, NOT a child of this scene's own transform.
        /// That transform is part of the UI hierarchy, so parenting the camera to
        /// it drags the camera around with the canvas as the menu lays itself out -
        /// and since the scene's world points are computed once, against the camera's
        /// pose at build time, a camera that then moves projects every one of them
        /// somewhere else. That's what put every on-screen label in the bottom-left
        /// corner, and why toggling a setting appeared to fix it: rebuilding
        /// recomputed the points against wherever the camera had drifted to.
        /// A root object at the origin can't drift.
        /// </summary>
        private void BuildCamera()
        {
            var go = new GameObject("SoD.PreviewCamera");
            DontDestroyOnLoad(go);
            _cameraObject = go;

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.cullingMask = 0;
            _camera.fieldOfView = FieldOfView;
            _camera.aspect = StageSize.x / StageSize.y;
            _camera.nearClipPlane = 0.05f;
        }

        private void OnEnable() => SyncStageActive();

        private void OnDisable() => SyncStageActive();

        /// <summary>
        /// The stage is deliberately outside the menu's own hierarchy (it has to be,
        /// to be rendered by a camera), which means closing the menu no longer
        /// switches it off. Left alone it would go on rendering a 1080p texture and
        /// running the detached indicator/compass managers' per-frame work for a
        /// menu nobody is looking at. Follows this component's own enabled state
        /// instead, which is exactly the menu's.
        /// </summary>
        private void SyncStageActive()
        {
            if (_stageCanvasObject == null)
            {
                return;
            }

            _stageCanvasObject.SetActive(isActiveAndEnabled);
        }

        private void OnDestroy()
        {
            // Both of these are root objects of our own making (see BuildCamera /
            // BuildStageCanvas), so nothing else will clean them up with the menu.
            if (_cameraObject != null)
            {
                Destroy(_cameraObject);
            }

            if (_stageCanvasObject != null)
            {
                Destroy(_stageCanvasObject);
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        /// <summary>
        /// Turns a position in the screenshot into a world point that projects
        /// back onto exactly that position. Straightforward inverse perspective:
        /// step <paramref name="meters"/> along the camera's forward axis, then
        /// out along its right/up axes by however far the frustum has spread at
        /// that depth.
        /// </summary>
        private Vector3 WorldPoint(float u, float v, float meters)
        {
            // The shared code multiplies distances back up by unitsToMeters, so
            // the point has to be placed in world units for the labels to read the
            // meters actually asked for here.
            //
            // Placed along the ray towards (u, v) at exactly that many world units
            // from the camera - not "depth along forward, then offset sideways",
            // which was the original approach and only puts the point at the
            // right *distance* for a point dead-centre (u=v=0.5). Off-axis (any
            // off-screen anchor, which is the whole point of placing one outside
            // 0..1) that earlier version's sideways offset added extra length on
            // top of the forward depth, so the point ended up farther from the
            // camera than `meters` actually said - which is exactly why the
            // compass (which measures real Vector3.Distance to this point) used to
            // report a bigger number than the off-screen label (which was just
            // told `meters` directly and printed it verbatim), and why a point
            // barely off-centre vertically could read as implausibly far above/
            // below the camera once its distance ballooned. The ray direction
            // alone decides where a point projects on screen, so tracking along
            // it doesn't change *where* it lands - only that its distance from the
            // camera now actually matches the number it's meant to represent.
            float tanHalfFovY = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanHalfFovX = tanHalfFovY * _camera.aspect;

            float dx = (u - 0.5f) * 2f * tanHalfFovX;
            float dy = (v - 0.5f) * 2f * tanHalfFovY;

            Transform cam = _camera.transform;
            Vector3 direction = (cam.forward + cam.right * dx + cam.up * dy).normalized;

            float distanceWorldUnits = meters / Mathf.Max(0.0001f, CharacterStats.unitsToMeters);
            return cam.position + direction * distanceWorldUnits;
        }

        private void BuildWidgets()
        {
            _builtFor = StructuralState.Current();
            PluginConfig cfg = Plugin.Instance.Cfg;

            BuildPlayer(Cast.PlayerOnScreen);
            BuildPlayer(Cast.PlayerOffLeftA);
            BuildPlayer(Cast.PlayerOffLeftB);
            BuildPlayer(Cast.PlayerOffRight);

            BuildCampfire();
            BuildPing(cfg);
            BuildItemPings(cfg);
        }

        private void BuildPlayer(PlayerSpec spec)
        {
            Vector3 world = WorldPoint(spec.U, spec.V, spec.Meters);
            PlayerLabel label = PlayerLabel.Create(() => world, _stage);

            IndicatorAnchor anchor = label.Anchor;
            anchor.CompassKind = CompassMarkerKind.Player;
            anchor.GetPlacement = () => Plugin.Instance.Cfg.PlayerLabelPlacement.Value;
            anchor.GetCompassColor = () => CompassColor(spec.Color);
            anchor.GetCompassLabel = () => spec.Name;
            anchor.GetIsDead = () => spec.IsDead;
            anchor.GetIsUnconscious = () => spec.IsUnconscious;

            // Deliberately always active, exactly as in game, where the anchor only
            // asks whether the character exists. Hiding a label is not done by
            // switching its anchor off - that would pop it out instantly - but by
            // driving its alpha to 0 and letting the label's own CanvasGroup fade
            // there. See RefreshPlayer.
            anchor.IsActive = () => true;
            anchor.IsCompassVisible = () => IsPlayerVisible(spec);

            _indicators.RegisterAnchor(anchor);
            _playerLabels.Add(label);
        }

        /// <summary>Whether the labels are toggled on, in <see cref="LabelDisplayMode.Toggle"/>. Starts on, unlike the real controller, so the tab isn't blank the moment it's opened.</summary>
        private bool _toggleVisible = true;

        private bool _toggleKeyWasDown;

        /// <summary>When the <see cref="LabelDisplayMode.Hold"/> grace period runs out - see <see cref="ComputeLabelsVisible"/>.</summary>
        private float _holdReleaseUntil;

        private bool _labelsVisible = true;

        /// <summary>
        /// The display mode, live in the preview. This is
        /// <c>PlayerLabelController.ComputeLabelsVisible</c> reimplemented against
        /// the same config and the same key, so that pressing that key here does
        /// what it does in game: Toggle flips the labels on and off, and Hold keeps
        /// them up only while the key is down - plus <c>hold-shown-duration</c>
        /// afterwards, a timer set on every held frame so a quick tap is covered by
        /// it too.
        ///
        /// It is a reimplementation rather than a call into the controller because
        /// the controller's copy of this state belongs to the real HUD: driving the
        /// preview off it would make opening the menu and tapping the key toggle the
        /// player's actual labels underneath. The preview keeps its own.
        ///
        /// Two deliberate differences from the original, both because this runs in a
        /// menu rather than in play:
        /// - <see cref="Time.unscaledTime"/>, not <c>Time.time</c>. The menu freezes
        ///   the game, so a scaled clock would leave the Hold grace period frozen
        ///   with it and the labels stuck up forever.
        /// - Keys are ignored while a rebind row is waiting for one, or the very
        ///   press that rebinds this key would also fire it.
        ///
        /// The edge detection is done by hand off <c>GetKey</c> for the same reason
        /// the controller does it - see the comment there about <c>GetKeyDown</c>
        /// silently missing presses while another key is held.
        /// </summary>
        private bool ComputeLabelsVisible(PluginConfig cfg)
        {
            if (!cfg.EnablePlayerLabels.Value)
            {
                return false;
            }

            bool keyDownNow = !KeyRebindControl.IsCapturing && Input.GetKey(cfg.PlayerLabelToggleKey.Value);

            switch (cfg.PlayerLabelDisplayMode.Value)
            {
                case LabelDisplayMode.AlwaysOn:
                    return true;

                case LabelDisplayMode.Toggle:
                    if (keyDownNow && !_toggleKeyWasDown)
                    {
                        _toggleVisible = !_toggleVisible;
                    }
                    _toggleKeyWasDown = keyDownNow;
                    return _toggleVisible;

                case LabelDisplayMode.Hold:
                    if (keyDownNow)
                    {
                        _holdReleaseUntil = Time.unscaledTime + cfg.HoldShownDuration.Value;
                        return true;
                    }
                    return Time.unscaledTime < _holdReleaseUntil;

                default:
                    return false;
            }
        }

        private bool IsPlayerVisible(PlayerSpec spec)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            return _labelsVisible && spec.Meters <= cfg.PlayerLabelMaxDistanceMeters.Value;
        }

        private void BuildCampfire()
        {
            Vector3 world = WorldPoint(Cast.CampfireU, Cast.CampfireV, Cast.CampfireMeters);
            _campfire = CampfireWidget.Create(() => world, _stage);

            IndicatorAnchor anchor = _campfire.Anchor;
            anchor.CompassKind = CompassMarkerKind.Campfire;
            anchor.GetPlacement = () => Plugin.Instance.Cfg.CampfirePlacement.Value;
            anchor.IsActive = () => Plugin.Instance.Cfg.EnableCampfireIndicator.Value;
            anchor.IsCompassVisible = () => Plugin.Instance.Cfg.EnableCampfireIndicator.Value;

            _indicators.RegisterAnchor(anchor);
        }

        /// <summary>
        /// The hand itself. Pointed from the visible teammate's own head - it's the
        /// only character in the shot, so it's the one whose ping this reads as -
        /// which is what decides the way the hand tilts (see
        /// <see cref="PreviewPingMarker"/>).
        /// </summary>
        private void BuildPingMarker()
        {
            _pingMarker = PreviewPingMarker.TryCreate(
                _stage, _camera,
                WorldPoint(Cast.PingU, Cast.PingV, Cast.PingMeters),
                WorldPoint(Cast.PlayerOnScreen.U, Cast.PlayerOnScreen.V, Cast.PlayerOnScreen.Meters),
                Cast.PingColor, _background);
        }

        private void BuildPing(PluginConfig cfg)
        {
            Vector3 world = WorldPoint(Cast.PingU, Cast.PingV, Cast.PingMeters);

            // Cast.PingColor, not the visible teammate's own color: the hand baked
            // into the screenshot is orange regardless of who's pinging, so the
            // label has to match the hand actually shown rather than whichever
            // player's color the preview otherwise demonstrates elsewhere.
            Color color = Cast.PingColor;
            _ping = PingWidget.CreateDetached(_stage, () => world, color, cfg.EnablePingOffScreenIndicator.Value);

            IndicatorAnchor anchor = _ping.Anchor;
            anchor.CompassKind = CompassMarkerKind.Ping;
            anchor.GetPlacement = () => Plugin.Instance.Cfg.PingPlacement.Value;
            anchor.GetCompassColor = () => CompassColor(color);

            _indicators.RegisterAnchor(anchor);
        }

        private void BuildItemPings(PluginConfig cfg)
        {
            Sprite coconutIcon = FindItemIcon("Coconut");
            Sprite backpackIcon = FindItemIcon("Backpack");

            if (cfg.EnableItemPingGrouping.Value)
            {
                // Grouped: the two coconuts fold into one "2x COCONUT" highlight
                // at the pair's centre, exactly as ItemPingHighlight would.
                float centreU = (Cast.CoconutU + Cast.CoconutBU) * 0.5f;
                float centreV = (Cast.CoconutV + Cast.CoconutBV) * 0.5f;
                AddItemPing("Coconut", 2, centreU, centreV, Cast.CoconutMeters, coconutIcon, cfg);
            }
            else
            {
                AddItemPing("Coconut", 1, Cast.CoconutU, Cast.CoconutV, Cast.CoconutMeters, coconutIcon, cfg);
                AddItemPing("Coconut", 1, Cast.CoconutBU, Cast.CoconutBV, Cast.CoconutBMeters, coconutIcon, cfg);
            }

            AddItemPing("Backpack", 1, Cast.BackpackU, Cast.BackpackV, Cast.BackpackMeters, backpackIcon, cfg);

            // Null icon, always: luggage has none in the game at all. See the
            // Cast.Luggage* comment - that's the point of it being here. Colored
            // to match the ping hand (Cast.PingColor), not the visible teammate -
            // it's the same orange the hand itself uses in the screenshot.
            AddItemPing("Luggage", 1, Cast.LuggageU, Cast.LuggageV, Cast.LuggageMeters, null, cfg, Cast.PingColor);
        }

        private void AddItemPing(string baseName, int count, float u, float v, float meters, Sprite icon, PluginConfig cfg, Color? colorOverride = null)
        {
            Vector3 world = WorldPoint(u, v, meters);
            Color color = colorOverride ?? Cast.PlayerOnScreen.Color;

            ItemPingWidget widget = ItemPingWidget.CreateDetached(_stage, () => world, color, cfg.EnableItemPingOffScreenIndicator.Value);

            var entry = new ItemEntry
            {
                Widget = widget,
                BaseName = baseName,
                Count = count,
                Meters = meters,
                NativeIcon = icon,
            };

            IndicatorAnchor anchor = widget.Anchor;
            anchor.CompassKind = CompassMarkerKind.ItemPing;
            anchor.GetPlacement = () => Plugin.Instance.Cfg.ItemPingPlacement.Value;
            anchor.GetCompassColor = () => CompassColor(color);
            anchor.GetCompassLabel = () => CurrentLabel(entry);
            anchor.GetCompassIcon = () => CurrentIcon(entry);
            anchor.IsActive = () => Plugin.Instance.Cfg.EnableItemPings.Value;
            anchor.IsCompassVisible = () => Plugin.Instance.Cfg.EnableItemPings.Value;

            _indicators.RegisterAnchor(anchor);
            _itemPings.Add(widget);
            _itemEntries.Add(entry);
        }

        /// <summary>The label's own name color follows use-character-color, same as a real label; the compass marker keeps the player's color regardless (it has no other way to say whose it is).</summary>
        private static Color CompassColor(Color playerColor) =>
            Plugin.Instance.Cfg.UseCharacterColor.Value ? playerColor : NativeAssets.DefaultTextColor;

        /// <summary>Mirrors <c>ItemPingHighlight</c>'s own naming: a grouped highlight keeps its count even when the name is hidden behind its icon.</summary>
        private static string CurrentLabel(ItemEntry entry)
        {
            string upper = entry.BaseName.ToUpperInvariant();
            string countOnly = entry.Count > 1 ? $"{entry.Count}x" : null;
            string full = entry.Count > 1 ? $"{countOnly} {upper}" : upper;

            ItemPingNameMode mode = Plugin.Instance.Cfg.ItemPingNameMode.Value;
            return mode == ItemPingNameMode.HideWhenIconShown && CurrentIcon(entry) != null ? countOnly : full;
        }

        private static Sprite CurrentIcon(ItemEntry entry) =>
            Plugin.Instance.Cfg.UseNativeItemPingIcons.Value ? entry.NativeIcon : null;

        /// <summary>
        /// The pinged item's real in-game icon, pulled off any loaded
        /// <see cref="Item"/> prefab of that kind - the same art
        /// <c>use-native-icons</c> shows in a real item ping. Null if that item
        /// isn't loaded in this session, in which case the preview simply falls
        /// back to the mod's own generic icon, exactly as it would for a
        /// creature or a piece of luggage.
        /// </summary>
        private static Sprite FindItemIcon(string itemName)
        {
            foreach (Item item in Resources.FindObjectsOfTypeAll<Item>())
            {
                if (item == null || item.UIData == null)
                {
                    continue;
                }

                if (item.name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return NativeIconCache.ForItem(item);
                }
            }

            return null;
        }

        private void Update()
        {
            NativeAssets.TryFindAll();
            EnsureRenderTexture();
            UpdateLoupe();
            RebuildIfNeeded();

            PluginConfig cfg = Plugin.Instance.Cfg;

            // Once per frame, not once per label: the Toggle edge detection and the
            // Hold timer are one piece of state, and asking four times would flip
            // the toggle four times on a single press.
            _labelsVisible = ComputeLabelsVisible(cfg);

            RefreshPlayer(_playerLabels[0], Cast.PlayerOnScreen, cfg);
            RefreshPlayer(_playerLabels[1], Cast.PlayerOffLeftA, cfg);
            RefreshPlayer(_playerLabels[2], Cast.PlayerOffLeftB, cfg);
            RefreshPlayer(_playerLabels[3], Cast.PlayerOffRight, cfg);

            _campfire.Refresh(Cast.CampfireMeters, cfg.ShowCampfireDistance.Value);
            _ping.Refresh(Cast.PingMeters, cfg.ShowPingDistanceLabel.Value);

            if (_pingMarker != null)
            {
                _pingMarker.Refresh(cfg);
            }

            foreach (ItemEntry entry in _itemEntries)
            {
                string label = CurrentLabel(entry);
                bool showName = cfg.ItemPingNameMode.Value != ItemPingNameMode.Never && !string.IsNullOrEmpty(label);
                entry.Widget.Refresh(label, entry.Meters, showName, cfg.ShowItemPingDistance.Value, CurrentIcon(entry));
            }
        }

        /// <summary>
        /// Tracks the lens to the cursor and crops it to the patch of stage under it.
        ///
        /// The crop is where the 1:1 claim is actually made, and it's made by doing
        /// nothing clever: the lens is <see cref="LoupeSize"/> design pixels of the
        /// menu, and it shows exactly that many pixels of a stage that is itself a
        /// 1920x1080 screen. Both are scaled to the player's real resolution by the
        /// same canvas scaler, so what lands on the monitor is the size the HUD will
        /// be on it.
        ///
        /// Both the lens and its crop are clamped, and to different bounds on
        /// purpose: the lens stays wholly inside the frame (a half-clipped lens
        /// reads as a bug), and the crop stays wholly inside the stage (sampling
        /// past the edge would smear the border pixels across the lens). In the
        /// interior - which is nearly all of it - neither clamp is doing anything
        /// and what's under the cursor is dead centre in the lens. In the last few
        /// pixels of an edge the two part company slightly, and the lens holds
        /// still while showing the corner it's over, which is what you want when
        /// what you're inspecting *is* the edge - an edge-clamped off-screen
        /// indicator, say.
        /// </summary>
        private void UpdateLoupe()
        {
            if (_loupeRoot == null)
            {
                return;
            }

            // Null camera: the menu's canvas is a screen-space overlay, where screen
            // points and canvas points are the same space.
            Vector2 mouse = Input.mousePosition;
            bool hovered = RectTransformUtility.ScreenPointToLocalPointInRectangle(_frame, mouse, null, out Vector2 local)
                && _frame.rect.Contains(local);

            if (hovered)
            {
                Vector2 uv = new Vector2(local.x / _frameSize.x, local.y / _frameSize.y) + Vector2.one * 0.5f;
                Vector2 crop = new Vector2(LoupeSize.x / StageSize.x, LoupeSize.y / StageSize.y);
                Vector2 origin = uv - crop * 0.5f;

                _loupeContent.uvRect = new Rect(
                    Mathf.Clamp(origin.x, 0f, 1f - crop.x),
                    Mathf.Clamp(origin.y, 0f, 1f - crop.y),
                    crop.x, crop.y);

                // The hand is clickable, and clicking it throws the ping again -
                // the ripple is a one-second thing, so a preview that only showed it
                // once, when the menu opened, would be a preview of a setting nobody
                // ever sees fire. The stage's uv *is* the camera's viewport, so it
                // can be hit-tested against the hand as it stands right now, at
                // whatever size the scale settings currently give it.
                if (_pingMarker != null && !KeyRebindControl.IsCapturing
                    && Input.GetMouseButtonDown(0) && _pingMarker.HitTest(uv))
                {
                    _pingMarker.EmitRipple();
                }

                // Right up against the frame's own edge - no inset for the ring. The
                // lens is at its most useful exactly here, over an edge-clamped
                // off-screen indicator, so this is the last place to hold it back
                // from.
                Vector2 limit = (_frameSize - LoupeSize) * 0.5f;
                _loupeRoot.anchoredPosition = new Vector2(
                    Mathf.Clamp(local.x, -limit.x, limit.x),
                    Mathf.Clamp(local.y, -limit.y, limit.y));
            }

            // Unscaled: the menu freezes the game, and a fade on a frozen clock never
            // finishes.
            float step = LoupeFadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / LoupeFadeDuration;
            _loupeAlpha = Mathf.MoveTowards(_loupeAlpha, hovered ? 1f : 0f, step);
            _loupeGroup.alpha = _loupeAlpha;
        }

        private void RefreshPlayer(PlayerLabel label, PlayerSpec spec, PluginConfig cfg)
        {
            Color nameColor = cfg.UseCharacterColor.Value ? spec.Color : NativeAssets.DefaultTextColor;

            // The one part of the real ComputeTargetAlpha that has no meaning in a
            // still preview is the look-at test - nobody is looking anywhere. What's
            // left is exactly what it is in game: the master switch, the display
            // mode, and the max distance, expressed as an alpha the label fades
            // towards rather than a switch that pops it in and out.
            float targetAlpha = IsPlayerVisible(spec) ? 1f : 0f;

            label.Refresh(
                spec.Name, spec.Meters, spec.IsHost, spec.IsDead, spec.IsUnconscious,
                nameColor, cfg.PlayerLabelNameFontSize.Value, cfg.PlayerLabelDistanceFontSize.Value,
                targetAlpha,
                showDistance: cfg.ShowPlayerLabelDistance.Value,
                showBadges: cfg.ShowStatusBadges.Value);
        }

        /// <summary>
        /// A handful of settings are baked into a widget when it's built (its
        /// off-screen arrow) or decide how many widgets there even are (grouping),
        /// so they can't be picked up by a per-frame refresh. Toggling one of
        /// those rebuilds the ping/item-ping half of the scene - cheap, and only
        /// on an actual change.
        /// </summary>
        private void RebuildIfNeeded()
        {
            StructuralState current = StructuralState.Current();
            if (current.Equals(_builtFor))
            {
                return;
            }
            _builtFor = current;

            _indicators.UnregisterAnchor(_ping.Anchor);
            foreach (ItemPingWidget widget in _itemPings)
            {
                _indicators.UnregisterAnchor(widget.Anchor);
            }
            _itemPings.Clear();
            _itemEntries.Clear();

            PluginConfig cfg = Plugin.Instance.Cfg;
            BuildPing(cfg);
            BuildItemPings(cfg);
        }
    }
}
