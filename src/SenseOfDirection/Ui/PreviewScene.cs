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
    /// <item>The stage is a full 1920x1080 rect scaled down into the panel's
    /// frame, rather than a small canvas in its own right. Font sizes, edge
    /// margins and the compass's pixel width are all absolute, so a stage that
    /// was literally 1000px wide would render everything ~2x too big relative to
    /// a real screen. At a logical 1920x1080 the preview is a faithful
    /// miniature.</item>
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

        private RectTransform _stage;
        private Camera _camera;
        private GameObject _cameraObject;
        private IndicatorManager _indicators;
        private CompassManager _compass;

        private readonly List<PlayerLabel> _playerLabels = new List<PlayerLabel>();
        private readonly List<ItemPingWidget> _itemPings = new List<ItemPingWidget>();
        private readonly List<ItemEntry> _itemEntries = new List<ItemEntry>();
        private CampfireWidget _campfire;
        private PingWidget _ping;

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
                "MAYA", u: 0.347f, v: 0.528f, meters: 18f,
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

            /// <summary>Off the right edge, alone: the uncrowded case, next to the deliberately crowded left one.</summary>
            internal static readonly PlayerSpec PlayerOffRight = new PlayerSpec(
                "SAM", u: 1.26f, v: 0.53f, meters: 240f,
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

            /// <summary>The vanilla ping hand baked into the screenshot: the 3D marker is the game's, the distance label and off-screen arrow around it are ours.</summary>
            internal const float PingU = 0.795f;
            internal const float PingV = 0.383f;
            internal const float PingMeters = 24f;

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
            // The frame is the window you look through; the stage behind it is a
            // full-size 1920x1080 screen scaled to fit. The frame masks anything
            // the stage draws outside it - which includes edge-clamped indicators
            // sitting exactly on the stage's border.
            //
            // A stencil Mask with a rounded sprite rather than a RectMask2D: it
            // gives the preview the same rounded corners as the panel it sits in,
            // which a rect mask can't do.
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

            var stageGo = new GameObject("Stage", typeof(RectTransform));
            scene._stage = (RectTransform)stageGo.transform;
            scene._stage.SetParent(frame, false);

            // Centre-anchored, like the mod's own live overlay canvas. This is not
            // cosmetic: IndicatorManager positions every widget by anchoredPosition
            // measured from the canvas *centre*, and a RectTransform created in code
            // defaults to a bottom-left anchor - which silently shoved every label
            // half a screen down and left.
            scene._stage.anchorMin = new Vector2(0.5f, 0.5f);
            scene._stage.anchorMax = new Vector2(0.5f, 0.5f);
            scene._stage.pivot = new Vector2(0.5f, 0.5f);
            scene._stage.anchoredPosition = Vector2.zero;
            scene._stage.sizeDelta = StageSize;
            scene._stage.localScale = Vector3.one * (frameSize.x / StageSize.x);

            scene.BuildBackground();
            scene.BuildCamera();

            scene._indicators = IndicatorManager.CreateDetached(scene._stage, scene._camera);
            scene._compass = CompassManager.CreateDetached(scene._stage, scene._camera, () => scene._indicators.Anchors);

            scene.BuildWidgets();
            return scene;
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

            Sprite background = LoadBackground();
            if (background != null)
            {
                image.sprite = background;
            }
            else
            {
                // The menu is still perfectly usable without the screenshot - the
                // indicators just float over a flat backdrop instead of a scene.
                image.color = new Color(0.10f, 0.13f, 0.20f);
            }
        }

        /// <summary>The screenshot, embedded in the DLL (see the .csproj) - no loose asset to ship or lose.</summary>
        private static Sprite LoadBackground()
        {
            using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SenseOfDirection.Icons.preview-background.jpg");
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

        private void OnDestroy()
        {
            // The camera is a root object of our own making (see BuildCamera), so
            // nothing else will clean it up with the menu.
            if (_cameraObject != null)
            {
                Destroy(_cameraObject);
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
            float depth = meters / Mathf.Max(0.0001f, CharacterStats.unitsToMeters);

            float halfHeight = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * depth;
            float halfWidth = halfHeight * _camera.aspect;

            Transform cam = _camera.transform;
            return cam.position
                   + cam.forward * depth
                   + cam.right * ((u - 0.5f) * 2f * halfWidth)
                   + cam.up * ((v - 0.5f) * 2f * halfHeight);
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

        private void BuildPing(PluginConfig cfg)
        {
            Vector3 world = WorldPoint(Cast.PingU, Cast.PingV, Cast.PingMeters);

            // Colored as the visible teammate's ping, matching the mod's own rule
            // that a ping wears its pinging player's character color.
            Color color = Cast.PlayerOnScreen.Color;
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
            // Cast.Luggage* comment - that's the point of it being here.
            AddItemPing("Luggage", 1, Cast.LuggageU, Cast.LuggageV, Cast.LuggageMeters, null, cfg);
        }

        private void AddItemPing(string baseName, int count, float u, float v, float meters, Sprite icon, PluginConfig cfg)
        {
            Vector3 world = WorldPoint(u, v, meters);
            Color color = Cast.PlayerOnScreen.Color;

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

            foreach (ItemEntry entry in _itemEntries)
            {
                string label = CurrentLabel(entry);
                bool showName = cfg.ItemPingNameMode.Value != ItemPingNameMode.Never && !string.IsNullOrEmpty(label);
                entry.Widget.Refresh(label, entry.Meters, showName, cfg.ShowItemPingDistance.Value, CurrentIcon(entry));
            }
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
