using System;
using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.Pings;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Drives one on-screen item-ping highlight for the duration of
    /// <c>item-ping-duration-seconds</c>: tracks a group of one or more
    /// <see cref="PingableTarget"/>s (grouped by display name when
    /// <c>enable-item-ping-grouping</c> is on), positions the widget at the
    /// live average center of whichever targets are still valid, and fades
    /// out (reusing <see cref="Pings.PingWidgetFadeOut"/> - it only needs a
    /// <c>CanvasGroup</c>/<c>IndicatorAnchor</c> pair, nothing ping-specific)
    /// early if every target in the group stops being valid (picked up /
    /// luggage opened) before the timer runs out. Lives on its own throwaway
    /// GameObject rather than being attached onto the item/luggage's own
    /// GameObject, so it survives that GameObject being deactivated/destroyed
    /// out from under it (that's exactly the "no longer valid" signal it
    /// watches for) instead of being torn down along with it.
    ///
    /// <see cref="ItemPingSpawner"/> keeps a target-GameObject -> highlight
    /// registry so re-pinging an already-highlighted item calls
    /// <see cref="Refresh"/> (reset the timer, merge in any newly-detected
    /// targets) instead of stacking a second overlapping label on top - the
    /// maintainer's top complaint about the first pass of this feature.
    /// <see cref="OnFadeStart"/> lets the spawner drop its registry entries
    /// the moment a highlight starts fading, rather than only once it's
    /// finally destroyed, so a re-ping during the brief fade window is free
    /// to spawn a fresh highlight instead of trying to revive a dying one.
    /// </summary>
    public class ItemPingHighlight : MonoBehaviour
    {
        private readonly List<PingableTarget> _targets = new List<PingableTarget>();

        /// <summary>Rebuilt in place every frame instead of LINQ-ing out a fresh list - see <see cref="Update"/>.</summary>
        private readonly List<PingableTarget> _valid = new List<PingableTarget>();

        private readonly HashSet<GameObject> _knownTargets = new HashSet<GameObject>();

        private ItemPingWidget _widget;
        private float _remainingSeconds;
        private bool _endingEarly;

        /// <summary>Full label ("COCONUT" / "3x COCONUT"), and the bare count ("3x", null for a single target) that's left of it when the name itself is suppressed - see <see cref="_currentLabel"/>.</summary>
        private string _currentDisplayName;
        private string _currentCountOnly;

        /// <summary>
        /// What's actually shown right now, on both the widget and the compass
        /// marker (they must agree - a name hidden on one and shown on the other
        /// would just look like a bug).
        ///
        /// Normally <see cref="_currentDisplayName"/>; with
        /// <c>only-show-item-ping-name-without-icon</c> on, it drops to
        /// <see cref="_currentCountOnly"/> for anything already showing its own
        /// in-game icon, since the icon says what the thing is far more directly
        /// than its name does. The count survives that: it's the one part of
        /// the label an icon *can't* convey ("3x" vs. one bandage), so hiding
        /// it along with the name would lose real information rather than a
        /// redundant restatement. Null/empty means no name line at all - a
        /// single pinged item with an icon, which is the whole point of the
        /// setting.
        /// </summary>
        private string _currentLabel;

        /// <summary>
        /// The pinged thing's own in-game icon while <c>use-native-item-ping-
        /// icons</c> is on and it has one (items and the campfire do; luggage,
        /// creatures and hazards have no icon anywhere in the game). Null means
        /// the widget/compass marker fall back to the mod's own generic
        /// item-ping icon. Re-read each frame off the group's first still-valid
        /// target - it can genuinely change while the highlight is up (the
        /// front item of a group gets picked up and the next one takes over the
        /// label), and resolving it is a dictionary hit, not an asset load (see
        /// <see cref="Common.NativeIconCache"/>).
        /// </summary>
        private Sprite _currentIcon;

        /// <summary>
        /// Inputs the display name was last built from. The name only changes
        /// when the group's size or its first target's name does (an item being
        /// picked up, a cooked-state rename), which is rare - but building it
        /// meant an uppercase conversion plus, for a group, an interpolated
        /// string, every frame for every live highlight. Cached so a steady
        /// highlight allocates nothing per frame.
        /// </summary>
        private int _lastValidCount = -1;
        private string _lastBaseName;

        /// <summary>Invoked once, when this highlight starts fading out (not when it's finally destroyed).</summary>
        public Action OnFadeStart;

        public IReadOnlyList<PingableTarget> Targets => _targets;

        public static ItemPingHighlight Spawn(List<PingableTarget> targets, Color color, float durationSeconds, bool enableArrow)
        {
            var go = new GameObject("SoD.ItemPingHighlight");
            var highlight = go.AddComponent<ItemPingHighlight>();
            foreach (PingableTarget target in targets)
            {
                if (highlight._knownTargets.Add(target.GameObject))
                {
                    highlight._targets.Add(target);
                }
            }
            highlight._remainingSeconds = durationSeconds;
            highlight._widget = ItemPingWidget.Rent(highlight.GetGroupCenter, color, enableArrow);

            highlight._widget.Anchor.CompassKind = CompassMarkerKind.ItemPing;
            highlight._widget.Anchor.GetDisplayMode = () => Plugin.Instance.Cfg.ItemPingsCompassDisplayMode.Value;
            highlight._widget.Anchor.GetCompassColor = () => color;
            // _currentLabel, not _currentDisplayName: the compass marker shows
            // the same icon the widget does, so the name is redundant there for
            // exactly the same targets (only-show-item-ping-name-without-icon).
            highlight._widget.Anchor.GetCompassLabel = () => highlight._currentLabel;
            highlight._widget.Anchor.GetCompassIcon = () => highlight._currentIcon;

            // Before the anchor is registered (i.e. before anything can render
            // this highlight), not left to the first Update - see ApplyVisuals.
            highlight.CollectValidTargets();
            if (highlight._valid.Count > 0 && Character.localCharacter != null)
            {
                highlight.ApplyVisuals();
            }

            IndicatorManager.Instance.RegisterAnchor(highlight._widget.Anchor);
            return highlight;
        }

        /// <summary>Resets the display timer and merges in any targets not already tracked (dedup by GameObject).</summary>
        public void Refresh(List<PingableTarget> newTargets, float durationSeconds)
        {
            // _knownTargets is maintained as targets are added rather than
            // rebuilt from _targets on every merge - a re-ping of an already
            // highlighted group lands here, and that's on the ping path.
            foreach (PingableTarget target in newTargets)
            {
                if (_knownTargets.Add(target.GameObject))
                {
                    _targets.Add(target);
                }
            }
            _remainingSeconds = durationSeconds;
        }

        private Vector3 GetGroupCenter()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (PingableTarget target in _targets)
            {
                if (target.GameObject == null || !target.GameObject.activeInHierarchy)
                {
                    continue;
                }
                sum += target.GetCenter();
                count++;
            }
            return count > 0 ? sum / count : transform.position;
        }

        /// <summary>Refills <see cref="_valid"/> with the targets that still exist and are still active.</summary>
        private void CollectValidTargets()
        {
            _valid.Clear();
            foreach (PingableTarget target in _targets)
            {
                if (target.GameObject != null && target.GameObject.activeInHierarchy)
                {
                    _valid.Add(target);
                }
            }
        }

        private void Update()
        {
            if (_endingEarly)
            {
                return;
            }

            // One liveness pass per frame, reusing the same list - this used to
            // be a LINQ Any() followed by a Where().ToList(), i.e. two walks
            // plus a new list and two enumerators every frame, for every live
            // highlight. Harmless for one, but "ping a pile of items" spawns a
            // highlight per group and they all run this at once.
            CollectValidTargets();
            if (_valid.Count == 0)
            {
                BeginFadeOut();
                return;
            }

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f)
            {
                BeginFadeOut();
                return;
            }

            if (Character.localCharacter == null)
            {
                return;
            }

            ApplyVisuals();
        }

        /// <summary>
        /// Pushes the group's current icon/label/distance into the widget (and,
        /// via the fields the anchor's getters close over, into the compass
        /// marker). Expects <see cref="_valid"/> to have been refilled already.
        ///
        /// Called from <see cref="Spawn"/> as well as <see cref="Update"/>,
        /// which is what keeps a brand-new highlight from being visible for one
        /// frame in its default state before the first Update resolves what it
        /// actually shows: the widget is rented (and shown) during the ping RPC,
        /// but MonoBehaviour.Update doesn't run until the next frame, so that
        /// first rendered frame used to show the generic diamond and an
        /// unsuppressed name, then snap to the item's real icon (and drop the
        /// name under only-show-item-ping-name-without-icon) - read in-game as a
        /// flicker on every first ping of an item.
        /// </summary>
        private void ApplyVisuals()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            // Game's own pickup prompts/UI show item names in all caps
            // (RESEARCH.md/ISSUES.md) - native Item.GetItemName()/Mob
            // GameObject-name fallbacks come through in mixed/native case, so
            // normalize here rather than at each individual capture site.
            string baseName = _valid[0].GetDisplayName();
            if (_valid.Count != _lastValidCount || !string.Equals(baseName, _lastBaseName, StringComparison.Ordinal))
            {
                _lastValidCount = _valid.Count;
                _lastBaseName = baseName;
                string upper = baseName.ToUpperInvariant();
                _currentCountOnly = _valid.Count > 1 ? $"{_valid.Count}x" : null;
                _currentDisplayName = _valid.Count > 1 ? $"{_currentCountOnly} {upper}" : upper;
            }

            Func<Sprite> getIcon = _valid[0].GetNativeIcon;
            _currentIcon = cfg.UseNativeItemPingIcons.Value && getIcon != null ? getIcon() : null;

            _currentLabel = _currentIcon != null && cfg.OnlyShowItemPingNameWithoutIcon.Value
                ? _currentCountOnly
                : _currentDisplayName;
            bool showName = cfg.ShowItemPingName.Value && !string.IsNullOrEmpty(_currentLabel);

            float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), GetGroupCenter()) * CharacterStats.unitsToMeters;
            _widget.Refresh(_currentLabel, distanceMeters, showName, cfg.ShowItemPingDistance.Value, _currentIcon);
        }

        private void BeginFadeOut()
        {
            _endingEarly = true;
            OnFadeStart?.Invoke();
            PingWidgetFadeOut.Begin(_widget.CanvasGroup, _widget.Anchor);
            Destroy(gameObject, 1f);
        }
    }
}
