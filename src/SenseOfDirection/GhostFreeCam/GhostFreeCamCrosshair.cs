using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// Small screen-center reticle shown only while
    /// <see cref="GhostFreeCamPatches"/> has free-cam engaged - vanilla's
    /// spectate view has no crosshair at all, so pinging while free-camming
    /// otherwise has nothing to aim at (`PointPinger` raycasts from the
    /// screen center same as normal play, see RESEARCH.md Q6).
    ///
    /// A small procedurally-drawn round dot (feathered-alpha technique, same
    /// as <see cref="GhostFreeCamKeyHint"/>'s badge), tinted with vanilla's
    /// own default-reticle color (<c>GUIManager.reticleColorDefault</c>) so
    /// it at least matches vanilla's palette. Not a literal reuse of
    /// vanilla's own reticle sprite/material - that also carries whatever
    /// shader drives its "jagged edge" look, which didn't survive being
    /// copied onto a plain <see cref="Image"/> (rendered as a flat square).
    ///
    /// Parented under <see cref="Indicators.IndicatorManager"/>'s existing
    /// full-screen overlay canvas (already centered at anchor (0.5, 0.5),
    /// so anchoredPosition zero is exactly screen center) rather than
    /// standing up a second canvas just for this.
    /// </summary>
    public static class GhostFreeCamCrosshair
    {
        private static RectTransform _root;
        private static Image _image;
        private static Sprite _sprite;

        public static void SetVisible(bool visible)
        {
            if (!visible && _root == null)
            {
                // Never instantiated and nothing to show - skip creating it.
                return;
            }

            EnsureCreated();
            _root.gameObject.SetActive(visible);
        }

        private static void EnsureCreated()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject("GhostFreeCamCrosshair", typeof(RectTransform), typeof(Image));
            _root = (RectTransform)go.transform;
            _root.SetParent(Indicators.IndicatorManager.Instance.CanvasTransform, false);
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(10f, 10f);

            _image = go.GetComponent<Image>();
            _image.sprite = _sprite ??= BuildDotSprite();
            _image.type = Image.Type.Simple;
            _image.raycastTarget = false;
            _image.preserveAspect = true;

            Color color = GUIManager.instance != null
                ? GUIManager.instance.reticleColorDefault
                : Color.white;
            color.a *= 0.85f;
            _image.color = color;

            go.SetActive(false);
        }

        private static Sprite BuildDotSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = size * 0.42f;
            const float feather = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist / feather);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
