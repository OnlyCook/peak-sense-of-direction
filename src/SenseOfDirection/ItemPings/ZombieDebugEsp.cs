using System.Collections.Generic;
using System.Linq;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Temporary dev/QA aid, added purely to speed up hunting rare
    /// naturally-spawned zombies during the Phase 5b investigation - NOT a
    /// planned/shipped feature, remove once that investigation wraps up.
    /// Shows an always-visible edge-of-screen label for every
    /// <c>MushroomZombie</c> in the scene (through walls and off-screen, same
    /// as every other indicator here - <see cref="IndicatorManager"/>'s
    /// overlay does no occlusion check), so the maintainer doesn't have to
    /// wander a whole level hoping to stumble across one. Gated by
    /// <c>enable-zombie-debug-esp</c> (off by default, `Debug` section).
    /// Re-scans the scene once a second (cheap enough, and zombies don't
    /// spawn/despawn fast) rather than every frame, keeping one
    /// <see cref="IndicatorAnchor"/> alive per zombie between scans; each
    /// anchor's world position uses <see cref="ItemPingDetector.GetLiveCenter"/>
    /// (the same renderer-bounds-based live-position fix applied to zombie
    /// ping detection itself), so this ESP also doubles as a live check of
    /// whether that fix is actually tracking movement correctly.
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

        private readonly Dictionary<MushroomZombie, IndicatorAnchor> _anchors = new Dictionary<MushroomZombie, IndicatorAnchor>();
        private float _nextScanTime;

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
            var seen = new HashSet<MushroomZombie>();
            foreach (MushroomZombie zombie in UnityEngine.Object.FindObjectsByType<MushroomZombie>(FindObjectsSortMode.None))
            {
                if (zombie == null)
                {
                    continue;
                }
                seen.Add(zombie);
                bool isNew = !_anchors.ContainsKey(zombie);
                if (isNew)
                {
                    _anchors[zombie] = CreateAnchor(zombie);
                }

                // Raw, unprocessed numbers straight from the source -
                // deliberately bypasses the whole IndicatorManager/
                // ScreenSpaceTracker UI pipeline so a bad reading here can
                // only mean GetLiveCenter/the renderer pick itself is wrong,
                // not a screen-space projection bug. Only logged for newly
                // found zombies (not every rescan) to avoid spamming.
                if (debugLog && isNew)
                {
                    Vector3 liveCenter = ItemPingDetector.GetLiveCenter(zombie.gameObject);
                    float distanceFromLocal = Character.localCharacter != null
                        ? Vector3.Distance(Character.localCharacter.Head, zombie.transform.position) * CharacterStats.unitsToMeters
                        : -1f;
                    Plugin.Instance.Log.LogInfo(
                        $"ZombieDebugEsp: found '{zombie.gameObject.name}' activeInHierarchy={zombie.gameObject.activeInHierarchy} " +
                        $"transform.position={zombie.transform.position} ({distanceFromLocal:F1}m from local player) " +
                        $"GetLiveCenter={liveCenter}");
                }
            }

            foreach (MushroomZombie stale in _anchors.Keys.Where(z => z == null || !seen.Contains(z)).ToList())
            {
                if (debugLog)
                {
                    Plugin.Instance.Log.LogInfo("ZombieDebugEsp: a previously-found zombie is now gone (destroyed, or FindObjectsByType stopped returning it).");
                }
                IndicatorManager.Instance.UnregisterAnchor(_anchors[stale]);
                _anchors.Remove(stale);
            }
        }

        private static IndicatorAnchor CreateAnchor(MushroomZombie zombie)
        {
            GameObject zombieGo = zombie.gameObject;

            var rootGo = new GameObject("SoD.ZombieEspDebug", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(IndicatorManager.Instance.CanvasTransform, false);
            root.sizeDelta = new Vector2(160f, 30f);

            var text = rootGo.AddComponent<TextMeshProUGUI>();
            text.text = "ZOMBIE (debug)";
            text.color = Color.red;
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

            var anchor = new IndicatorAnchor(() => ItemPingDetector.GetLiveCenter(zombieGo), root)
            {
                IsActive = () => zombieGo != null && zombieGo.activeInHierarchy,
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
