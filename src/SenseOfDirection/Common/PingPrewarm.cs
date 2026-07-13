using System.Collections;
using SenseOfDirection.Compass;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using SenseOfDirection.Pings;
using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Does, once and up front, everything the ping path would otherwise do
    /// lazily the first time someone pings - ISSUES.md asks for pinging never
    /// to stutter, "except if you must the first time to initialize it, but
    /// then try to do that in advance still", which is exactly this.
    ///
    /// What was deferred to the first ping before:
    /// - Icon sprites (<see cref="IconAssets"/>) are loaded on first access,
    ///   and loading one means decoding an embedded PNG and uploading a texture
    ///   with mipmaps. The item-ping crosshair and the off-screen arrow were
    ///   both first touched by the first ping's own widget.
    /// - The first ping/item-ping widget and compass marker of a session build
    ///   TMP text objects, which pull in font materials and grow the font atlas
    ///   for glyphs it hasn't rendered yet.
    /// - <see cref="PingRipple"/>'s sphere mesh and material.
    /// - The registry's first scene sweep (<see cref="PingableRegistry"/>).
    ///
    /// Runs once the game is actually in a level with its own UI up (the fonts
    /// this borrows from PEAK's HUD don't exist before that - see
    /// <see cref="NativeAssets"/>), then gets out of the way.
    /// </summary>
    public class PingPrewarm : MonoBehaviour
    {
        /// <summary>
        /// How many of each pooled widget to build ahead of time. Sized for
        /// "someone pings a pile of loot": one item-ping widget/compass marker
        /// per pinged group, plus a few pings in flight from other players at
        /// once. The pools grow on demand anyway - this is just how much is
        /// free by the time it's first needed.
        /// </summary>
        private const int PrewarmedItemPingWidgets = 8;
        private const int PrewarmedPingWidgets = 4;

        private static PingPrewarm _instance;

        public static PingPrewarm Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PingPrewarm");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PingPrewarm>();
                }
                return _instance;
            }
        }

        private bool _done;

        private void Start()
        {
            StartCoroutine(PrewarmWhenReady());
        }

        private IEnumerator PrewarmWhenReady()
        {
            while (!_done)
            {
                // NativeAssets only finds PEAK's own font/material once its HUD
                // exists, so prewarming any earlier would bake TMP's default
                // font into every pooled widget and force a re-layout later.
                if (Character.localCharacter != null && NativeAssets.TryFindAll())
                {
                    Prewarm();
                    _done = true;
                    yield break;
                }
                yield return null;
            }
        }

        private void Prewarm()
        {
            // Touching each sprite property is what actually loads it.
            _ = IconAssets.ItemPingDiamond;
            _ = IconAssets.PingArrow;
            _ = IconAssets.PingRing;
            _ = IconAssets.PlayerFace;

            PingRipple.EnsureAssets();

            ItemPingWidget.Prewarm(PrewarmedItemPingWidgets);
            PingWidget.Prewarm(PrewarmedPingWidgets);

            RectTransform markerRoot = CompassManager.Instance.MarkerRoot;
            if (markerRoot != null)
            {
                CompassMarkerWidget.Prewarm(markerRoot, CompassMarkerKind.ItemPing, PrewarmedItemPingWidgets);
                CompassMarkerWidget.Prewarm(markerRoot, CompassMarkerKind.Ping, PrewarmedPingWidgets);
            }

            // The first sweep would otherwise be whichever one the periodic
            // loop happened to run - which, for a ping in the first seconds of
            // a level, could be the ping itself waiting on an empty registry.
            PingableRegistry.Instance.Rebuild();

            if (Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                Plugin.Instance.Log.LogInfo(
                    $"PingPrewarm: built {PrewarmedItemPingWidgets} item-ping widgets, {PrewarmedPingWidgets} ping widgets, "
                    + "their compass markers, the ripple mesh/material and the ping icons.");
            }
        }
    }
}
