using System.Collections;
using System.Collections.Generic;
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
    ///   for glyphs it hasn't rendered yet - both pools are now warmed with a
    ///   full alphabet/digit/punctuation sample rather than one throwaway word,
    ///   since a real item/creature/hazard name can need any of those glyphs,
    ///   not just the handful "WARMUP" itself covered.
    /// - <see cref="PingRipple"/>'s sphere mesh and material.
    /// - The registry's first scene sweep (<see cref="PingableRegistry"/>).
    /// - The ping marker's own material: <c>Pings.PointPingerPatches.SpawnPingNow</c>
    ///   instantiates a clone of the local character's own body material and
    ///   sets a shader float on it for every single ping - first use of that
    ///   shader/keyword combination is a variant compile, same class of hitch
    ///   as the font atlas one above, and one this file didn't touch at all
    ///   before.
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

            WarmPingMaterial();
            WarmItemPingDetection();

            if (Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                Plugin.Instance.Log.LogInfo(
                    $"PingPrewarm: built {PrewarmedItemPingWidgets} item-ping widgets, {PrewarmedPingWidgets} ping widgets, "
                    + "their compass markers, the ripple mesh/material and the ping icons.");
            }
        }

        /// <summary>
        /// Mirrors <c>Pings.PointPingerPatches.SpawnPingNow</c>'s own
        /// <c>Instantiate(character.refs.mainRenderer.sharedMaterial)</c> +
        /// <c>SetFloat("_Opacity", ...)</c> exactly, then throws the clone away -
        /// every ping does this, but the very first time this exact
        /// shader/keyword combination is instantiated is a shader variant
        /// compile, not just a cheap clone, and that cost otherwise landed on
        /// whichever ping happened to be first rather than here.
        /// </summary>
        private static void WarmPingMaterial()
        {
            Material sharedMaterial = Character.localCharacter.refs.mainRenderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                return;
            }
            Material warm = UnityEngine.Object.Instantiate(sharedMaterial);
            warm.SetFloat("_Opacity", 1f);
            UnityEngine.Object.Destroy(warm);
        }

        /// <summary>
        /// Same call shape <see cref="ItemPingSpawner.SpawnFor"/> makes into
        /// <see cref="ItemPingDetector.FindNear"/> on every real ping, but with
        /// every radius zeroed out so it can never actually match a real
        /// item/luggage/creature/hazard - existing purely to pay this (large,
        /// closure-heavy) method's JIT cost, and its static buffers'/
        /// <c>NamedHazards</c> table's one-time initialization, once and
        /// silently rather than on whichever ping happens to be the first one
        /// that actually finds something to highlight.
        /// </summary>
        private static void WarmItemPingDetection()
        {
            Vector3 point = Character.localCharacter.Head;
            List<PingableTarget> found = ItemPingDetector.FindNear(
                point, 0f, 0f, 0f, point, Vector3.forward, 0f, 0f, includeCreatures: true);
            found.Clear();
        }
    }
}
