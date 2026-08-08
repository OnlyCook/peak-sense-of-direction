using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Lazily discovers a few of PEAK's own UI assets at runtime, since Unity
    /// asset references (font/material/sprite) aren't visible in the game's
    /// decompiled IL - see RESEARCH.md Q2/Q7. Retried until each is found,
    /// since the native UI may not have created any instances yet when Sense
    /// of Direction first looks (e.g. still on the main menu).
    ///
    /// A retry is NOT cheap, which an earlier version of this comment claimed
    /// and <see cref="TryFindAll"/>'s callers took at face value: three of them
    /// (<see cref="PlayerLabelController"/>,
    /// <see cref="CampfireIndicator.CampfireIndicatorController"/> and
    /// <see cref="Compass.CompassManager"/>) call it unconditionally at the top
    /// of their own <c>Update</c>, before any enable/level check, so every
    /// unresolved lookup ran three times a frame forever. Each one is a full
    /// <c>Resources.FindObjectsOfTypeAll</c> sweep over every loaded object
    /// *and asset* - and <see cref="TryFindFont"/> additionally touches
    /// <c>materialForRendering</c> on every text it walks. Measured on the main
    /// menu that came to ~5.2ms per <c>TryFindAll</c>, ~15.6ms per frame,
    /// ~890ms of every wall-clock second: the mod on its own was eating an
    /// entire 60fps frame budget while the game sat on its title screen.
    ///
    /// Two things keep that from happening, both in <see cref="TryFindAll"/>:
    /// the scans only run inside a level at all, and even there they're
    /// throttled rather than per-frame. See its own comments.
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

        /// <summary>
        /// Everything <see cref="TryFindAll"/> gates on is resolved, so it has
        /// nothing to do but say so - the state the mod spends an entire run
        /// in, and the reason a resolved <see cref="TryFindAll"/> is free.
        ///
        /// Re-derived rather than latched: a latch would also stop the scans
        /// from ever running again, and the whole point of the original
        /// "retry until found" design is that a reference going null again
        /// (an asset unloaded on a scene change) re-resolves itself.
        /// </summary>
        private static bool AllFound =>
            Font != null && OutlineMaterial != null && HostStarSprite != null
            && _foundDefaultTextColor && CampfireIconSprite != null;

        /// <summary>
        /// How long an unresolved lookup waits before being retried. Shared
        /// across every caller (it's wall-clock, not per-caller), so the three
        /// per-frame ones cost one scan between them instead of three each.
        ///
        /// Short enough that nothing ends up visibly unstyled: every in-world
        /// widget (player label, campfire, ping, item ping, compass tick and
        /// marker) re-reads <see cref="Font"/>/<see cref="OutlineMaterial"/> on
        /// each refresh rather than baking them in at construction, so one
        /// built inside the retry window restyles itself the moment the scan
        /// catches up.
        /// </summary>
        private const float RetryIntervalSeconds = 0.25f;

        private static float _lastScanTime = float.NegativeInfinity;

        /// <summary>Call periodically (e.g. once per frame) until this returns true.</summary>
        public static bool TryFindAll()
        {
            if (AllFound)
            {
                return true;
            }

            // One throttle for the whole method, checked before any scan and
            // shared across callers. Inside a level the assets do eventually
            // resolve, but "eventually" can be several seconds (the HUD builds
            // asynchronously), and until then every attempt is another
            // scene-wide sweep. Four attempts a second is plenty to catch the
            // HUD coming up without paying for it every frame.
            if (Throttled())
            {
                return false;
            }

            // The two cheap ones first, and above the in-level gate below: both
            // resolve on their very first call (KeyboardSprites via a direct
            // Resources.Load, FallbackFont via a name match against TMP's own
            // always-loaded stock font), including on the main menu, so they
            // never become a repeating cost. Deliberately not folded into the
            // return value - neither is required by any existing caller, so
            // there's no real "not ready yet" case to gate on.
            if (KeyboardSprites == null)
            {
                KeyboardSprites = InputSpriteData.Instance != null ? InputSpriteData.Instance.keyboardSprites : null;
            }
            if (FallbackFont == null)
            {
                TryFindFallbackFont();
            }

            // Everything below is borrowed off PEAK's in-game HUD - the
            // outlined text style, a PlayerName label, the stamina bar's
            // campfire icon. None of it exists before there's a local
            // character, which was measured, not assumed: on the main menu all
            // three scans ran ~170 times a second for a whole session and never
            // once resolved. So outside a level they're pure waste, and an
            // expensive kind - see this class' own summary.
            if (Character.localCharacter == null)
            {
                return false;
            }

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

            return AllFound;
        }

        /// <summary>
        /// Whether the last scan was recent enough that this call should skip
        /// doing another. Stamps the clock on the way through, so the first
        /// caller in a window does the work and everyone after it is turned
        /// away until the window is up.
        /// </summary>
        private static bool Throttled()
        {
            if (Time.unscaledTime - _lastScanTime < RetryIntervalSeconds)
            {
                return true;
            }
            _lastScanTime = Time.unscaledTime;
            return false;
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
