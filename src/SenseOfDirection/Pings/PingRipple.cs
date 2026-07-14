using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// A short-lived expanding translucent sphere in the pinging player's own
    /// character color, spawned at a ping's hit point so it reads against
    /// similarly-colored terrain (ROADMAP.md's "3D ripple effect"). A true 3D
    /// volume rather than the original flat ring - a flat disc laid on the
    /// hit-normal plane degenerates to a thin sliver (or vanishes entirely)
    /// when viewed near edge-on, which looks broken on steep/vertical
    /// surfaces; a sphere reads correctly from every angle.
    ///
    /// Tracks the pinging <c>PointPing</c>'s own (uncapped, distance-relative)
    /// <c>transform.localScale</c> every frame so the ripple always appears
    /// at a consistent size *relative to the ping marker itself*, the same
    /// "always the same apparent size regardless of distance" behavior
    /// <see cref="PointPingerPatches"/>'s own scale fix gives the ping -
    /// rather than a fixed world-unit radius, which would look tiny far away
    /// or oversized up close.
    /// </summary>
    public class PingRipple : MonoBehaviour
    {
        private const float Duration = 1f;
        private const float StartRadius = 0.05f;
        private const float BaseMaxRadius = 2f;

        private Renderer _renderer;
        private Color _color;
        private Transform _pingTransform;
        private float _elapsed;
        private bool _unscaledTime;

        /// <summary>
        /// Last observed <c>_pingTransform.localScale.x</c>, kept even after
        /// that transform is gone. Re-pinging the same spot makes
        /// <c>PointPingerPatches</c> immediately <c>DestroyImmediate</c> the
        /// *previous* ping marker (only one tracked per <c>PointPinger</c> at
        /// a time) - this ripple is free-standing, not destroyed with it, so
        /// it was left reading a destroyed transform and falling back to a
        /// hardcoded <c>1f</c> mid-fade, which (especially up close, where the
        /// real distance-relative scale is well under 1) showed up as the
        /// ripple suddenly jumping/growing right as its source ping vanished.
        /// Freezing at the last real value instead removes that discontinuity.
        /// </summary>
        private float _lastKnownPingScale = 1f;

        /// <summary>
        /// Mesh and material, resolved once instead of once per ripple.
        /// <c>GameObject.CreatePrimitive</c> (what this used to spawn with)
        /// builds a GameObject *and* a SphereCollider that was then immediately
        /// destroyed again - a physics object created and torn down for
        /// nothing, on the ping path - <c>Shader.Find</c> is a by-name lookup
        /// through every loaded shader, and a <c>new Material</c> per ripple
        /// makes the renderer set up a fresh shader variant each time. None of
        /// that varies per ripple: only the color does, and that's per-instance
        /// state a <see cref="MaterialPropertyBlock"/> carries without touching
        /// the material at all.
        /// </summary>
        private static Mesh _sphereMesh;
        private static Material _sharedMaterial;
        private static MaterialPropertyBlock _propertyBlock;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        /// <summary>Resolves the shared mesh/material. Safe to call repeatedly; <see cref="Common.PingPrewarm"/> calls it before any ping does.</summary>
        internal static void EnsureAssets()
        {
            if (_sphereMesh == null)
            {
                GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphereMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(primitive);
            }
            if (_sharedMaterial == null)
            {
                _sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            _propertyBlock ??= new MaterialPropertyBlock();
        }

        /// <summary>
        /// <paramref name="unscaledTime"/> is for the config preview
        /// (<see cref="Ui.PreviewPingMarker"/>), whose ripple is emitted from a menu
        /// rather than from play: a ripple lives for one second, and a menu is not a
        /// place where the game's own clock can be relied on to still be running.
        /// Returns the ripple so a caller that keeps its own little world can adopt
        /// it into it.
        /// </summary>
        public static GameObject Spawn(Vector3 worldPosition, Color color, Transform pingTransform, bool unscaledTime = false)
        {
            EnsureAssets();

            var go = new GameObject("SoD.PingRipple", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = worldPosition;
            go.transform.localScale = Vector3.one * (StartRadius * 2f);
            go.GetComponent<MeshFilter>().sharedMesh = _sphereMesh;

            var ripple = go.AddComponent<PingRipple>();
            ripple._pingTransform = pingTransform;
            ripple._color = color;
            ripple._unscaledTime = unscaledTime;
            ripple.Init();

            return go;
        }

        private void Init()
        {
            _renderer = GetComponent<Renderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sharedMaterial = _sharedMaterial;
            ApplyColor(_color);
        }

        /// <summary>Per-ripple tint/alpha, applied without instancing a material per ripple (see <see cref="EnsureAssets"/>).</summary>
        private void ApplyColor(Color color)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorProperty, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void Update()
        {
            _elapsed += _unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Duration);

            if (_pingTransform != null)
            {
                _lastKnownPingScale = _pingTransform.localScale.x;
            }
            float radius = Mathf.Lerp(StartRadius, BaseMaxRadius, t) * _lastKnownPingScale;
            transform.localScale = Vector3.one * (radius * 2f);

            Color c = _color;
            c.a = (1f - t) * 0.5f;
            ApplyColor(c);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
