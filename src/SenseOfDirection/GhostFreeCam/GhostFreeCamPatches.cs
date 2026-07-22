using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Zorro.ControllerSupport;
using Zorro.Settings;

namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// Phase 6 (Mechanic 3): lets a dead, spectating player fly a free
    /// camera instead of being stuck in vanilla's third-person spectate
    /// view, leashed to whichever living player they're currently
    /// spectating.
    ///
    /// Per RESEARCH.md Q10, the original plan was to reuse PEAK's own
    /// dormant free-fly camera controller (<c>GodCam</c> - fully built,
    /// nothing in the shipped game ever instantiates it) wholesale rather
    /// than write a bespoke one. In-game testing surfaced exactly the
    /// "hidden gotcha" RESEARCH.md flagged as the fallback trigger: reused
    /// as-is, it was unusably slow to fly or turn with - most likely
    /// because it reads mouse look via the legacy <c>Input.GetAxis("Mouse
    /// X"/"Mouse Y")</c> virtual axes, which nothing else in the shipped
    /// game touches at all (PEAK's real character camera drives look
    /// entirely off <c>CharacterData.lookDirection</c>, itself written
    /// elsewhere via the new Input System) - so whatever this project's
    /// legacy axis sensitivity is actually configured to, it was never
    /// tuned against real gameplay feel. Movement was also far slower than
    /// its `force`/`drag` fields suggest it should be. Rather than fight
    /// unowned vanilla tuning/input plumbing, this is now a small bespoke
    /// flight controller: move/jump/crouch/sprint driven directly in real-
    /// world meters/second off PEAK's own already-sampled
    /// <c>CharacterInput.movementInput</c>/<c>jumpIsPressed</c>/
    /// <c>crouchIsPressed</c>/<c>sprintIsPressed</c> (kept live even while
    /// dead - <c>Character.CanDoInput()</c> only checks for a blocking GUI/
    /// wheel, not alive/dead state), so it automatically follows whatever
    /// the player has bound those to in PEAK's own Controls settings rather
    /// than assuming WASD/Space/Ctrl/Shift (<see cref="ApplyMovement"/>) -
    /// plus two extra mod-only up/down keys with no vanilla action to read
    /// (E/Q by default, <c>ascend-key</c>/<c>descend-key</c>). Mouse look
    /// reuses <c>CharacterInput.lookInput</c> the same way, scaled by the
    /// player's actual in-game Mouse/Controller Sensitivity and Invert X/Y
    /// settings (<see cref="ApplyLook"/>), mirroring vanilla's own
    /// <c>CharacterMovement.CameraLook()</c> formula exactly - deliberately
    /// *not* a separate free-cam-only sensitivity setting, since dialing in
    /// one more sensitivity value on top of everyone's already-different
    /// mouse/DPI/personal preference is exactly the kind of setting nobody
    /// wants to have to tune twice.
    ///
    /// Implemented as a single postfix on the private
    /// <c>MainCameraMovement.LateUpdate</c>, deliberately *not* touching
    /// <c>isGodCam</c> at all: that field's early-return in vanilla's own
    /// method would skip <c>Spectate()</c> entirely, which is also where
    /// vanilla's own target-switching input handling and the networked
    /// <c>PlayerGhost</c> creation/target-follow RPCs live - reusing
    /// <c>isGodCam</c> would mean reimplementing all of that ourselves. Far
    /// simpler to let vanilla's own <c>Spectate()</c> keep running every
    /// frame exactly as normal (so switching who you're spectating, and the
    /// ghost body other clients see, both keep working unmodified), then in
    /// this postfix - which runs after it - overwrite the camera transform
    /// with our own free-fly movement and leash-clamp it to the currently
    /// spectated player.
    ///
    /// Other clients seeing this player's ghost body follow the free camera
    /// (rather than vanilla's tightly-anchored pan/zoom) is handled
    /// separately by <see cref="GhostFreeCamPoseSync"/> plus this file's own
    /// <c>PlayerGhost.Update</c> postfix - see that class's doc comment for
    /// why that needed real (if lightweight) networking, unlike everything
    /// else in this mod.
    /// </summary>
    public static class GhostFreeCamPatches
    {
        private static ManualLogSource _log;

        /// <summary>Player's toggle intent - reset to false whenever they stop being eligible (revived, no valid spectate target, or the host has ghost free-cam off).</summary>
        private static bool _active;

        /// <summary>
        /// Consecutive frames <c>local.data.fullyPassedOut</c> has read false
        /// since it last read true. Some third-party mods (e.g. PEAKSleepTalk's
        /// <c>CharacterVoiceHandler.Update</c>/<c>AnimatedMouth.ProcessMicData</c>
        /// patches, which let passed-out players keep talking) transiently
        /// flip <c>CharacterData.passedOut</c>/<c>.fullyPassedOut</c> to
        /// <see langword="false"/> and back to <see langword="true"/> within a
        /// single method call every frame - normally fully contained within
        /// Unity's Update phase, well before this postfix (which runs in
        /// LateUpdate) ever observes it. But if such a mod throws between its
        /// own prefix and postfix (a bad interaction with a *third* mod, a
        /// game-version mismatch in what it patches, etc.) the restore never
        /// happens and <c>fullyPassedOut</c> can read false for one or more
        /// stray frames despite the player still actually being passed
        /// out/dead. Reacting to that immediately would incorrectly
        /// <see cref="Disengage"/> (dropping <see cref="_active"/> and hiding
        /// the key hint) for however long the flicker lasts. Requiring a
        /// short run of consecutive false reads before treating the player as
        /// no-longer-eligible absorbs single-frame flicker like this without
        /// meaningfully delaying real re-conscious/revive transitions (which
        /// hold false indefinitely, not for one frame).
        /// </summary>
        private static int _notFullyPassedOutStreak;

        private const int NotFullyPassedOutStreakToDisengage = 3;

        private static float _lastDiagLogTime;

        /// <summary>Set once <see cref="Compatibility.SleepTalkCompat"/> has run - deferred to the first LateUpdate postfix call (rather than <see cref="Apply"/> itself) so every other mod's own Awake, including PEAKSleepTalk's <c>harmony.PatchAll()</c>, has already had a chance to run first; BepInEx finishes all plugins' Awake before any MonoBehaviour Update/LateUpdate ever fires.</summary>
        private static bool _compatChecked;

        private static Harmony _harmony;

        /// <summary>Toggle state for <see cref="PluginConfig.EnableGhostFreeCamKeyHintPreview"/>'s dev/QA hint preview - kept separate from <see cref="_active"/> so the preview can't affect real free-cam engage/disengage state.</summary>
        private static bool _debugPreviewActive;

        /// <summary>True once <see cref="_lastPosition"/>/<see cref="_lastRotation"/> hold a live free-cam pose from last frame, rather than a stale/unseeded one.</summary>
        private static bool _engagedLastFrame;

        private static Vector3 _lastPosition;
        private static Quaternion _lastRotation;
        private static float _yaw;
        private static float _pitch;

        // Resolved lazily (GameHandler.Instance.SettingsHandler isn't
        // guaranteed ready at plugin Awake time) and cached, same pattern
        // MainCameraMovement.Start itself uses for FovSetting/ExtraFovSetting.
        private static MouseSensitivitySetting _mouseSensSetting;
        private static ControllerSensitivitySetting _controllerSensSetting;
        private static InvertXSetting _invertXSetting;
        private static InvertYSetting _invertYSetting;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            _log = log;
            _harmony = harmony;
            try
            {
                var lateUpdate = AccessTools.Method(typeof(MainCameraMovement), "LateUpdate");
                harmony.Patch(
                    lateUpdate,
                    postfix: new HarmonyMethod(typeof(GhostFreeCamPatches), nameof(LateUpdatePostfix)),
                    finalizer: new HarmonyMethod(typeof(GhostFreeCamPatches), nameof(LateUpdateFinalizer)));

                var ghostUpdate = AccessTools.Method(typeof(PlayerGhost), "Update");
                harmony.Patch(ghostUpdate, postfix: new HarmonyMethod(typeof(GhostFreeCamPatches), nameof(GhostUpdatePostfix)));

                GhostFreeCamPoseSync.EnsureRegistered();

                log.LogInfo("GhostFreeCamPatches: patched MainCameraMovement.LateUpdate, PlayerGhost.Update.");
            }
            catch (Exception e)
            {
                log.LogError($"GhostFreeCamPatches.Apply failed (non-fatal, ghost free-cam won't work): {e}");
            }
        }

        private static float _lastThirdPartyExceptionLogTime;

        /// <summary>
        /// Harmony finalizers, unlike prefixes/postfixes, always run even
        /// when the patched method (or another mod's own prefix/patch
        /// somewhere in its call chain) throws - and clearing
        /// <c>__exception</c> here tells Harmony to treat the call as having
        /// succeeded, so <see cref="LateUpdatePostfix"/> still gets to run
        /// afterward. Confirmed necessary via a real bug report: PEAKSleepTalk
        /// (see <see cref="Compatibility.SleepTalkCompat"/>) had a patch on
        /// <c>MainCameraMovement.HandleSpecSelection</c> - called from
        /// <c>Spectate()</c>, itself only ever called once
        /// <c>fullyPassedOut</c> is true - that caused vanilla's own
        /// <c>LateUpdate</c> to throw every frame once a player was fully
        /// passed out/dead, silently starving our own postfix (plain
        /// postfixes never run when the method they're attached to threw)
        /// and leaving the spectate camera frozen with no way to ever enter
        /// ghost free-cam. <see cref="Compatibility.SleepTalkCompat"/> removes
        /// that specific known-bad patch outright, but this finalizer is the
        /// general safety net: it means *any* other mod that ever breaks
        /// vanilla's spectate flow the same way (now or in the future) can't
        /// take our own ghost free-cam down with it - we just lose that one
        /// frame's vanilla spectate positioning instead, which self-corrects
        /// next frame same as the null-spec-target case below already
        /// handles.
        /// </summary>
        private static Exception LateUpdateFinalizer(Exception __exception)
        {
            if (__exception != null)
            {
                float now = Time.unscaledTime;
                if (now - _lastThirdPartyExceptionLogTime >= 5f)
                {
                    _lastThirdPartyExceptionLogTime = now;
                    _log?.LogWarning(
                        "GhostFreeCamPatches: vanilla MainCameraMovement.LateUpdate (or another mod's patch somewhere in its call chain, e.g. HandleSpecSelection/Spectate) threw - "
                        + $"suppressing so ghost free-cam keeps working: {__exception}");
                }
            }
            return null;
        }

        private static void LateUpdatePostfix(MainCameraMovement __instance)
        {
            try
            {
                LateUpdatePostfixImpl(__instance);
            }
            catch (Exception e)
            {
                _log?.LogError($"GhostFreeCamPatches.LateUpdatePostfix failed, disengaging free-cam for this frame: {e}");
                Disengage();
            }
        }

        private static void LateUpdatePostfixImpl(MainCameraMovement __instance)
        {
            if (!_compatChecked)
            {
                _compatChecked = true;
                Compatibility.SleepTalkCompat.Apply(_harmony, _log);
            }

            GhostFreeCamConfigSync.Tick();

            PluginConfig cfg = Plugin.Instance.Cfg;

            // Dev/QA-only preview: lets the hint's exact look be checked
            // without dying first. Runs ahead of (and independent of) the
            // real passed-out gate below, using its own toggle state rather
            // than `_active` so it can't interfere with the real free-cam
            // engage/disengage logic once actually a ghost.
            if (cfg.EnableGhostFreeCamKeyHintPreview.Value)
            {
                if (Input.GetKeyDown(cfg.GhostFreeCamToggleKey.Value))
                {
                    _debugPreviewActive = !_debugPreviewActive;
                }
                GhostFreeCamKeyHint.SetState(_debugPreviewActive, cfg.GhostFreeCamToggleKey.Value);
            }

            Character local = Character.localCharacter;
            if (local == null)
            {
                _notFullyPassedOutStreak = NotFullyPassedOutStreakToDisengage;
                Disengage();
                return;
            }

            if (!local.data.fullyPassedOut)
            {
                _notFullyPassedOutStreak++;
                if (_notFullyPassedOutStreak >= NotFullyPassedOutStreakToDisengage)
                {
                    Disengage();
                    return;
                }
            }
            else
            {
                _notFullyPassedOutStreak = 0;
            }

            if (cfg.EnableDebugLogging.Value)
            {
                LogDiagnostics(local);
            }

            if (Input.GetKeyDown(cfg.GhostFreeCamToggleKey.Value))
            {
                _active = !_active;
            }

            if (!GhostFreeCamConfigSync.TryGetEffective(out float maxDistanceMeters, out bool unlimited))
            {
                Disengage();
                return;
            }

            if (cfg.GhostFreeCamShowKeyHint.Value)
            {
                GhostFreeCamKeyHint.SetState(_active, cfg.GhostFreeCamToggleKey.Value);
            }
            else
            {
                GhostFreeCamKeyHint.Hide();
            }

            if (!_active)
            {
                _engagedLastFrame = false;
                GhostFreeCamCrosshair.SetVisible(false);
                return;
            }

            Character spec = MainCameraMovement.specCharacter;
            MainCamera cam = MainCamera.instance;
            if (spec == null || cam == null)
            {
                // Nothing valid to anchor/render to this frame - leave
                // vanilla's own Spectate() output on screen rather than
                // freezing on a stale free-cam pose.
                _engagedLastFrame = false;
                GhostFreeCamCrosshair.SetVisible(false);
                return;
            }

            GhostFreeCamCrosshair.SetVisible(Plugin.Instance.Cfg.GhostFreeCamShowCrosshair.Value);

            Transform camTransform = __instance.transform;

            if (!_engagedLastFrame)
            {
                // Fresh engage: start flying from wherever vanilla's own
                // spectate camera currently is, and derive our own
                // yaw/pitch from that same orientation so the very first
                // frame of mouse look continues smoothly instead of
                // snapping to some other stale angle.
                _lastPosition = camTransform.position;
                _lastRotation = camTransform.rotation;
                Vector3 euler = _lastRotation.eulerAngles;
                _yaw = euler.y;
                _pitch = NormalizePitch(euler.x);
            }

            camTransform.position = _lastPosition;
            camTransform.rotation = _lastRotation;

            ApplyLook(camTransform);
            ApplyMovement(camTransform, cfg, local);

            Vector3 anchor = spec.GetSpectatePosition();
            if (!unlimited)
            {
                float maxDistanceUnits = maxDistanceMeters / CharacterStats.unitsToMeters;
                Vector3 offset = camTransform.position - anchor;
                if (offset.magnitude > maxDistanceUnits)
                {
                    camTransform.position = anchor + offset.normalized * maxDistanceUnits;
                }
            }

            _lastPosition = camTransform.position;
            _lastRotation = camTransform.rotation;
            _engagedLastFrame = true;

            GhostFreeCamPoseSync.SendPose(camTransform.position, camTransform.rotation);
        }

        /// <summary>
        /// Mirrors <c>CharacterMovement.CameraLook()</c>'s own formula
        /// exactly (same input field, same settings, same invert handling)
        /// so free-cam turns at whatever rate the player already tuned for
        /// normal play, rather than asking them to separately dial in a
        /// free-cam-only sensitivity. <c>Character.localCharacter.input</c>
        /// is the same <c>CharacterInput</c> instance vanilla's own look
        /// code reads from - its <c>lookInput</c> keeps sampling live even
        /// while dead (see this file's own doc comment).
        /// </summary>
        private static void ApplyLook(Transform camTransform)
        {
            Character local = Character.localCharacter;
            if (local == null || local.input == null)
            {
                return;
            }

            if (_mouseSensSetting == null)
            {
                _mouseSensSetting = GameHandler.Instance.SettingsHandler.GetSetting<MouseSensitivitySetting>();
                _controllerSensSetting = GameHandler.Instance.SettingsHandler.GetSetting<ControllerSensitivitySetting>();
                _invertXSetting = GameHandler.Instance.SettingsHandler.GetSetting<InvertXSetting>();
                _invertYSetting = GameHandler.Instance.SettingsHandler.GetSetting<InvertYSetting>();
            }

            Vector2 lookInput = local.input.lookInput;
            float sensitivity = InputHandler.GetCurrentUsedInputScheme() == InputScheme.KeyboardMouse
                ? _mouseSensSetting.Value
                : _controllerSensSetting.Value;
            float invertX = _invertXSetting.Value == OffOnMode.OFF ? 1f : -1f;
            float invertY = _invertYSetting.Value == OffOnMode.OFF ? 1f : -1f;

            _yaw += lookInput.x * sensitivity * invertX;
            _pitch -= lookInput.y * sensitivity * invertY;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            camTransform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>
        /// Forward/strafe, jump and sprint all read <c>local.input</c> - the
        /// same live-while-dead <c>CharacterInput</c> instance <see
        /// cref="ApplyLook"/> already reads <c>lookInput</c> from (see that
        /// method's own doc comment) - rather than hardcoded <c>KeyCode</c>s,
        /// so free-cam automatically follows whatever the player has bound
        /// Move Forward/Back/Left/Right, Jump, Crouch and Sprint to in
        /// PEAK's own Controls settings instead of assuming WASD/Space/Ctrl/
        /// Shift. <c>ascend-key</c>/<c>descend-key</c> are the one exception:
        /// they're this mod's own extra convenience keys (E/Q by default,
        /// stacking on top of jump/crouch), with no vanilla action of their
        /// own to read, so those two stay plain <c>Input.GetKey</c> reads of
        /// their own <c>ConfigEntry&lt;KeyCode&gt;</c>.
        ///
        /// While merely unconscious (<c>fullyPassedOut</c> but not yet
        /// <c>dead</c>), the ascend/descend keys are ignored entirely - E/Q
        /// default there are vanilla's own keys to speed up dying, and a
        /// player flying around during that window must not be able to
        /// accidentally hasten their own death just by using the free-cam's
        /// extra up/down keys. Jump/crouch remain available throughout,
        /// including once actually dead, so vertical movement still works
        /// for a full ghost.
        /// </summary>
        private static void ApplyMovement(Transform camTransform, PluginConfig cfg, Character local)
        {
            if (local.input == null)
            {
                return;
            }

            bool unconsciousNotDead = local.data.fullyPassedOut && !local.data.dead;

            Vector2 movement = local.input.movementInput;
            Vector3 moveInput = Vector3.forward * movement.y + Vector3.right * movement.x;

            bool ascendKeyHeld = !unconsciousNotDead && cfg.GhostFreeCamAscendKey.Value != KeyCode.None && Input.GetKey(cfg.GhostFreeCamAscendKey.Value);
            bool descendKeyHeld = !unconsciousNotDead && cfg.GhostFreeCamDescendKey.Value != KeyCode.None && Input.GetKey(cfg.GhostFreeCamDescendKey.Value);
            if (local.input.jumpIsPressed || ascendKeyHeld) moveInput += Vector3.up;
            if (local.input.crouchIsPressed || descendKeyHeld) moveInput += Vector3.down;

            if (moveInput.sqrMagnitude <= 0f)
            {
                return;
            }

            float sprintMultiplier = local.input.sprintIsPressed ? cfg.GhostFreeCamSprintMultiplier.Value : 1f;
            float speedUnitsPerSecond = cfg.GhostFreeCamMoveSpeedMetersPerSecond.Value / CharacterStats.unitsToMeters * sprintMultiplier;
            camTransform.position += camTransform.TransformDirection(moveInput.normalized) * speedUnitsPerSecond * Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Dev/QA-only, throttled to ~once/second: dumps the exact state this
        /// class's own gating depends on while the local player is passed
        /// out/dead, so a bug report that only says "ghost free-cam doesn't
        /// work with mod X" can be matched against what actually happened
        /// frame-to-frame (e.g. <c>specCharacter</c> going null and staying
        /// null, <c>fullyPassedOut</c> flicker being absorbed by the debounce
        /// above, or the camera pose genuinely not moving despite input).
        /// </summary>
        private static void LogDiagnostics(Character local)
        {
            float now = Time.unscaledTime;
            if (now - _lastDiagLogTime < 1f)
            {
                return;
            }
            _lastDiagLogTime = now;

            Character spec = MainCameraMovement.specCharacter;
            _log?.LogInfo(
                $"GhostFreeCam diag: dead={local.data.dead} fullyPassedOut={local.data.fullyPassedOut} "
                + $"notFullyPassedOutStreak={_notFullyPassedOutStreak} specCharacter={(spec == null ? "null" : (spec == local ? "self" : spec.characterName))} "
                + $"ghost={(local.Ghost == null ? "null" : "present")} active={_active} engagedLastFrame={_engagedLastFrame} "
                + $"lastPos={_lastPosition}");
        }

        private static float NormalizePitch(float eulerX)
        {
            return eulerX > 180f ? eulerX - 360f : eulerX;
        }

        private static void Disengage()
        {
            _active = false;
            _engagedLastFrame = false;
            GhostFreeCamCrosshair.SetVisible(false);

            // Leave the hint alone while the dev/QA preview is on - it's
            // driven independently above and would otherwise be hidden
            // again on this same frame every time this runs (e.g. every
            // frame the local player isn't actually a ghost).
            if (!Plugin.Instance.Cfg.EnableGhostFreeCamKeyHintPreview.Value)
            {
                GhostFreeCamKeyHint.Hide();
            }
        }

        /// <summary>
        /// Only ever meaningfully affects <em>other</em> clients' view of
        /// this ghost - <c>RPCA_InitGhost</c> already permanently disables
        /// all of a ghost's own renderers for its owner, so neither the
        /// pose override nor the hide-all-ghosts check below ever has
        /// anything visible to change on the owner's own client.
        /// </summary>
        private static void GhostUpdatePostfix(PlayerGhost __instance)
        {
            try
            {
                GhostUpdatePostfixImpl(__instance);
            }
            catch (Exception e)
            {
                _log?.LogError($"GhostFreeCamPatches.GhostUpdatePostfix failed: {e}");
            }
        }

        private static void GhostUpdatePostfixImpl(PlayerGhost ghost)
        {
            if (ghost.m_view == null || ghost.m_view.IsMine)
            {
                return;
            }

            if (GhostFreeCamPoseSync.TryGetPose(ghost.m_view.OwnerActorNr, out Vector3 position, out Quaternion rotation))
            {
                ghost.transform.position = position;
                ghost.transform.rotation = rotation;
            }

            bool hide = Plugin.Instance.Cfg.HideAllGhosts.Value;
            SetRenderersEnabled(ghost, !hide);
        }

        private static void SetRenderersEnabled(PlayerGhost ghost, bool visible)
        {
            if (ghost.PlayerRenderers != null)
            {
                foreach (Renderer r in ghost.PlayerRenderers)
                {
                    if (r != null) r.enabled = visible;
                }
            }
            if (ghost.EyeRenderers != null)
            {
                foreach (Renderer r in ghost.EyeRenderers)
                {
                    if (r != null) r.enabled = visible;
                }
            }
            if (ghost.mouthRenderer != null) ghost.mouthRenderer.enabled = visible;
            if (ghost.accessoryRenderer != null) ghost.accessoryRenderer.enabled = visible;
        }
    }
}
