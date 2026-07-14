using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// One live panel drawn with <see cref="PanelChrome"/>: the jagged, grained
    /// blue panel used as the background of the config preview menu (and of the
    /// preview viewport's own frame).
    ///
    /// The panel's silhouette gently animates by cycling
    /// <see cref="PanelChrome.JagFrameCount"/> pre-baked variants of the same
    /// shape rather than re-rolling noise per frame.
    ///
    /// The grain overlay is a child of an invisible <see cref="Mask"/> inset by
    /// the border thickness, so it covers exactly the fill area and never paints
    /// over the border ring. The mask host's own Image is left invisible via
    /// <c>Mask.showMaskGraphic = false</c> rather than a transparent color: the
    /// border is painted by the *fill* image below, which is not a child of the
    /// mask and so is never clipped by it.
    /// </summary>
    internal class JaggedPanel : MonoBehaviour
    {
        private Image _fillImage;
        private Image _grainImage;
        private RectTransform _rect;

        private int _jagFrame;
        private float _jagFrameTimer;
        private Vector2 _bakedSize = Vector2.zero;

        /// <summary>Fades the whole panel (fill, border and grain together) - the menu eases this in on open.</summary>
        internal float Alpha { get; set; } = 1f;

        internal static JaggedPanel Create(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;

            var panel = go.AddComponent<JaggedPanel>();
            panel._rect = rect;
            panel._fillImage = go.GetComponent<Image>();

            // Type.Simple, matching how the sprite is baked: the whole texture
            // stretches as one piece, which is what keeps the jag visible on the
            // long straight edges (a 9-sliced sprite stretches those strips
            // along their own axis and dilutes the jag away to nothing there).
            panel._fillImage.type = Image.Type.Simple;
            panel._fillImage.raycastTarget = true; // swallow clicks so they don't reach the world behind

            var maskGo = new GameObject("GrainMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            var maskRect = (RectTransform)maskGo.transform;
            maskRect.SetParent(rect, false);
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(PanelChrome.PanelBorderThickness, PanelChrome.PanelBorderThickness);
            maskRect.offsetMax = new Vector2(-PanelChrome.PanelBorderThickness, -PanelChrome.PanelBorderThickness);

            var maskImage = maskGo.GetComponent<Image>();
            maskImage.sprite = PanelChrome.MaskSprite(128, Mathf.Max(1f, PanelChrome.PanelCornerRadius - PanelChrome.PanelBorderThickness));
            maskImage.type = Image.Type.Sliced;
            maskImage.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            var grainGo = new GameObject("Grain", typeof(RectTransform), typeof(Image));
            var grainRect = (RectTransform)grainGo.transform;
            grainRect.SetParent(maskRect, false);
            grainRect.anchorMin = Vector2.zero;
            grainRect.anchorMax = Vector2.one;
            grainRect.offsetMin = Vector2.zero;
            grainRect.offsetMax = Vector2.zero;

            panel._grainImage = grainGo.GetComponent<Image>();
            Texture2D grain = PanelChrome.GrainTexture();
            panel._grainImage.sprite = Sprite.Create(grain, new Rect(0, 0, grain.width, grain.height), new Vector2(0.5f, 0.5f), 100f);
            panel._grainImage.raycastTarget = false;

            panel.Rebake();
            return panel;
        }

        private void Update()
        {
            // Unscaled: the menu can be open while the game is otherwise
            // uninteractable, and the panel should keep breathing regardless.
            _jagFrameTimer += Time.unscaledDeltaTime;
            if (_jagFrameTimer >= PanelChrome.JagFrameInterval)
            {
                _jagFrameTimer = 0f;
                _jagFrame = (_jagFrame + 1) % PanelChrome.JagFrameCount;
                ApplyFrame();
            }

            // Re-bake only when the size actually changed (the sprite is baked
            // at exact pixel dimensions, so a stretched one would distort both
            // the corner radius and the jag scale).
            if (_rect.rect.size != _bakedSize)
            {
                Rebake();
            }

            ApplyAlpha();
        }

        private void Rebake()
        {
            _bakedSize = _rect.rect.size;
            ApplyFrame();
            ApplyAlpha();
        }

        private void ApplyFrame()
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(_bakedSize.x));
            int height = Mathf.Max(1, Mathf.RoundToInt(_bakedSize.y));
            _fillImage.sprite = PanelChrome.PanelSprite(width, height, _jagFrame);
        }

        private void ApplyAlpha()
        {
            _fillImage.color = new Color(1f, 1f, 1f, Alpha);

            // The grain is baked fully opaque, so it has to be faded in step
            // with the fill it sits on top of, or it would stay solid over a
            // half-faded panel.
            _grainImage.color = new Color(1f, 1f, 1f, Alpha);
        }
    }
}
