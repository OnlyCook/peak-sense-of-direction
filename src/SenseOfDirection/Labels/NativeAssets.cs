using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Lazily discovers a few of PEAK's own UI assets at runtime, since Unity
    /// asset references (font/material/sprite) aren't visible in the game's
    /// decompiled IL - see RESEARCH.md Q2/Q7. Retried on demand (cheap, no
    /// caching invalidation needed) until each is found, since the native UI
    /// may not have created any instances yet when Sense of Direction first
    /// looks (e.g. still on the main menu).
    /// </summary>
    public static class NativeAssets
    {
        public static TMP_FontAsset Font { get; private set; }
        public static Material OutlineMaterial { get; private set; }

        /// <summary>
        /// TMP's stock "LiberationSans SDF" fallback font - not a vanilla PEAK
        /// asset, just the plain sans font already bundled with TMP (present
        /// in <c>resources.assets</c> as TMP's own fallback), used where a
        /// plain non-decorative font reads better than <see cref="Font"/>
        /// (e.g. <see cref="GhostFreeCam.GhostFreeCamKeyHint"/>'s badge
        /// letter when no <see cref="KeyboardSprites"/> glyph covers the key).
        /// </summary>
        public static TMP_FontAsset FallbackFont { get; private set; }

        /// <summary>
        /// Vanilla's own per-key keyboard glyph atlas (<c>InputSpriteData
        /// .keyboardSprites</c>) - a public, always-`Resources.Load`-able
        /// singleton asset (see <c>SingletonAsset&lt;T&gt;</c>), not
        /// discovered by scanning live instances like <see cref="Font"/>.
        /// </summary>
        public static TMP_SpriteAsset KeyboardSprites { get; private set; }

        /// <summary>Vanilla's own player-name-label text color, e.g. for the "use plain color" fallback.</summary>
        public static Color DefaultTextColor { get; private set; } = Color.white;

        public static Sprite HostStarSprite { get; private set; }

        /// <summary>
        /// The game's own HUD campfire icon (StaminaBar.campfire - the small
        /// icon shown while the no-hunger buff is active), reused the same
        /// way `~/Projects/GitHub/peak-checkpoint-save`'s SavePicker title
        /// row grabs it for its F7 menu - no bundled asset needed.
        /// </summary>
        public static Sprite CampfireIconSprite { get; private set; }

        private static bool _foundDefaultTextColor;

        /// <summary>Call periodically (e.g. once per frame) until this returns true.</summary>
        public static bool TryFindAll()
        {
            if (Font == null || OutlineMaterial == null)
            {
                TryFindFont();
            }
            if (HostStarSprite == null || !_foundDefaultTextColor)
            {
                TryFindPlayerNameAssets();
            }
            if (CampfireIconSprite == null)
            {
                TryFindCampfireIcon();
            }
            // Best-effort, not folded into the return value below - neither
            // is required by any existing caller of TryFindAll, and both
            // resolve trivially (KeyboardSprites via a direct Resources.Load,
            // FallbackFont via a name match against TMP's own always-loaded
            // stock font) so there's no real "not ready yet" case to gate on.
            if (KeyboardSprites == null)
            {
                KeyboardSprites = InputSpriteData.Instance != null ? InputSpriteData.Instance.keyboardSprites : null;
            }
            if (FallbackFont == null)
            {
                TryFindFallbackFont();
            }

            return Font != null && OutlineMaterial != null && HostStarSprite != null
                   && _foundDefaultTextColor && CampfireIconSprite != null;
        }

        private static void TryFindFont()
        {
            var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                Material material = text.materialForRendering;
                if (material != null && material.name.Contains("DarumaDropOne-Regular SDF Outline"))
                {
                    Font = text.font;
                    OutlineMaterial = material;
                    return;
                }
            }
        }

        private static void TryFindPlayerNameAssets()
        {
            var playerNames = Resources.FindObjectsOfTypeAll<PlayerName>();
            foreach (var playerName in playerNames)
            {
                if (!_foundDefaultTextColor && playerName.text != null)
                {
                    DefaultTextColor = playerName.text.color;
                    _foundDefaultTextColor = true;
                }
                if (HostStarSprite == null && playerName.hostStar != null)
                {
                    var image = playerName.hostStar.GetComponentInChildren<Image>(includeInactive: true);
                    if (image != null && image.sprite != null)
                    {
                        HostStarSprite = image.sprite;
                    }
                }
                if (HostStarSprite != null && _foundDefaultTextColor)
                {
                    return;
                }
            }
        }

        private static void TryFindFallbackFont()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in fonts)
            {
                if (font.name.Contains("LiberationSans SDF"))
                {
                    FallbackFont = font;
                    return;
                }
            }
        }

        private static void TryFindCampfireIcon()
        {
            var bar = Object.FindObjectOfType<StaminaBar>();
            var icon = bar != null && bar.campfire != null
                ? bar.campfire.GetComponentInChildren<Image>(includeInactive: true) ?? bar.campfire.GetComponent<Image>()
                : null;
            if (icon != null && icon.sprite != null)
            {
                CampfireIconSprite = icon.sprite;
            }
        }
    }
}
