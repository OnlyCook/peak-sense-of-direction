using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using UnityEngine;

namespace SenseOfDirection.CampfireIndicator
{
    /// <summary>
    /// Phase 4: points the shared edge-of-screen indicator system at the
    /// current segment's campfire (<c>MapHandler.CurrentCampfire</c>) -
    /// typically the not-yet-lit fire the player is trying to reach next, per
    /// ROADMAP.md's "always see the direction of the (typically next, or
    /// current-segment) campfire" bonus. Re-resolves <c>CurrentCampfire</c>
    /// every frame rather than hooking <c>MapHandler.GoToSegment</c> - cheap,
    /// and means the indicator naturally follows segment advancement with no
    /// cache to invalidate.
    /// </summary>
    public class CampfireIndicatorController : MonoBehaviour
    {
        private static CampfireIndicatorController _instance;

        public static CampfireIndicatorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.CampfireIndicatorController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CampfireIndicatorController>();
                }
                return _instance;
            }
        }

        private Campfire _trackedCampfire;
        private CampfireWidget _widget;

        private void Update()
        {
            NativeAssets.TryFindAll();

            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.EnableCampfireIndicator.Value || !MapHandler.ExistsAndInitialized)
            {
                Teardown();
                return;
            }

            Campfire current = MapHandler.CurrentCampfire;
            if (current != _trackedCampfire)
            {
                Teardown();
                if (current != null)
                {
                    _trackedCampfire = current;
                    _widget = CampfireWidget.Create(() => current.transform.position);
                    _widget.Anchor.IsActive = () => current != null && current.gameObject.activeInHierarchy;
                    IndicatorManager.Instance.RegisterAnchor(_widget.Anchor);
                }
            }

            if (_widget != null && current != null && Character.localCharacter != null)
            {
                float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), current.transform.position) * CharacterStats.unitsToMeters;
                _widget.Refresh(distanceMeters, cfg.ShowCampfireDistance.Value);
            }
        }

        private void Teardown()
        {
            if (_widget != null)
            {
                IndicatorManager.Instance.UnregisterAnchor(_widget.Anchor);
                _widget.Destroy();
                _widget = null;
            }
            _trackedCampfire = null;
        }
    }
}
