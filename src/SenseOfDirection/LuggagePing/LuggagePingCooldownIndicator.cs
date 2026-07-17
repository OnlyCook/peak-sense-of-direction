using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;

namespace SenseOfDirection.LuggagePing
{
    /// <summary>
    /// Subtle "still on cooldown" feedback for <see cref="LuggagePingController"/>:
    /// a small, low-opacity text that briefly appears near the bottom of the
    /// screen then fades out, shown whenever the player presses Luggage-Ping/key
    /// while Luggage-Ping/cooldown-seconds is still running. Without this, a
    /// press that visibly does nothing is easy to mistake for the key not
    /// working at all rather than a cooldown - a real report from the first
    /// pass of this feature (before any cooldown/feedback existed).
    ///
    /// Holds at full opacity briefly, then eases out - same two-phase shape
    /// <see cref="Pings.PingWidgetFadeOut"/> uses for its own fade, just with a
    /// hold phase in front since this needs to actually be read, not just
    /// register as "something happened" the way a disappearing ping marker does.
    /// </summary>
    public class LuggagePingCooldownIndicator : MonoBehaviour
    {
        private const float HoldSeconds = 0.6f;
        private const float FadeSeconds = 0.5f;

        private static LuggagePingCooldownIndicator _instance;

        public static LuggagePingCooldownIndicator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.LuggagePingCooldownIndicator");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LuggagePingCooldownIndicator>();
                }
                return _instance;
            }
        }

        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private TMP_Text _text;
        private float _elapsed;
        private bool _active;

        public void Show(float remainingSeconds)
        {
            EnsureCreated();
            ApplyNativeFontIfReady();

            _text.text = $"LUGGAGE PING ON COOLDOWN ({Mathf.CeilToInt(remainingSeconds)}s)";
            _elapsed = 0f;
            _active = true;
            _root.gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed <= HoldSeconds)
            {
                return;
            }

            float fadeT = Mathf.Clamp01((_elapsed - HoldSeconds) / FadeSeconds);
            _canvasGroup.alpha = 1f - fadeT;
            if (fadeT >= 1f)
            {
                _active = false;
                _root.gameObject.SetActive(false);
            }
        }

        private void EnsureCreated()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject("LuggagePingCooldownIndicator", typeof(RectTransform), typeof(CanvasGroup));
            _root = (RectTransform)go.transform;
            _root.SetParent(IndicatorManager.Instance.CanvasTransform, false);
            _root.anchorMin = new Vector2(0.5f, 0f);
            _root.anchorMax = new Vector2(0.5f, 0f);
            _root.pivot = new Vector2(0.5f, 0f);
            // GhostFreeCamKeyHint sits at y=200 with a ~44px content-fitted
            // height (top edge ~244) - this sits with a clear gap above that,
            // rather than below (nearer the vanilla stamina bar), so the two
            // can never overlap even though they don't actually show at the
            // same time in practice (one needs the player alive, the other
            // dead).
            _root.anchoredPosition = new Vector2(0f, 264f);
            _root.sizeDelta = new Vector2(500f, 30f);

            _canvasGroup = go.GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(_root, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 20f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = new Color(1f, 1f, 1f, 0.8f);
            _text.raycastTarget = false;
            _text.enableWordWrapping = false;

            go.SetActive(false);
        }

        /// <summary>
        /// Same font + outline material vanilla-styled text elsewhere in this
        /// mod uses (e.g. the preview menu's own "Loading..." text) - without
        /// the outline material, plain white text over a bright/high-contrast
        /// background (snow, a light wall) is easy to lose entirely.
        /// </summary>
        private void ApplyNativeFontIfReady()
        {
            NativeAssets.TryFindAll();
            TMP_FontAsset font = NativeAssets.Font;
            if (font != null && _text.font != font)
            {
                _text.font = font;
            }
            if (NativeAssets.OutlineMaterial != null && _text.fontSharedMaterial != NativeAssets.OutlineMaterial)
            {
                _text.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }
        }
    }
}
