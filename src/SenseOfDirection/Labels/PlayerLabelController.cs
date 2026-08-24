using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using UnityEngine;
using Zorro.Core;

namespace SenseOfDirection.Labels
{
    // Owns one PlayerLabel per non-local, non-bot Character in the scene (registered/
    // unregistered via PlayerLabelPatches), and drives the Toggle/AlwaysOn/Hold display
    // logic plus per-frame refresh (distance gate, dead/unconscious/host icons, color,
    // and the fade crossfade with vanilla's own name label - see RESEARCH.md Q1).
    // Positioning is IndicatorManager's job; this class never touches screen-space math.
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
            public IsLookedAt LookedAt; // may be null; Head is the fallback anchor

            // set by CullDistantDeadLabels when this player died far from a campfire that
            // just got lit; UpdateImpl un-culls once they're revived. Dead only, never unconscious.
            public bool Culled;
        }

        private readonly Dictionary<Character, Entry> _entries = new Dictionary<Character, Entry>();

        private bool _toggleVisible;
        private bool _toggleKeyWasDown;
        private float _holdReleaseUntil;
        private bool _labelsVisible;

        // Backs show-skeleton. Built lazily on first use so the overlay canvas is only
        // touched once someone turns the feature on. Driven from LateUpdate, not Update
        // like the labels, since it projects bone positions with no lag-hiding smoothing
        // and needs the camera to have already finished moving for the frame.
        private PlayerSkeletonEsp _skeletonEsp;

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
                // a dead character's bodypart transforms become an unreliable moving/
                // despawning target - freeze at LastLivingPosition instead, same as CharacterPositions
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

            var entry = new Entry { LookedAt = lookedAt };

            var label = PlayerLabel.Create(AnchorPosition);
            label.Anchor.IsActive = () => character != null && character.gameObject.activeInHierarchy && !entry.Culled;

            label.Anchor.CompassKind = CompassMarkerKind.Player;
            label.Anchor.GetPlacement = () => Plugin.Instance.Cfg.PlayerLabelPlacement.Value;
            label.Anchor.AllowOffScreen = () => Plugin.Instance.Cfg.EnablePlayerLabelOffScreenIndicator.Value;
            label.Anchor.GetCompassColor = () => Plugin.Instance.Cfg.UseCharacterColor.Value
                ? character.refs.customization.PlayerColor
                : NativeAssets.DefaultTextColor;
            label.Anchor.GetCompassLabel = () => character.characterName;
            label.Anchor.GetIsDead = () => character.data.dead;
            label.Anchor.GetIsUnconscious = () => character.data.passedOut || character.data.fullyPassedOut;
            // same toggle-key/AlwaysOn/Hold + max-distance gate as the label; no vanilla-label
            // crossfade here since there's no vanilla compass to hand off to/from
            label.Anchor.IsCompassVisible = () => _labelsVisible && !entry.Culled
                && Vector3.Distance(CharacterPositions.LocalViewpoint(), CharacterPositions.EffectivePosition(character)) * CharacterStats.unitsToMeters <= Plugin.Instance.Cfg.PlayerLabelMaxDistanceMeters.Value;

            IndicatorManager.Instance.RegisterAnchor(label.Anchor);

            entry.Label = label;
            _entries[character] = entry;
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

        // Called by SceneResetCoordinator on every scene load - unconditionally clears every
        // tracked label (whether or not UnregisterCharacter fired on its own), fading them
        // out first so one still visible right as the scene loads eases away instead of popping.
        public void ResetAll()
        {
            if (_entries.Count == 0)
            {
                return;
            }
            StartCoroutine(FadeOutAndClearAll(new List<Character>(_entries.Keys)));
        }

        // beyond this range from a just-lit campfire, a dead player's body is unreachable
        private const float DeadLabelCampfireCullRadiusMeters = 100f;

        // called by DeadLabelCullPatches when a campfire's lighting advances the run - hides
        // (doesn't destroy) dead players' labels too far away to reach; UpdateImpl un-hides on revive
        public void CullDistantDeadLabels(Vector3 campfireWorldPosition)
        {
            foreach (var pair in _entries)
            {
                Character character = pair.Key;
                if (character == null || !character.data.dead)
                {
                    continue;
                }

                float distanceMeters = Vector3.Distance(
                    CharacterPositions.EffectivePosition(character), campfireWorldPosition) * CharacterStats.unitsToMeters;
                if (distanceMeters > DeadLabelCampfireCullRadiusMeters)
                {
                    pair.Value.Culled = true;
                }
            }
        }

        private const float ResetFadeDurationSeconds = 0.25f;

        private System.Collections.IEnumerator FadeOutAndClearAll(List<Character> characters)
        {
            var labels = new List<PlayerLabel>();
            foreach (Character character in characters)
            {
                if (_entries.TryGetValue(character, out Entry entry))
                {
                    labels.Add(entry.Label);
                }
            }

            // unscaled: a scene load can happen while paused, and a scaled delta would be
            // zero there, turning this into an instant pop instead of a fade
            float elapsed = 0f;
            while (elapsed < ResetFadeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / ResetFadeDurationSeconds);
                foreach (PlayerLabel label in labels)
                {
                    // Min, not a direct set: a label already fading out on its own should
                    // keep heading to 0 rather than pop back up towards this curve
                    label.Alpha = Mathf.Min(label.Alpha, alpha);
                }
                yield return null;
            }

            foreach (Character character in characters)
            {
                if (_entries.TryGetValue(character, out Entry entry))
                {
                    IndicatorManager.Instance.UnregisterAnchor(entry.Label.Anchor);
                    entry.Label.Destroy();
                    _entries.Remove(character);
                }
            }

            _skeletonEsp?.Clear();
        }

        private static readonly Common.Safe.Context _ctxUpdateImpl =
            new Common.Safe.Context("PlayerLabelController.Update", failureLimit: 300);

        private void Update()
        {
            if (_ctxUpdateImpl.Disabled) return;
            try { UpdateImpl(); _ctxUpdateImpl.Succeeded(); }
            catch (System.Exception e) { _ctxUpdateImpl.Failed(e); }
        }

        private void UpdateImpl()
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

                if (entry.Culled)
                {
                    if (data.dead)
                    {
                        continue;
                    }
                    entry.Culled = false;
                }

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
                    cfg.ShowPlayerLabelDistance.Value, cfg.ShowStatusBadges.Value, cfg.PlayerLabelBadgeSizePixels.Value);
            }
        }

        // draws the through-walls skeletons, gated on the same _labelsVisible state and
        // max-distance cap as the labels so one key press flashes both
        private static readonly Common.Safe.Context _ctxLateUpdateImpl =
            new Common.Safe.Context("PlayerLabelController.LateUpdate (skeleton ESP)", failureLimit: 300);

        private void LateUpdate()
        {
            if (_ctxLateUpdateImpl.Disabled) return;
            try { LateUpdateImpl(); _ctxLateUpdateImpl.Succeeded(); }
            catch (System.Exception e) { _ctxLateUpdateImpl.Failed(e); }
        }

        private void LateUpdateImpl()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.ShowPlayerSkeleton.Value || !_labelsVisible || Character.localCharacter == null)
            {
                _skeletonEsp?.Clear();
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                _skeletonEsp?.Clear();
                return;
            }

            if (_skeletonEsp == null)
            {
                _skeletonEsp = new PlayerSkeletonEsp(IndicatorManager.Instance.CanvasTransform);
            }

            Vector2 canvasSize = IndicatorManager.Instance.CanvasTransform.rect.size;
            float thickness = cfg.PlayerSkeletonLineThickness.Value;
            bool showJoints = cfg.ShowPlayerSkeletonJoints.Value;

            _skeletonEsp.BeginFrame();

            foreach (var pair in _entries)
            {
                Character character = pair.Key;
                if (character == null || !character.gameObject.activeInHierarchy)
                {
                    continue;
                }
                // same reason RegisterCharacter freezes at LastLivingPosition - a dead
                // character's bodyparts aren't trustworthy, and there's no "last known
                // pose" fallback for a whole rig, so dead players just don't get a skeleton
                if (character.data.dead)
                {
                    continue;
                }

                float distanceMeters = Vector3.Distance(
                    CharacterPositions.LocalViewpoint(),
                    CharacterPositions.EffectivePosition(character)) * CharacterStats.unitsToMeters;
                if (distanceMeters > cfg.PlayerLabelMaxDistanceMeters.Value)
                {
                    continue;
                }

                Color color = cfg.PlayerSkeletonUseCharacterColor.Value
                    ? character.refs.customization.PlayerColor
                    : NativeAssets.DefaultTextColor;

                _skeletonEsp.Draw(character, camera, canvasSize, color, thickness, showJoints);
            }

            _skeletonEsp.EndFrame();
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
            // a dead player has no native label at all - always show ours
            if (isDead)
            {
                return true;
            }
            return !IsNativeLabelVisible(lookedAt, cfg);
        }

        // reimplements IsLookedAt.Update's own visibility formula (RESEARCH.md Q1) so our
        // label fades in exactly where vanilla's fades out. Reads thresholds off the
        // character's live IsLookedAt instance rather than a hardcoded copy, since the
        // decompiled field defaults aren't guaranteed to match the live prefab.
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

            // PrepareEndCutscene only disables each Character's animator child, not the
            // Character itself, so activeInHierarchy stays true through the whole win/
            // helicopter cutscene - without this, labels kept tracking players frozen
            // wherever they were when it started. isPlayingCinematic is the same flag
            // vanilla itself gates cinematic-only behavior on (e.g. CharacterVoiceHandler).
            if (Singleton<PeakHandler>.Instance != null && Singleton<PeakHandler>.Instance.isPlayingCinematic)
            {
                return false;
            }

            switch (cfg.PlayerLabelDisplayMode.Value)
            {
                case LabelDisplayMode.AlwaysOn:
                    return true;

                case LabelDisplayMode.Toggle:
                {
                    // not Input.GetKeyDown: Unity's legacy Input Manager can silently miss
                    // a key-down edge when another key (e.g. WASD) is already held that
                    // frame, so we do the edge detection ourselves off plain GetKey
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
                        // set on every held frame, so a quick tap is already covered
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
