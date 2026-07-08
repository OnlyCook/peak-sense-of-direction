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
        private Material _material;
        private Color _color;
        private Transform _pingTransform;
        private float _elapsed;

        public static void Spawn(Vector3 worldPosition, Color color, Transform pingTransform)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SoD.PingRipple";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = worldPosition;
            go.transform.localScale = Vector3.one * (StartRadius * 2f);

            var ripple = go.AddComponent<PingRipple>();
            ripple._pingTransform = pingTransform;
            ripple._color = color;
            ripple.Init();
        }

        private void Init()
        {
            _renderer = GetComponent<Renderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            _material = new Material(Shader.Find("Sprites/Default"));
            _material.color = _color;
            _renderer.material = _material;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Duration);

            float pingScale = _pingTransform != null ? _pingTransform.localScale.x : 1f;
            float radius = Mathf.Lerp(StartRadius, BaseMaxRadius, t) * pingScale;
            transform.localScale = Vector3.one * (radius * 2f);

            Color c = _color;
            c.a = (1f - t) * 0.5f;
            _material.color = c;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
