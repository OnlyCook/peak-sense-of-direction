using System;
using System.Collections.Generic;
using System.Linq;
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
        private ItemPingWidget _widget;
        private float _remainingSeconds;
        private bool _endingEarly;

        /// <summary>Invoked once, when this highlight starts fading out (not when it's finally destroyed).</summary>
        public Action OnFadeStart;

        public IReadOnlyList<PingableTarget> Targets => _targets;

        public static ItemPingHighlight Spawn(List<PingableTarget> targets, Color color, float durationSeconds, bool enableArrow)
        {
            var go = new GameObject("SoD.ItemPingHighlight");
            var highlight = go.AddComponent<ItemPingHighlight>();
            highlight._targets.AddRange(targets);
            highlight._remainingSeconds = durationSeconds;
            highlight._widget = ItemPingWidget.Create(highlight.GetGroupCenter, color, enableArrow);
            IndicatorManager.Instance.RegisterAnchor(highlight._widget.Anchor);
            return highlight;
        }

        /// <summary>Resets the display timer and merges in any targets not already tracked (dedup by GameObject).</summary>
        public void Refresh(List<PingableTarget> newTargets, float durationSeconds)
        {
            var known = new HashSet<GameObject>(_targets.Select(t => t.GameObject));
            foreach (PingableTarget target in newTargets)
            {
                if (known.Add(target.GameObject))
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

        private bool AnyTargetStillValid()
        {
            return _targets.Any(t => t.GameObject != null && t.GameObject.activeInHierarchy);
        }

        private void Update()
        {
            if (_endingEarly)
            {
                return;
            }

            if (!AnyTargetStillValid())
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

            PluginConfig cfg = Plugin.Instance.Cfg;
            List<PingableTarget> valid = _targets.Where(t => t.GameObject != null && t.GameObject.activeInHierarchy).ToList();
            string name = valid.Count > 1 ? $"{valid.Count}x {valid[0].GetDisplayName()}" : valid[0].GetDisplayName();
            float distanceMeters = Vector3.Distance(Character.localCharacter.Head, GetGroupCenter()) * CharacterStats.unitsToMeters;
            _widget.Refresh(name, distanceMeters, cfg.ShowItemPingName.Value, cfg.ShowItemPingDistance.Value);
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
