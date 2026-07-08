using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using pworld.Scripts.Extensions;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Mechanic 2 (better pings): a single Harmony patch set on
    /// <c>PointPinger</c>/<c>PointPing</c>, per RESEARCH.md Q6-Q9's finding
    /// that scaling, the ripple, the visibility-cutoff fix, anti-spam, and
    /// ghost-pinging all revolve around the same
    /// <c>ReceivePoint_Rpc</c>/<c>canPing</c>/<c>Go</c> surface rather than
    /// being three separate systems.
    ///
    /// <c>ReceivePoint_Rpc</c> is fully replaced (prefix returns false) rather
    /// than run alongside vanilla's own body, since vanilla's harsh
    /// visibility-based early-exit lives inside that one method with no
    /// prefix-only way to skip past it - our prefix reimplements the same
    /// spawn logic vanilla uses (RESEARCH.md Q6), just with our own
    /// visibility/anti-spam gates and hooks for the ripple/indicator/scale
    /// add-ons.
    /// </summary>
    public static class PointPingerPatches
    {
        /// <summary>Populated by <see cref="PingAwakePostfix"/>; read by <see cref="PingAudioTuner"/> to identify ping sounds among all currently-playing SFX.</summary>
        internal static readonly HashSet<AudioClip> PingClips = new HashSet<AudioClip>();

        /// <summary>
        /// `pingSound.settings.volume` is a shared SFX_Instance asset value,
        /// same as `.range` - cached here the first time it's seen so the
        /// volume multiplier is always relative to vanilla's real original
        /// value (never known ahead of time - RESEARCH.md Q6) rather than a
        /// guessed constant, and so toggling audio boost off mid-session
        /// correctly restores it.
        /// </summary>
        private static readonly Dictionary<SFX_Instance, float> OriginalPingVolume = new Dictionary<SFX_Instance, float>();

        private class SpamState
        {
            public float LastPingTime = float.NegativeInfinity;
            public int RapidCount;
        }

        private static readonly Dictionary<Character, SpamState> SpamStates = new Dictionary<Character, SpamState>();

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var receivePointRpc = AccessTools.Method(typeof(PointPinger), "ReceivePoint_Rpc");
                harmony.Patch(receivePointRpc, prefix: new HarmonyMethod(typeof(PointPingerPatches), nameof(ReceivePointRpcPrefix)));

                var canPingGetter = AccessTools.PropertyGetter(typeof(PointPinger), "canPing");
                harmony.Patch(canPingGetter, prefix: new HarmonyMethod(typeof(PointPingerPatches), nameof(CanPingGetterPrefix)));

                var go = AccessTools.Method(typeof(PointPing), "Go");
                harmony.Patch(go, postfix: new HarmonyMethod(typeof(PointPingerPatches), nameof(GoPostfix)));

                var pingAwake = AccessTools.Method(typeof(PointPing), "Awake");
                harmony.Patch(pingAwake, postfix: new HarmonyMethod(typeof(PointPingerPatches), nameof(PingAwakePostfix)));

                log.LogInfo("PointPingerPatches: patched PointPinger.ReceivePoint_Rpc/canPing, PointPing.Go/Awake.");
            }
            catch (Exception e)
            {
                log.LogError($"PointPingerPatches.Apply failed (non-fatal, ping enhancements won't work): {e}");
            }
        }

        /// <summary>
        /// Lets dead players keep pinging as ghosts when enable-ghost-ping is
        /// on (RESEARCH.md Q9) - a prefix replacing the conditional rather
        /// than an index-based IL transpiler like the GhostPing reference mod
        /// used, since a transpiler is fragile against any PEAK update that
        /// reorders this property's IL.
        /// </summary>
        private static bool CanPingGetterPrefix(PointPinger __instance, ref bool __result)
        {
            if (!Plugin.Instance.Cfg.EnableGhostPing.Value)
            {
                return true;
            }
            if (__instance.character == null || !__instance.character.data.dead)
            {
                return true;
            }
            bool inCooldown = Traverse.Create(__instance).Property("inCooldown").GetValue<bool>();
            __result = !inCooldown;
            return false;
        }

        /// <summary>
        /// Overwrites vanilla's own just-computed (and hard-capped,
        /// `minMaxScale` clamped to 0.2-3.0) <c>transform.localScale</c> with
        /// an uncapped recompute of the same frustum-relative formula, times
        /// our own multiplier. The vanilla clamp is exactly why pings used to
        /// visibly shrink (in apparent screen size) past a certain distance -
        /// removing it, rather than just multiplying the already-clamped
        /// value like an earlier version of this patch did, is what actually
        /// keeps a ping's apparent on-screen size constant no matter how far
        /// away it is. Recomputed fresh every frame since vanilla's own
        /// `Go()` does the same (called every `PointPing.Update()`).
        /// </summary>
        private static void GoPostfix(PointPing __instance)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            if (!cfg.EnablePingScaling.Value)
            {
                return;
            }
            Camera camera = Camera.main;
            if (camera == null || Character.localCharacter == null)
            {
                return;
            }
            float distance = Vector3.Distance(Character.localCharacter.Center, __instance.transform.position);
            float frustumValue = camera.SizeOfFrustumAtDistance(distance);
            float scale = frustumValue * __instance.sizeOfFrustum * cfg.PingScaleMultiplier.Value;
            __instance.transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Ping's `pingSound` is a shared SFX_Instance ScriptableObject asset
        /// (same instance across every spawned ping), so this only actually
        /// needs to run once to take effect mod-wide - cheap enough to just
        /// re-set on every ping spawn instead of tracking whether it already
        /// ran. Always computed from the known vanilla default (150,
        /// RESEARCH.md Q6) rather than an incremental Max(), so toggling the
        /// setting off mid-session correctly reverts it. Also feeds
        /// <see cref="PingClips"/> so <see cref="PingAudioTuner"/> can find
        /// the actual playing AudioSource - `settings.range` alone only
        /// controls whether `SFX_Player.PlaySFX` starts playing at all, not
        /// how quickly the sound actually falls off once playing.
        /// </summary>
        private static void PingAwakePostfix(PointPing __instance)
        {
            if (__instance.pingSound == null || __instance.pingSound.settings == null)
            {
                return;
            }
            PluginConfig cfg = Plugin.Instance.Cfg;

            const float vanillaDefaultRange = 150f;
            __instance.pingSound.settings.range = cfg.EnablePingAudioBoost.Value
                ? cfg.PingAudioRangeMeters.Value
                : vanillaDefaultRange;

            if (!OriginalPingVolume.TryGetValue(__instance.pingSound, out float originalVolume))
            {
                originalVolume = __instance.pingSound.settings.volume;
                OriginalPingVolume[__instance.pingSound] = originalVolume;
            }
            __instance.pingSound.settings.volume = cfg.EnablePingAudioBoost.Value
                ? originalVolume * cfg.PingAudioVolumeMultiplier.Value
                : originalVolume;

            if (__instance.pingSound.clips != null)
            {
                foreach (AudioClip clip in __instance.pingSound.clips)
                {
                    if (clip != null)
                    {
                        PingClips.Add(clip);
                    }
                }
            }
        }

        private static bool ReceivePointRpcPrefix(PointPinger __instance, Vector3 point, Vector3 hitNormal)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            Character character = __instance.character;
            if (character == null || Character.localCharacter == null || __instance.pointPrefab == null)
            {
                return true;
            }

            if (cfg.EnablePingAntiSpam.Value && !ShouldAcceptPing(character, cfg))
            {
                return false;
            }

            PointPing prefabPing = __instance.pointPrefab.GetComponent<PointPing>();

            float visibility = 1f;
            if (!cfg.RemoveVisibilityCutoff.Value)
            {
                bool obstructed = Physics.Linecast(
                    character.Head, Character.localCharacter.Head, out _,
                    HelperFunctions.LayerType.TerrainMap.ToLayerMask());
                float distanceToLocal = Vector3.Distance(character.Head, Character.localCharacter.Head);
                Vector2 v = prefabPing.visibilityFullNoneNoLos;
                visibility = 1f - Mathf.InverseLerp(
                    v.x, v.x + (v.y - v.x) * (obstructed ? prefabPing.NoLosVisibilityMul : 1f),
                    distanceToLocal);
                if (visibility <= 0f)
                {
                    return false;
                }
            }

            var pingInstanceField = AccessTools.Field(typeof(PointPinger), "pingInstance");
            if (pingInstanceField.GetValue(__instance) is GameObject existing && existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject spawned = UnityEngine.Object.Instantiate(
                __instance.pointPrefab, point,
                Quaternion.LookRotation((point - character.Head).normalized, Vector3.up));
            pingInstanceField.SetValue(__instance, spawned);

            PointPing spawnedPing = spawned.GetComponent<PointPing>();
            spawnedPing.hitNormal = hitNormal;
            spawnedPing.Init(character);
            spawnedPing.pointPinger = __instance;
            spawnedPing.renderer.material = UnityEngine.Object.Instantiate(character.refs.mainRenderer.sharedMaterial);
            spawnedPing.material.SetFloat("_Opacity", visibility);

            Color pingColor = character.refs.customization.PlayerColor;

            if (cfg.EnablePingRipple.Value)
            {
                PingRipple.Spawn(point, pingColor, spawnedPing.transform);
            }

            if (cfg.EnablePingOffScreenIndicator.Value || cfg.ShowPingDistanceLabel.Value)
            {
                PingWidgetLink.Attach(spawned, pingColor, cfg.EnablePingOffScreenIndicator.Value, cfg.ShowPingDistanceLabel.Value);
            }

            UnityEngine.Object.Destroy(spawned, 2f);
            return false;
        }

        /// <summary>
        /// Gradual, self-decaying rate limit - never applied to the local
        /// player's own pings (maintainer feedback: this is only meant to
        /// throttle other players spamming, not the person running the mod).
        /// An occasional ping from someone else is never delayed at all
        /// (`RapidCount` starts at, and quickly decays back to, 0); only
        /// pinging faster than `PingAntiSpamRapidIntervalSeconds` repeatedly
        /// ramps the required gap up (by `PingAntiSpamCooldownStepSeconds`
        /// per rapid ping, capped at `PingAntiSpamMaxCooldownSeconds`), and a
        /// quiet period of `PingAntiSpamResetSeconds` fully clears it again.
        /// </summary>
        private static bool ShouldAcceptPing(Character character, PluginConfig cfg)
        {
            if (character == Character.localCharacter)
            {
                return true;
            }

            if (!SpamStates.TryGetValue(character, out SpamState state))
            {
                state = new SpamState();
                SpamStates[character] = state;
            }

            float now = Time.time;
            float gap = now - state.LastPingTime;

            float requiredGap = Mathf.Min(state.RapidCount * cfg.PingAntiSpamCooldownStepSeconds.Value, cfg.PingAntiSpamMaxCooldownSeconds.Value);
            bool accepted = gap >= requiredGap;

            if (gap >= cfg.PingAntiSpamResetSeconds.Value)
            {
                state.RapidCount = 0;
            }
            else if (gap < cfg.PingAntiSpamRapidIntervalSeconds.Value)
            {
                state.RapidCount++;
            }
            else
            {
                state.RapidCount = Mathf.Max(0, state.RapidCount - 1);
            }

            state.LastPingTime = now;
            return accepted;
        }
    }
}
