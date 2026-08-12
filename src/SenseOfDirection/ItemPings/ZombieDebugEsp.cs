using System.Collections.Generic;
using System.Linq;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Dev/QA aid, added purely to speed up hunting rare naturally-spawned
    /// things during pingability investigations - NOT a planned/shipped
    /// feature. Shows an always-visible edge-of-screen label for every one of
    /// them in the scene (through walls and off-screen, same as every other
    /// indicator here - <see cref="IndicatorManager"/>'s overlay does no
    /// occlusion check), so the maintainer doesn't have to wander a whole level
    /// hoping to stumble across one. Gated by <c>enable-zombie-debug-esp</c>
    /// (off by default, `Debug` section).
    ///
    /// Covers two kinds, both on that one setting:
    /// - <c>MushroomZombie</c>, the original reason this exists (Phase 5b).
    /// - <c>Peak.EarlyWorm</c> (PEAK 2.0), rare enough that finding one in the
    ///   wild to test with is most of the work. Worth knowing while hunting
    ///   one: an Early Worm is not a creature in the sense the others here are
    ///   - the component just decorates a regular <c>Item</c> (it holds an
    ///   <c>Item</c> reference and toggles its own hand/ground colliders by
    ///   <c>itemState</c>), so a wild one should already be pingable as an
    ///   item. The per-worm debug line below reports exactly the state that
    ///   decides whether it is - see <see cref="LogFound"/>.
    ///
    /// Re-scans the scene once a second (cheap enough, and neither kind
    /// spawns/despawns fast) rather than every frame, keeping one
    /// <see cref="IndicatorAnchor"/> alive per tracked object between scans;
    /// each anchor's world position uses
    /// <see cref="ItemPingDetector.GetLiveCenter"/> (the same
    /// renderer-bounds-based live-position fix applied to zombie ping detection
    /// itself), so this ESP also doubles as a live check of whether that fix is
    /// actually tracking movement correctly.
    /// </summary>
    public class ZombieDebugEsp : MonoBehaviour
    {
        private static ZombieDebugEsp _instance;

        public static ZombieDebugEsp Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.ZombieDebugEsp");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ZombieDebugEsp>();
                }
                return _instance;
            }
        }

        /// <summary>Keyed on the plain <c>MonoBehaviour</c> so the two tracked kinds share one registry (and one staleness pass).</summary>
        private readonly Dictionary<MonoBehaviour, IndicatorAnchor> _anchors = new Dictionary<MonoBehaviour, IndicatorAnchor>();
        private float _nextScanTime;

        private static readonly Color ZombieColor = Color.red;
        private static readonly Color WormColor = new Color(1f, 0.6f, 0.1f);

        private void Update()
        {
            if (!Plugin.Instance.Cfg.EnableZombieDebugEsp.Value)
            {
                Teardown();
                return;
            }

            if (Time.time >= _nextScanTime)
            {
                _nextScanTime = Time.time + 1f;
                Rescan();
            }
        }

        private void Rescan()
        {
            bool debugLog = Plugin.Instance.Cfg.EnableDebugLogging.Value;
            var seen = new HashSet<MonoBehaviour>();

            ScanKind<MushroomZombie>(seen, "ZOMBIE (debug)", ZombieColor, debugLog);
            ScanKind<Peak.EarlyWorm>(seen, "EARLY WORM (debug)", WormColor, debugLog);

            foreach (MonoBehaviour stale in _anchors.Keys.Where(z => z == null || !seen.Contains(z)).ToList())
            {
                if (debugLog)
                {
                    Plugin.Instance.Log.LogInfo(
                        "ZombieDebugEsp: a previously-found object is now gone (destroyed, or FindObjectsByType stopped returning it).");
                }
                IndicatorManager.Instance.UnregisterAnchor(_anchors[stale]);
                _anchors.Remove(stale);
            }
        }

        private void ScanKind<T>(HashSet<MonoBehaviour> seen, string label, Color color, bool debugLog)
            where T : MonoBehaviour
        {
            foreach (T found in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (found == null)
                {
                    continue;
                }
                seen.Add(found);
                if (!_anchors.ContainsKey(found))
                {
                    _anchors[found] = CreateAnchor(found, label, color);
                    if (debugLog)
                    {
                        LogFound(found);
                    }
                }
            }
        }

        /// <summary>
        /// Raw, unprocessed numbers straight from the source - deliberately
        /// bypasses the whole <see cref="IndicatorManager"/>/
        /// <c>ScreenSpaceTracker</c> UI pipeline, so a bad reading here can only
        /// mean <see cref="ItemPingDetector.GetLiveCenter"/>/the renderer pick
        /// itself is wrong, not a screen-space projection bug. Only logged when
        /// an object is first found (not every rescan) to avoid spamming.
        ///
        /// For an <see cref="Peak.EarlyWorm"/> it additionally reports the three
        /// things that actually decide whether a wild one is pingable *without*
        /// having to ping it: its <c>itemState</c>, whether its GameObject is
        /// active (the registry only holds active objects), and whether
        /// <see cref="PingableRegistry"/> is already carrying its <c>Item</c>.
        /// A worm that shows up here with <c>inRegistry=True</c> is pingable and
        /// needs no further work.
        /// </summary>
        private static void LogFound(MonoBehaviour found)
        {
            Vector3 liveCenter = ItemPingDetector.GetLiveCenter(found.gameObject);
            float distanceFromLocal = Character.localCharacter != null
                ? Vector3.Distance(Character.localCharacter.Head, found.transform.position) * CharacterStats.unitsToMeters
                : -1f;

            string extra = string.Empty;
            if (found is Peak.EarlyWorm worm)
            {
                Item item = worm.item != null ? worm.item : worm.GetComponentInParent<Item>();
                extra = item != null
                    ? $" itemState={item.itemState} itemActive={item.gameObject.activeInHierarchy} "
                        + $"inRegistry={PingableRegistry.Instance.Items.Contains(item)}"
                    : " (no Item found on it at all - would explain it not being pingable)";
            }

            Plugin.Instance.Log.LogInfo(
                $"ZombieDebugEsp: found {found.GetType().Name} '{found.gameObject.name}' " +
                $"activeInHierarchy={found.gameObject.activeInHierarchy} " +
                $"transform.position={found.transform.position} ({distanceFromLocal:F1}m from local player) " +
                $"GetLiveCenter={liveCenter}{extra}");
        }

        private static IndicatorAnchor CreateAnchor(MonoBehaviour tracked, string label, Color color)
        {
            GameObject trackedGo = tracked.gameObject;

            var rootGo = new GameObject("SoD.CreatureEspDebug", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(IndicatorManager.Instance.CanvasTransform, false);
            root.sizeDelta = new Vector2(200f, 30f);

            var text = rootGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22f;
            if (NativeAssets.Font != null)
            {
                text.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null)
            {
                text.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            var anchor = new IndicatorAnchor(() => ItemPingDetector.GetLiveCenter(trackedGo), root)
            {
                IsActive = () => trackedGo != null && trackedGo.activeInHierarchy,
            };
            IndicatorManager.Instance.RegisterAnchor(anchor);
            return anchor;
        }

        private void Teardown()
        {
            foreach (IndicatorAnchor anchor in _anchors.Values)
            {
                IndicatorManager.Instance.UnregisterAnchor(anchor);
            }
            _anchors.Clear();
        }
    }
}
