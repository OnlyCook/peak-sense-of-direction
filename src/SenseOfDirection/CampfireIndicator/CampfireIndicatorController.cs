using SenseOfDirection.Common;
using SenseOfDirection.Compass;
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
    /// current-segment) campfire" bonus. Re-resolves the target every frame
    /// rather than hooking <c>MapHandler.GoToSegment</c> - cheap, and means the
    /// indicator naturally follows segment advancement with no cache to
    /// invalidate.
    ///
    /// The Kiln - the final segment - has no campfire, so from the moment the run
    /// enters it there is nothing left to light and vanilla's own
    /// <c>CurrentCampfire</c> stops resolving at all (see
    /// <see cref="Common.MapTargets"/> for exactly why). For that whole last climb
    /// the indicator retargets onto the summit instead and swaps its icon/label to
    /// match - the same "where am I heading next" question, just with the answer
    /// no longer being a campfire.
    /// </summary>
    public class CampfireIndicatorController : MonoBehaviour
    {
        /// <summary>What the single widget is currently pointed at, so a change of kind rebuilds it.</summary>
        private enum TargetKind
        {
            None,
            Campfire,
            Peak,
            Portal,
        }

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

        private TargetKind _kind;
        private Campfire _trackedCampfire;
        private Transform _trackedPeak;
        private Transform _trackedPortal;
        private CampfireWidget _widget;

        /// <summary>One <see cref="Common.MapTargets.LogPeakCandidates"/> dump per entry into peak territory, not per frame.</summary>
        private bool _peakDumpLogged;

        /// <summary>Same one-shot contract as <see cref="_peakDumpLogged"/>, for the Nadir's portal never resolving.</summary>
        private bool _portalMissingLogged;

        private void Update()
        {
            NativeAssets.TryFindAll();

            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.EnableCampfireIndicator.Value || !MapHandler.ExistsAndInitialized)
            {
                Teardown();
                return;
            }

            // Segment check first, and ahead of resolving a campfire at all:
            // reaching Segment.TheKiln *is* the "last campfire got lit" event
            // (Campfire.Light_Rpc -> MapHandler.GoToSegment(advanceToSegment)),
            // observed somewhere that can't throw - The Kiln is the final segment
            // and has no campfire, so the fire that advances the run into it is
            // the last one there is, and the summit is a sub-area of that same
            // segment rather than one the run ever moves to. See
            // MapTargets.IsPastLastCampfire for the vanilla evidence.
            // The campfire-scan route below is the backstop for a run that runs
            // out of fires without the segment counter following.
            //
            // The Nadir is checked ahead of all of it: its own segment sits past
            // The Kiln (MapHandler.SetUpVoidSegment appends it to the segment
            // table when the run drops into the biome), so every "no campfire
            // left -> head for the summit" test below reads true down there, and
            // the indicator used to point up at a Peak nobody in the Void is
            // climbing towards. What matters there is the portal back out.
            if (MapTargets.IsInNadir())
            {
                _peakDumpLogged = false;
                TrackPortal(MapTargets.PortalTransform());
                RefreshWidget(cfg);
                return;
            }

            _portalMissingLogged = false;

            Campfire campfire = MapTargets.IsPastLastCampfire() ? null : MapTargets.CurrentCampfire();
            if (campfire != null)
            {
                _peakDumpLogged = false;
                TrackCampfire(campfire);
            }
            else if (MapTargets.IsPastLastCampfire() || !MapTargets.AnyUnlitCampfireRemains())
            {
                // Nothing left to light - the summit is what's left to head for.
                // The candidate dump goes out on entering this state rather than
                // on a successful retarget, so a run where no summit anchor
                // resolves at all still says why in the log instead of silently
                // showing nothing.
                if (!_peakDumpLogged)
                {
                    _peakDumpLogged = true;
                    if (cfg.EnableDebugLogging.Value)
                    {
                        MapTargets.LogPeakCandidates();
                    }
                }
                TrackPeak(MapTargets.PeakTransform());
            }
            else
            {
                // No resolvable current campfire, but unlit ones do still exist -
                // a transient state (mid-segment-transition, or a map layout this
                // mod can't read). Show nothing rather than guessing.
                Teardown();
            }

            RefreshWidget(cfg);
        }

        /// <summary>
        /// Per-frame upkeep of whatever the widget is currently pointed at: the
        /// distance line, and the icon override for the two modes that aren't
        /// showing the game's own campfire sprite (null hands
        /// <see cref="CampfireWidget.Refresh"/> back to the native campfire
        /// icon).
        /// </summary>
        private void RefreshWidget(PluginConfig cfg)
        {
            Transform tracked = _kind switch
            {
                TargetKind.Peak => _trackedPeak,
                TargetKind.Portal => _trackedPortal,
                TargetKind.Campfire => _trackedCampfire != null ? _trackedCampfire.transform : null,
                _ => null,
            };
            if (_widget == null || tracked == null || Character.localCharacter == null)
            {
                return;
            }

            Sprite icon = _kind switch
            {
                TargetKind.Peak => IconAssets.Peak,
                TargetKind.Portal => IconAssets.Portal,
                _ => null,
            };

            float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), tracked.position) * CharacterStats.unitsToMeters;
            _widget.Refresh(distanceMeters, cfg.ShowCampfireDistance.Value, icon);
        }

        private void TrackCampfire(Campfire campfire)
        {
            if (_kind == TargetKind.Campfire && campfire == _trackedCampfire)
            {
                return;
            }

            Teardown();
            _kind = TargetKind.Campfire;
            _trackedCampfire = campfire;

            Build(
                () => campfire.transform.position,
                () => campfire != null && campfire.gameObject.activeInHierarchy,
                () => Plugin.Instance.Cfg.HideCampfireName.Value ? null : CampfireLocalization.Name,
                () => null);
        }

        private void TrackPeak(Transform peak)
        {
            if (peak == null)
            {
                Teardown();
                return;
            }

            if (_kind == TargetKind.Peak && peak == _trackedPeak)
            {
                return;
            }

            Teardown();
            _kind = TargetKind.Peak;
            _trackedPeak = peak;

            if (Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                // Logged once per retarget (not per frame): which transform the
                // summit actually resolved to is the one thing about this mode
                // that can't be checked from a decompile - the progress point's
                // position only has to be right in Z for the game's own IsAtPeak
                // test, so its X/Z placement is worth eyeballing in-game.
                Plugin.Instance.Log.LogInfo(
                    $"CampfireIndicatorController: no campfire left, tracking peak '{peak.name}' at {peak.position}");
            }

            // The compass marker keeps CompassMarkerKind.Campfire (the widget
            // shape/pool entry is identical) and swaps only the sprite through
            // GetCompassIcon - the same override item pings use to show a real
            // in-game item icon, and which already draws its sprite untinted.
            Build(
                () => peak.position,
                () => peak != null,
                () => Plugin.Instance.Cfg.HideCampfireName.Value ? null : PeakLocalization.Name,
                () => IconAssets.Peak);
        }

        private void TrackPortal(Transform portal)
        {
            if (portal == null)
            {
                // MapTargets.PortalTransform throttles its scene search, so this
                // is the normal answer for the first frames after dropping into
                // the biome. Teardown rather than leaving the previous target
                // (the summit) on screen: pointing at the wrong thing is worse
                // than pointing at nothing for a moment. If it never resolves at
                // all, say so once - the same reason TrackPeak's own candidate
                // dump fires on entering the state rather than on success.
                if (!_portalMissingLogged && Plugin.Instance.Cfg.EnableDebugLogging.Value)
                {
                    _portalMissingLogged = true;
                    Plugin.Instance.Log.LogInfo(
                        "CampfireIndicatorController: in the Nadir, but no PeakGatePortal resolved yet - showing nothing.");
                }
                Teardown();
                return;
            }

            _portalMissingLogged = false;

            if (_kind == TargetKind.Portal && portal == _trackedPortal)
            {
                return;
            }

            Teardown();
            _kind = TargetKind.Portal;
            _trackedPortal = portal;

            if (Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                Plugin.Instance.Log.LogInfo(
                    $"CampfireIndicatorController: in the Nadir, tracking portal '{portal.name}' at {portal.position}");
            }

            Build(
                () => portal.position,
                () => portal != null,
                () => Plugin.Instance.Cfg.HideCampfireName.Value ? null : PortalLocalization.Name,
                () => IconAssets.Portal);
        }

        private void Build(
            System.Func<Vector3> getWorldPosition, System.Func<bool> isActive,
            System.Func<string> getLabel, System.Func<Sprite> getCompassIcon)
        {
            _widget = CampfireWidget.Create(getWorldPosition);
            _widget.Anchor.IsActive = isActive;
            _widget.Anchor.CompassKind = CompassMarkerKind.Campfire;
            _widget.Anchor.GetPlacement = () => Plugin.Instance.Cfg.CampfirePlacement.Value;
            _widget.Anchor.GetCompassLabel = getLabel;
            _widget.Anchor.GetCompassIcon = getCompassIcon;

            IndicatorManager.Instance.RegisterAnchor(_widget.Anchor);
        }

        private void Teardown()
        {
            if (_widget != null)
            {
                IndicatorManager.Instance.UnregisterAnchor(_widget.Anchor);
                _widget.Destroy();
                _widget = null;
            }
            _kind = TargetKind.None;
            _trackedCampfire = null;
            _trackedPeak = null;
            _trackedPortal = null;
        }
    }
}
