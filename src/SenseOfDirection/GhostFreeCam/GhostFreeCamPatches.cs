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
    /// flight controller: WASD + Space/Ctrl + Shift-to-sprint movement
    /// driven directly in real-world meters/second, and mouse look reusing
    /// PEAK's own already-sampled <c>CharacterInput.lookInput</c> (kept
    /// live even while dead - <c>Character.CanDoInput()</c> only checks for
    /// a blocking GUI/wheel, not alive/dead state) scaled by the player's
    /// actual in-game Mouse/Controller Sensitivity and Invert X/Y settings
    /// (<see cref="ApplyLook"/>), mirroring vanilla's own
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
            try
            {
                var lateUpdate = AccessTools.Method(typeof(MainCameraMovement), "LateUpdate");
                harmony.Patch(lateUpdate, postfix: new HarmonyMethod(typeof(GhostFreeCamPatches), nameof(LateUpdatePostfix)));

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
            GhostFreeCamConfigSync.Tick();

            Character local = Character.localCharacter;
            if (local == null || !local.data.fullyPassedOut)
            {
                Disengage();
                return;
            }

            PluginConfig cfg = Plugin.Instance.Cfg;
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
            ApplyMovement(camTransform, cfg);

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

        private static void ApplyMovement(Transform camTransform, PluginConfig cfg)
        {
            Vector3 moveInput = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) moveInput += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) moveInput += Vector3.back;
            if (Input.GetKey(KeyCode.A)) moveInput += Vector3.left;
            if (Input.GetKey(KeyCode.D)) moveInput += Vector3.right;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) moveInput += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) moveInput += Vector3.down;

            if (moveInput.sqrMagnitude <= 0f)
            {
                return;
            }

            float sprintMultiplier = Input.GetKey(KeyCode.LeftShift) ? cfg.GhostFreeCamSprintMultiplier.Value : 1f;
            float speedUnitsPerSecond = cfg.GhostFreeCamMoveSpeedMetersPerSecond.Value / CharacterStats.unitsToMeters * sprintMultiplier;
            camTransform.position += camTransform.TransformDirection(moveInput.normalized) * speedUnitsPerSecond * Time.unscaledDeltaTime;
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
            GhostFreeCamKeyHint.Hide();
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
