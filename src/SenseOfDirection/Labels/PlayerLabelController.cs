using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using UnityEngine;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Owns one <see cref="PlayerLabel"/> per non-local, non-bot <c>Character</c>
    /// currently in the scene, registering/unregistering them as
    /// <c>Character.Awake</c>/<c>OnDestroy</c> fire (see
    /// <see cref="PlayerLabelPatches"/>), and drives the Toggle/AlwaysOn/Hold
    /// display-state key logic plus per-frame content refresh: distance gate,
    /// dead/unconscious/host icons, color, and the fade crossfade with
    /// vanilla's own name label (RESEARCH.md Q1's `IsLookedAt` distance/cone
    /// formula, reimplemented here reading each character's own live
    /// `IsLookedAt` instance directly - our label fades in exactly as
    /// vanilla's fades out).
    ///
    /// Positioning itself is <see cref="IndicatorManager"/>'s job - each label
    /// registers an <see cref="IndicatorAnchor"/> there and this class never
    /// touches screen-space math directly.
    /// </summary>
    public class PlayerLabelController : MonoBehaviour
    {
        private static PlayerLabelController _instance;

        public static PlayerLabelController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PlayerLabelController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerLabelController>();
                }
                return _instance;
            }
        }

        private class Entry
        {
            public PlayerLabel Label;
            public IsLookedAt LookedAt; // may be null (e.g. local player edge case); Head is the fallback anchor.
        }

        private readonly Dictionary<Character, Entry> _entries = new Dictionary<Character, Entry>();

        private bool _toggleVisible;
        private bool _toggleKeyWasDown;
        private float _holdReleaseUntil;
        private bool _labelsVisible;

        public void RegisterCharacter(Character character)
        {
            if (character == null || character.isBot || character == Character.localCharacter)
            {
                return;
            }
            if (_entries.ContainsKey(character))
            {
                return;
            }

            IsLookedAt lookedAt = character.GetComponentInChildren<IsLookedAt>(includeInactive: true);

            Vector3 AnchorPosition()
            {
                // A dead character's own bodypart transforms (including
                // whatever playerNamePos is parented under) become an
                // unreliable moving/despawning target some time after death
                // - freeze the label at LastLivingPosition instead of
                // following it there, same fix as CharacterPositions.
                if (character.data.dead)
                {
                    return character.LastLivingPosition;
                }
                if (lookedAt != null && lookedAt.playerNamePos != null)
                {
                    return lookedAt.playerNamePos.position;
                }
                return character.Head;
            }

            var label = PlayerLabel.Create(AnchorPosition);
            label.Anchor.IsActive = () => character != null && character.gameObject.activeInHierarchy;

            label.Anchor.CompassKind = CompassMarkerKind.Player;
            label.Anchor.GetPlacement = () => Plugin.Instance.Cfg.PlayerLabelPlacement.Value;
            label.Anchor.GetCompassColor = () => Plugin.Instance.Cfg.UseCharacterColor.Value
                ? character.refs.customization.PlayerColor
                : NativeAssets.DefaultTextColor;
            label.Anchor.GetCompassLabel = () => character.characterName;
            label.Anchor.GetIsDead = () => character.data.dead;
            label.Anchor.GetIsUnconscious = () => character.data.passedOut || character.data.fullyPassedOut;
            // Compass marker follows the same toggle-key/AlwaysOn/Hold visibility
            // and max-distance gate as the off-screen label itself (_labelsVisible,
            // computed once per frame in Update) - only the vanilla-label crossfade
            // (IsNativeLabelVisible) doesn't apply here, since there's no vanilla
            // compass to hand off to/from.
            label.Anchor.IsCompassVisible = () => _labelsVisible
                && Vector3.Distance(CharacterPositions.LocalViewpoint(), CharacterPositions.EffectivePosition(character)) * CharacterStats.unitsToMeters <= Plugin.Instance.Cfg.PlayerLabelMaxDistanceMeters.Value;

            IndicatorManager.Instance.RegisterAnchor(label.Anchor);

            _entries[character] = new Entry { Label = label, LookedAt = lookedAt };
        }

        public void UnregisterCharacter(Character character)
        {
            if (character == null)
            {
                return;
            }
            if (_entries.TryGetValue(character, out Entry entry))
            {
                IndicatorManager.Instance.UnregisterAnchor(entry.Label.Anchor);
                entry.Label.Destroy();
                _entries.Remove(character);
            }
        }

        private void Update()
        {
            NativeAssets.TryFindAll();

            PluginConfig cfg = Plugin.Instance.Cfg;
            _labelsVisible = ComputeLabelsVisible(cfg);

            if (Character.localCharacter == null)
            {
                return;
            }

            foreach (var pair in _entries)
            {
                Character character = pair.Key;
                if (character == null)
                {
                    continue;
                }
                Entry entry = pair.Value;
                PlayerLabel label = entry.Label;
                CharacterData data = character.data;

                float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), CharacterPositions.EffectivePosition(character)) * CharacterStats.unitsToMeters;
                bool isHost = character.photonView.Owner.IsMasterClient;
                bool isDead = data.dead;
                bool isUnconscious = data.passedOut || data.fullyPassedOut;

                Color nameColor = cfg.UseCharacterColor.Value
                    ? character.refs.customization.PlayerColor
                    : NativeAssets.DefaultTextColor;

                float targetAlpha = ComputeTargetAlpha(entry.LookedAt, distanceMeters, isDead, cfg) ? 1f : 0f;

                label.Refresh(
                    character.characterName, distanceMeters, isHost, isDead, isUnconscious,
                    nameColor, cfg.PlayerLabelNameFontSize.Value, cfg.PlayerLabelDistanceFontSize.Value, targetAlpha,
                    cfg.ShowPlayerLabelDistance.Value, cfg.ShowStatusBadges.Value);
            }
        }

        private bool ComputeTargetAlpha(IsLookedAt lookedAt, float distanceMeters, bool isDead, PluginConfig cfg)
        {
            if (!_labelsVisible)
            {
                return false;
            }
            if (distanceMeters > cfg.PlayerLabelMaxDistanceMeters.Value)
            {
                return false;
            }
            // A dead player's native label doesn't exist at all - always show
            // ours (still gated above by max distance + segment-active, the
            // latter via the anchor's hard IsActive check).
            if (isDead)
            {
                return true;
            }
            return !IsNativeLabelVisible(lookedAt, cfg);
        }

        /// <summary>
        /// Reimplements `IsLookedAt.Update`'s own visibility formula
        /// (RESEARCH.md Q1) so our label can fade in at exactly the point
        /// vanilla's own fades out. Reads the thresholds (and the check
        /// position) directly off the character's own live `IsLookedAt`
        /// instance rather than a hardcoded/duplicated copy, since the
        /// decompiled C# field defaults aren't guaranteed to match whatever
        /// values are actually serialized onto the live prefab - this way
        /// Sense of Direction's crossfade always matches vanilla exactly,
        /// even if that ever changes. Skipped entirely (treated as "never
        /// visible") when replace-vanilla-labels is on, since there's no
        /// vanilla label to hand off to/from in that mode, or if no
        /// `IsLookedAt` was found for this character at all.
        /// </summary>
        private bool IsNativeLabelVisible(IsLookedAt lookedAt, PluginConfig cfg)
        {
            if (cfg.ReplaceVanillaLabels.Value)
            {
                return false;
            }
            if (lookedAt == null)
            {
                return false;
            }
            if (Character.localCharacter.data.isBlind)
            {
                return false;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }
            Transform camTransform = camera.transform;
            Vector3 checkPos = lookedAt.transform.position;

            float visibleDistance = lookedAt.visibleDistance;
            float visibleAngle = lookedAt.visibleAngle;
            float angleDistRatio = lookedAt.angleDistRatio;

            float distance = Vector3.Distance(camTransform.position, checkPos);
            float angle = Vector3.Angle(camTransform.forward, checkPos - camTransform.position);

            return distance < visibleDistance
                   && angle < visibleAngle + (visibleDistance - distance) / visibleDistance * angleDistRatio;
        }

        private bool ComputeLabelsVisible(PluginConfig cfg)
        {
            if (!cfg.EnablePlayerLabels.Value)
            {
                return false;
            }

            switch (cfg.PlayerLabelDisplayMode.Value)
            {
                case LabelDisplayMode.AlwaysOn:
                    return true;

                case LabelDisplayMode.Toggle:
                {
                    // Deliberately not Input.GetKeyDown here - Unity's legacy
                    // Input Manager has a long-documented bug where its own
                    // internal down-edge detection can silently miss a key
                    // press when another key (e.g. a WASD movement key) is
                    // already held that same frame, so the toggle key only
                    // ever registered while standing still. Doing the edge
                    // detection ourselves off the plain (unaffected) GetKey
                    // level-state read avoids that bug entirely.
                    bool keyDownNow = Input.GetKey(cfg.PlayerLabelToggleKey.Value);
                    if (keyDownNow && !_toggleKeyWasDown)
                    {
                        _toggleVisible = !_toggleVisible;
                    }
                    _toggleKeyWasDown = keyDownNow;
                    return _toggleVisible;
                }

                case LabelDisplayMode.Hold:
                    if (Input.GetKey(cfg.PlayerLabelToggleKey.Value))
                    {
                        // Set on every held frame (including the press frame
                        // itself), so a quick tap is already covered by this
                        // same timer - no separate "minimum time" needed.
                        _holdReleaseUntil = Time.time + cfg.HoldShownDuration.Value;
                        return true;
                    }
                    return Time.time < _holdReleaseUntil;

                default:
                    return false;
            }
        }
    }
}
