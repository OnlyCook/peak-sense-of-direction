using System;
using System.Collections.Generic;
using System.Text;
using pworld.Scripts.Extensions;
using SenseOfDirection.Pings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// The ping marker in the preview: the game's own <c>PointPing</c> hand,
    /// really instantiated and really rendered, rather than the one that used to
    /// be baked into the screenshot.
    ///
    /// It has to be a live object because three settings are about nothing but
    /// how big that hand is drawn - <c>Pings/enable-scaling</c>,
    /// <c>Pings/scale-multiplier</c> and <c>Pings/enable-ripple</c> - and a
    /// picture of a hand can't answer any of them. So the preview grows the same
    /// hand the game does, off the same prefab, driven by the same two scale
    /// formulas (vanilla's clamped one, and the mod's uncapped one - see
    /// <see cref="PointPingerPatches"/>), with the same
    /// <see cref="PingRipple"/> spawned at its foot.
    ///
    /// It reaches the preview as a texture, not as geometry. The stage is a
    /// world-space UI canvas (<see cref="PreviewScene"/>), and 3D geometry put in
    /// front of it would also be in front of the mod's own labels - the exact
    /// inversion of the game, where the hand is in the world and the HUD is drawn
    /// over it. Rendering the hand alone into a transparent texture and showing
    /// that texture as one more layer of the stage - above the screenshot, below
    /// every widget - restores the right order, and (since the two cameras share a
    /// projection) puts it on exactly the pixels its ping anchor projects to.
    ///
    /// The hand's own little world sits far from both PEAK's map and the stage
    /// (<see cref="MarkerWorldOrigin"/>), so its camera has nothing else in front
    /// of it; the scene's sun still lights it, being directional, which is what
    /// keeps it shaded like the game's own ping rather than a flat cut-out.
    /// </summary>
    internal class PreviewPingMarker : MonoBehaviour
    {
        /// <summary>
        /// Far from PEAK's world (so the map isn't in the shot) and far from the
        /// stage canvas, which is parked at its own remote origin - a 1920x1080
        /// world-space rect would otherwise fill this camera's view from behind.
        /// </summary>
        private static readonly Vector3 MarkerWorldOrigin = new Vector3(50000f, 100000f, 0f);

        /// <summary>Tight around the hand: nothing else is meant to be in view, and a short frustum is what guarantees it.</summary>
        private const float FarClip = 200f;

        /// <summary>How far out the screenshot hangs - see <see cref="BuildBackdrop"/>. Far enough behind the hand that nothing can put the two the wrong way round, near enough to stay well inside <see cref="FarClip"/>.</summary>
        private const float BackdropDistance = 120f;

        /// <summary>Vanilla's own hard clamp on the frustum term - <c>PointPing.minMaxScale</c>, read off the prefab, this is only the fallback if it can't be.</summary>
        private static readonly Vector2 FallbackMinMaxScale = new Vector2(0.2f, 3f);

        /// <summary>
        /// How much smaller the hand is drawn with <c>enable-scaling</c> off, on top
        /// of vanilla's own clamped formula - and the one number in this class that
        /// is a picture rather than a calculation.
        ///
        /// Vanilla's clamp only starts biting past about 22m, and the preview's ping
        /// is at 24m: run honestly, the two formulas differ by a few percent there,
        /// which reads as the setting doing nothing at all. What the setting actually
        /// buys you is at 100m+, where an unclamped ping stays the size it is here
        /// and a vanilla one has shrunk to a speck - and a single still scene at one
        /// fixed distance has no way to show that. So the off state is drawn at the
        /// size it *would* be somewhere worth caring about, which is the honest
        /// summary of the setting even though it is not the honest arithmetic for
        /// this particular 24m ping.
        /// </summary>
        private const float VanillaShrink = 0.7f;

        /// <summary>How much of the hand's projected size counts as "clicked it" - generously padded, since it's a small target in a shrunk-down preview.</summary>
        private const float HitPadding = 0.02f;

        /// <summary>The stage the preview is drawn at (<see cref="PreviewScene"/>'s own 1920x1080), needed here to turn a distance in stage pixels back into world units.</summary>
        private const float StageHeightPixels = 1080f;

        /// <summary>How far below the anchor the ripple is centred, in stage pixels - half of the 22px the ping's distance label sits below it (<see cref="PingWidget"/>), i.e. squarely in the gap between the fingertip and the label.</summary>
        private const float RippleDropPixels = 11f;

        private Camera _camera;
        private GameObject _root;
        private GameObject _ping;
        private Transform _pingTransform;
        private RenderTexture _texture;
        private RawImage _surface;

        /// <summary>
        /// The prefab's own animation, if it has one. Disabled the moment the hand
        /// is built and only ever run again by <see cref="Ping"/>: whatever it plays
        /// is a one-shot for a ping that lives two seconds and is then destroyed, so
        /// left running it plays itself out and leaves the preview's hand - which is
        /// supposed to just stand there, being looked at - gone.
        /// </summary>
        private readonly List<Animator> _animators = new List<Animator>();
        private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();

        /// <summary>The game's own ping binding (default middle mouse), read live, so the preview answers to whatever the player has actually bound it to.</summary>
        private InputAction _pingAction;

        private Vector3 _worldPoint;

        /// <summary>The ping anchor, in the hand's own little world - the spot every widget of the ping is drawn around, and the spot the hand has to stay centred on at any scale.</summary>
        private Vector3 _anchorLocal;

        /// <summary>See <see cref="MeasureFootOffset"/>.</summary>
        private Vector3 _footOffsetAtUnitScale;

        private Color _color;
        private float _sizeOfFrustum = 0.1f;
        private Vector2 _minMaxScale = FallbackMinMaxScale;

        /// <summary>Only used to fade the ripple back in when <c>enable-ripple</c> is switched on, rather than leaving the setting looking dead until the next click.</summary>
        private bool _rippleWasEnabled;

        /// <summary>
        /// Builds the marker, or returns null if the game can't supply the pieces
        /// (no local character, no ping prefab). The preview is still perfectly
        /// usable then - it just shows the scene without a hand in it, which is
        /// what the screenshot behind it now looks like anyway.
        /// </summary>
        internal static PreviewPingMarker TryCreate(RectTransform stage, Camera previewCamera, Vector3 worldPoint, Vector3 pingerHeadWorld, Color color, Sprite background)
        {
            Character local = Character.localCharacter;
            PointPinger pinger = local != null ? local.GetComponent<PointPinger>() : null;
            if (pinger == null || pinger.pointPrefab == null)
            {
                return null;
            }

            var go = new GameObject("PingMarker", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(stage, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var marker = go.AddComponent<PreviewPingMarker>();
            marker._worldPoint = worldPoint;
            marker._color = color;

            marker._surface = go.GetComponent<RawImage>();
            marker._surface.raycastTarget = false;

            try
            {
                marker.Build(pinger.pointPrefab, previewCamera, local, pingerHeadWorld, background);
            }
            catch (Exception e)
            {
                Plugin.Instance.Log.LogError($"PreviewPingMarker: could not build the ping hand (preview will just show no hand): {e}");
                Destroy(go);
                return null;
            }

            return marker;
        }

        /// <summary>
        /// The hand's camera shares the preview camera's projection exactly, and
        /// the hand is placed at the same offset from it as the ping anchor is
        /// from the preview camera - so it lands on the same pixels of the stage
        /// as everything else that tracks that anchor (its distance label, its
        /// compass marker, its off-screen arrow).
        /// </summary>
        private void Build(GameObject prefab, Camera previewCamera, Character local, Vector3 pingerHeadWorld, Sprite background)
        {
            _root = new GameObject("SoD.PreviewPingWorld");
            DontDestroyOnLoad(_root);
            _root.transform.position = MarkerWorldOrigin;

            var cameraGo = new GameObject("SoD.PreviewPingCamera");
            cameraGo.transform.SetParent(_root.transform, false);

            _camera = cameraGo.AddComponent<Camera>();

            // Off until it has somewhere to render *to*. A camera with no target
            // texture renders to the screen, and this one is built a frame before
            // EnsureTexture first runs - which is exactly the flash of screenshot
            // seen over the loading screen when the menu was opened.
            _camera.enabled = false;

            _camera.fieldOfView = previewCamera.fieldOfView;
            _camera.aspect = previewCamera.aspect;
            _camera.nearClipPlane = previewCamera.nearClipPlane;
            _camera.farClipPlane = FarClip;

            // Opaque, and with the screenshot hung behind the hand rather than left
            // underneath it in the UI (see BuildBackdrop). The obvious build - clear
            // this camera to a transparent color and let the stage's own screenshot
            // show through the empty parts - is what this was first written as, and
            // URP simply doesn't honor it: what comes back is an opaque black frame,
            // which then paints over the entire preview. Compositing the two here,
            // where they're both just things in front of a camera, needs no alpha
            // channel to survive a render pipeline at all.
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;

            BuildBackdrop(background);

            _ping = SpawnHand(prefab, local);
            _pingTransform = _ping.transform;
            _pingTransform.SetParent(_root.transform, false);

            // The preview camera sits at the origin looking down +z, so an anchor's
            // world point *is* its offset from the camera - the same offset, applied
            // to this camera, projects it to the same place.
            _anchorLocal = _worldPoint - previewCamera.transform.position;
            _pingTransform.localPosition = _anchorLocal;
            _pingTransform.localRotation = HandRotation(_anchorLocal, pingerHeadWorld - previewCamera.transform.position);

            MeasureFootOffset();
        }

        /// <summary>
        /// Where the hand's foot - the bottom of it, horizontally centred - sits
        /// relative to its pivot, at scale 1.
        ///
        /// That foot is the point the preview grows the hand *from*, and it's neither
        /// of the two obvious candidates. Not the prefab's own pivot, which is the
        /// fingertip: that's the right thing for the game (a real ping is planted on
        /// the surface it hit, and grows away from it) but it sends the hand sliding
        /// off sideways as it gets bigger, since the tip is nowhere near under the
        /// body. And not the middle of the mesh either, which keeps the hand on its
        /// anchor but grows it *downward* as well as up, straight over the distance
        /// label sitting below.
        ///
        /// Bottom-centre gives what the mod's own scaling looks like in game: the
        /// hand stays where it is and gets taller upward, with everything underneath
        /// it left alone.
        ///
        /// Measured, not assumed: it depends on the mesh and on the rotation the hand
        /// happens to have landed at, neither of which is a number worth hardcoding.
        /// </summary>
        private void MeasureFootOffset()
        {
            _pingTransform.localScale = Vector3.one;

            bool any = false;
            Bounds bounds = default;
            foreach (Renderer renderer in _ping.GetComponentsInChildren<Renderer>())
            {
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (!any)
            {
                _footOffsetAtUnitScale = Vector3.zero;
                return;
            }

            // Bottom-right, not bottom-centre: the hand points off to the right, at
            // whatever it's pinging, so its body belongs to the left of the point it
            // is pointing from. Growing it out of that corner sends it up and to the
            // left, away from both the thing it points at and the label below it.
            var foot = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
            _footOffsetAtUnitScale = foot - _pingTransform.position;
        }

        /// <summary>
        /// The screenshot, hung far behind the hand as a plain unlit sprite, exactly
        /// filling this camera's view - so the texture this camera produces is the
        /// finished picture (scene, then hand on top of it) rather than a hand on a
        /// background that has to be blended in afterwards. Well behind the hand
        /// (which sits ~15 units out) and well inside the far clip, so the hand is
        /// always in front of it and nothing else ever is.
        /// </summary>
        private void BuildBackdrop(Sprite background)
        {
            if (background == null)
            {
                return;
            }

            var go = new GameObject("Backdrop", typeof(SpriteRenderer));
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, BackdropDistance);
            go.transform.localRotation = Quaternion.identity;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = background;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // The sprite's own world height, from its pixels-per-unit, scaled up to
            // the height the frustum has spread to out there. Its aspect already
            // matches the camera's, so getting the height right gets the width right.
            float spriteHeight = background.bounds.size.y;
            float frustumHeight = 2f * BackdropDistance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (spriteHeight > 0f)
            {
                go.transform.localScale = Vector3.one * (frustumHeight / spriteHeight);
            }
        }

        /// <summary>
        /// The prefab, with its <see cref="PointPing"/> component taken off it.
        /// That component is the whole reason this is instantiated under a
        /// deactivated holder: its <c>Start</c> plays the ping sound, hands the
        /// local character a "point at this" animation target, and re-derives the
        /// hand's scale every frame off <c>Camera.main</c> and the *real* local
        /// player's position - all of which belong to a ping that was actually
        /// thrown, not to a picture of one in a menu. Instantiating inactive means
        /// none of it ever runs; what's left is the art.
        /// </summary>
        private GameObject SpawnHand(GameObject prefab, Character local)
        {
            var holder = new GameObject("SoD.PreviewPingHolder");
            holder.SetActive(false);

            GameObject instance = Instantiate(prefab, holder.transform);

            var ping = instance.GetComponent<PointPing>();
            if (ping != null)
            {
                // Read before it's destroyed: these are the prefab's own numbers, the
                // ones both scale formulas are written against.
                _sizeOfFrustum = ping.sizeOfFrustum;
                _minMaxScale = ping.minMaxScale;
                DestroyImmediate(ping);
            }

            // Whatever the prefab animates, it animates on the way out: a real ping
            // exists for two seconds and is then destroyed, so its animation is a
            // one-shot that plays itself to an end nobody sees, because the object is
            // gone by then. The preview's hand is meant to just stand there, so it is
            // held at rest and only ever run by Ping().
            _animators.AddRange(instance.GetComponentsInChildren<Animator>(true));
            _particles.AddRange(instance.GetComponentsInChildren<ParticleSystem>(true));

            foreach (Animator animator in _animators)
            {
                animator.enabled = false;
            }

            foreach (ParticleSystem particles in _particles)
            {
                var emission = particles.emission;
                emission.enabled = false;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            instance.transform.SetParent(null, false);
            instance.SetActive(true);
            Destroy(holder);

            ApplyMaterial(instance, local);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                // A hand rendered 100km above the map would otherwise still be asked
                // to cast a shadow for a sun that has nothing here to cast it onto.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            LogHierarchy(instance);
            return instance;
        }

        /// <summary>
        /// What the ping prefab actually is, written to the log once per build. Its
        /// hierarchy is Unity-serialized data, not anything the decompiled game code
        /// shows, so this is the only way to know which of its pieces are what -
        /// worth keeping behind the debug flag rather than deleting, since the next
        /// PEAK update can quietly change all of it.
        /// </summary>
        private static void LogHierarchy(GameObject instance)
        {
            if (!Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                return;
            }

            var text = new StringBuilder("PreviewPingMarker: ping prefab hierarchy");
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                text.Append("\n  ").Append(child.name).Append(" [");
                foreach (Component component in child.GetComponents<Component>())
                {
                    text.Append(component == null ? "<missing>" : component.GetType().Name).Append(' ');
                }
                text.Append(']');
            }

            Plugin.Instance.Log.LogInfo(text.ToString());
        }

        /// <summary>
        /// The same material swap the real ping does (a copy of the pinging
        /// character's own body material - see <c>PointPingerPatches.SpawnPingNow</c>),
        /// re-tinted to the preview's ping color: the hand in the preview belongs to
        /// a teammate who isn't the one holding the menu open, so it can't just
        /// inherit the local player's color.
        /// </summary>
        private void ApplyMaterial(GameObject instance, Character local)
        {
            Renderer source = local.refs.mainRenderer;
            if (source == null || source.sharedMaterial == null)
            {
                return;
            }

            var material = Instantiate(source.sharedMaterial);
            material.SetColor(SkinColorProperty, _color);
            material.SetFloat(OpacityProperty, 1f);

            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.material = material;
            }
        }

        private static readonly int SkinColorProperty = Shader.PropertyToID("_SkinColor");
        private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

        /// <summary>
        /// <c>PointPing.Go</c>'s own rotation, which tilts the hand between the
        /// surface it hit and the direction it was pointed from. Computed once:
        /// unlike the scale, nothing in the menu changes it, and the two things it
        /// depends on (where the ping landed, who threw it) are fixed in a still
        /// scene.
        /// </summary>
        private Quaternion HandRotation(Vector3 pingLocal, Vector3 pingerHeadLocal)
        {
            Vector3 hitNormal = Vector3.up;
            Vector3 pingerForward = (pingLocal - pingerHeadLocal).normalized;

            // Straight from the camera, which is at this little world's origin.
            Vector3 toPing = pingLocal;

            const float angleThing = 90f;
            float angleToPinger = Vector3.Angle(pingerForward, toPing);
            Vector3 blended = Vector3.Lerp(-hitNormal, pingerForward, Mathf.Clamp01(angleToPinger / angleThing));
            float angleToBlended = Vector3.Angle(blended, toPing);
            Vector3 forward = Vector3.Lerp(-Vector3.up, blended, Mathf.Clamp01(angleToBlended / angleThing));

            return Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>Matches the stage's own render target, so the magnifier shows the hand at the same 1:1 sharpness as everything else in the preview.</summary>
        internal void EnsureTexture(int width, int height)
        {
            if (_texture != null && _texture.width == width && _texture.height == height)
            {
                return;
            }

            if (_texture != null)
            {
                _camera.targetTexture = null;
                _texture.Release();
                Destroy(_texture);
            }

            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "SoD.PreviewPingMarker",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            _camera.targetTexture = _texture;
            _camera.enabled = true;
            _surface.texture = _texture;
        }

        /// <summary>
        /// Vanilla's scale and the mod's, side by side - the whole reason the hand
        /// is live. Both are the frustum's size where the ping is, times the
        /// prefab's <c>sizeOfFrustum</c>; the only difference is that vanilla first
        /// clamps that frustum term to <c>minMaxScale</c> (which is what makes a
        /// distant ping shrink on screen), while the mod leaves it uncapped and
        /// applies the user's multiplier (which is what keeps it the same apparent
        /// size at any range). Recomputed every frame, exactly as the game does it,
        /// so dragging the multiplier moves the hand as you drag.
        /// </summary>
        private float CurrentScale(PluginConfig cfg)
        {
            float distance = Vector3.Distance(_camera.transform.position, _pingTransform.position);
            float frustum = _camera.SizeOfFrustumAtDistance(distance);

            if (!cfg.EnablePingScaling.Value)
            {
                return Mathf.Clamp(frustum, _minMaxScale.x, _minMaxScale.y) * _sizeOfFrustum * VanillaShrink;
            }

            return frustum * _sizeOfFrustum * cfg.PingScaleMultiplier.Value;
        }

        internal void Refresh(PluginConfig cfg)
        {
            if (_pingTransform == null)
            {
                return;
            }

            // Scaled about its own middle rather than about its pivot. Uniform
            // scaling moves everything away from the pivot in proportion to the
            // scale, the hand's middle included, so putting the pivot back by exactly
            // that much leaves the middle where it was - which is on the ping anchor,
            // where the distance label, the compass marker and the off-screen arrow
            // all agree the ping is. The game doesn't need this (its pivot is the
            // fingertip, planted on whatever surface was actually hit, and that's the
            // right place for it to grow from), but the preview isn't showing where a
            // ping landed - it's showing how big a ping is, at four settings' worth of
            // sizes, and it has to stay in frame while it does.
            float scale = CurrentScale(cfg);
            _pingTransform.localScale = Vector3.one * scale;
            _pingTransform.localPosition = _anchorLocal - _footOffsetAtUnitScale * scale;

            if (PingKeyWasPressed())
            {
                Ping();
            }

            bool rippleEnabled = cfg.EnablePingRipple.Value;
            if (rippleEnabled && !_rippleWasEnabled)
            {
                // Turning the setting on is a request to see the thing it turns on.
                EmitRipple();
            }
            _rippleWasEnabled = rippleEnabled;
        }

        /// <summary>
        /// Whether the player pressed their own ping button this frame - the game's
        /// <c>Ping</c> action (default middle mouse), the same one
        /// <c>PointPinger</c> reads, so rebinding it in PEAK's options rebinds it
        /// here with no further help.
        ///
        /// Read off the action's bound *controls* rather than off the action itself:
        /// the menu takes input away from the player while it's open, which leaves
        /// the action disabled, and a disabled action never reports a press however
        /// hard the button is pushed. Its bindings are still perfectly readable.
        /// </summary>
        private bool PingKeyWasPressed()
        {
            _pingAction ??= InputSystem.actions?.FindAction("Ping");
            if (_pingAction == null)
            {
                return false;
            }

            foreach (InputControl control in _pingAction.controls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Throws the ping again, in place: the hand plays whatever it plays when it
        /// lands, and the ripple goes out from its foot. The hand doesn't move - the
        /// whole point of the preview is that it stands still and is looked at - so
        /// this is a ping of the spot the ping is already at.
        /// </summary>
        internal void Ping()
        {
            // Deliberately not the prefab's own animation, which is left held where
            // SpawnHand held it: whatever it plays, it plays a hand *out* of a scene
            // it is only in for two seconds, and a preview hand that danced away
            // every time the ping key was pressed would be exactly the thing this
            // was asked to stop doing.
            foreach (ParticleSystem particles in _particles)
            {
                if (particles == null)
                {
                    continue;
                }

                var emission = particles.emission;
                emission.enabled = true;
                particles.Play(true);
            }

            EmitRipple();
        }

        /// <summary>
        /// A real <see cref="PingRipple"/> under the hand, tracking the hand's own
        /// scale exactly as it does in game - so it grows with the multiplier too. On
        /// an unscaled clock, because the ripple lives for one second of wall time and
        /// the menu is not a place where the game's clock can be relied on to be
        /// running.
        ///
        /// Centred in the gap between the fingertip and the distance label, rather
        /// than on the hand's pivot: in game the ripple sits on the surface the ping
        /// hit, which is behind and below the hand, and this is the preview's flat
        /// stand-in for that - it reads as the ground the hand is pointing at rather
        /// than as a bubble around the hand itself.
        /// </summary>
        internal void EmitRipple()
        {
            if (_pingTransform == null || !Plugin.Instance.Cfg.EnablePingRipple.Value)
            {
                return;
            }

            // The label's own drop below the anchor is in stage pixels, so the ripple's
            // half of it is converted back to world units through the frustum's size out
            // where the ping is - the one place the two measures meet.
            float distance = Vector3.Distance(_camera.transform.position, _root.transform.position + _anchorLocal);
            float worldPerStagePixel = _camera.SizeOfFrustumAtDistance(distance) / StageHeightPixels;
            Vector3 origin = _root.transform.position + _anchorLocal - Vector3.up * (RippleDropPixels * worldPerStagePixel);

            GameObject ripple = PingRipple.Spawn(origin, _color, _pingTransform, unscaledTime: true);
            if (ripple != null)
            {
                ripple.transform.SetParent(_root.transform, worldPositionStays: true);
            }
        }

        /// <summary>
        /// Whether a point of the preview (in viewport coordinates, the same 0..1
        /// the scene's own cast is written in) is on the hand. Measured off the
        /// hand's actual projected bounds rather than a fixed radius, so it stays
        /// right when the scale settings make the hand six times bigger.
        /// </summary>
        internal bool HitTest(Vector2 viewportPoint)
        {
            if (_ping == null)
            {
                return false;
            }

            Bounds? bounds = null;
            foreach (Renderer renderer in _ping.GetComponentsInChildren<Renderer>())
            {
                bounds = bounds == null ? renderer.bounds : Grow(bounds.Value, renderer.bounds);
            }

            if (bounds == null)
            {
                return false;
            }

            Vector2 min = Vector2.one * float.MaxValue;
            Vector2 max = Vector2.one * float.MinValue;

            Bounds b = bounds.Value;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? b.min.x : b.max.x,
                    (corner & 2) == 0 ? b.min.y : b.max.y,
                    (corner & 4) == 0 ? b.min.z : b.max.z);

                Vector3 viewport = _camera.WorldToViewportPoint(point);
                if (viewport.z <= 0f)
                {
                    return false;
                }

                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }

            return viewportPoint.x >= min.x - HitPadding && viewportPoint.x <= max.x + HitPadding
                && viewportPoint.y >= min.y - HitPadding && viewportPoint.y <= max.y + HitPadding;
        }

        private static Bounds Grow(Bounds bounds, Bounds other)
        {
            bounds.Encapsulate(other);
            return bounds;
        }

        /// <summary>The hand's world lives outside the menu's hierarchy (it has to, to be rendered by a camera), so it's switched on and off with the preview by hand - same as the stage. See <c>PreviewScene.SyncStageActive</c>.</summary>
        private void OnEnable() => SyncActive();

        private void OnDisable() => SyncActive();

        private void SyncActive()
        {
            if (_root != null)
            {
                _root.SetActive(isActiveAndEnabled);
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root);
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
