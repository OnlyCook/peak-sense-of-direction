using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using pworld.Scripts.Extensions;
using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
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
            public int FreeCount;
            public bool SlowMode;
            public int QueueLength;
            public float NextSlotTime;
        }

        private static readonly Dictionary<Character, SpamState> SpamStates = new Dictionary<Character, SpamState>();

        private static ManualLogSource _log;

        /// <summary>
        /// Reflection resolved once at patch time, not per ping/frame.
        /// <c>AccessTools.Field</c> was being called on every spawned ping and
        /// <c>Traverse</c> (which is reflection plus its own caching layer, and
        /// still the slowest way to read a property) on every <c>canPing</c>
        /// read - and <c>canPing</c> is read every frame the ping key is held,
        /// not once per ping.
        /// </summary>
        private static FieldInfo _pingInstanceField;
        private static Func<PointPinger, bool> _inCooldownGetter;

        /// <summary>
        /// Reused across pings: <see cref="TryGetPingHitPrefix"/> runs a
        /// spherecast that can return many hits and then sorts them by
        /// distance. The NonAlloc physics calls fill this instead of allocating
        /// a fresh array per ping, and the comparison is a cached delegate
        /// rather than a lambda allocated at each sort.
        /// </summary>
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[256];
        private static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            _log = log;
            try
            {
                _pingInstanceField = AccessTools.Field(typeof(PointPinger), "pingInstance");
                _inCooldownGetter = AccessTools.MethodDelegate<Func<PointPinger, bool>>(
                    AccessTools.PropertyGetter(typeof(PointPinger), "inCooldown"));

                var receivePointRpc = AccessTools.Method(typeof(PointPinger), "ReceivePoint_Rpc");
                harmony.Patch(receivePointRpc, prefix: new HarmonyMethod(typeof(PointPingerPatches), nameof(ReceivePointRpcPrefix)));

                var canPingGetter = AccessTools.PropertyGetter(typeof(PointPinger), "canPing");
                harmony.Patch(canPingGetter, prefix: new HarmonyMethod(typeof(PointPingerPatches), nameof(CanPingGetterPrefix)));

                var go = AccessTools.Method(typeof(PointPing), "Go");
                harmony.Patch(go, postfix: new HarmonyMethod(typeof(PointPingerPatches), nameof(GoPostfix)));

                var pingAwake = AccessTools.Method(typeof(PointPing), "Awake");
                harmony.Patch(pingAwake, postfix: new HarmonyMethod(typeof(PointPingerPatches), nameof(PingAwakePostfix)));

                var tryGetPingHit = AccessTools.Method(typeof(PointPinger), "TryGetPingHit");
                harmony.Patch(tryGetPingHit, prefix: new HarmonyMethod(typeof(PointPingerPatches), nameof(TryGetPingHitPrefix)));

                log.LogInfo("PointPingerPatches: patched PointPinger.ReceivePoint_Rpc/canPing/TryGetPingHit, PointPing.Go/Awake.");
            }
            catch (Exception e)
            {
                log.LogError($"PointPingerPatches.Apply failed (non-fatal, ping enhancements won't work): {e}");
            }
        }

        /// <summary>
        /// Widens vanilla's own ping raycast (plain <c>Raycast</c> against
        /// <c>TerrainMap</c> only, RESEARCH.md Q6) to also hit the "Default"
        /// layer items/luggage sit on (<c>AllPhysicalExceptCharacter</c>),
        /// optionally as a <c>SphereCast</c> instead of a plain raycast for a
        /// forgiving "hitbox" so aiming near an item - not pixel-perfect on
        /// its exact collider - still pings it directly instead of the ping
        /// phasing through to whatever terrain/foliage is behind it (the
        /// maintainer's top complaint about the first pass of this feature:
        /// a coconut up a tree, or small items like an energy drink, were
        /// nearly impossible to actually land a ping point on). This is the
        /// same underlying technique <c>memiczny-PingItems</c> used (an IL
        /// transpiler swapping the same layer-mask constant, confirmed via
        /// its decompiled `PointPingerPatch.UpdateTranspiler`) before it
        /// broke for unrelated reasons - reimplemented here as a plain
        /// Harmony prefix instead of an index-based transpiler, consistent
        /// with this file's existing preference for prefixes (RESEARCH.md
        /// Q9). Falls back to vanilla's own raycast (<c>return true</c>) if
        /// disabled, if <c>Camera.main</c> isn't available yet, or on any
        /// exception - this fully replaces the original body when it does
        /// run, so (like <see cref="ReceivePointRpcPrefix"/>) any uncaught
        /// exception here would otherwise break pinging outright rather than
        /// just one visual enhancement.
        /// </summary>
        private static bool TryGetPingHitPrefix(out RaycastHit hit, ref bool __result)
        {
            try
            {
                PluginConfig cfg = Plugin.Instance.Cfg;
                if (cfg.EnableItemPingHitAssist.Value)
                {
                    Camera camera = Camera.main;
                    if (camera != null)
                    {
                        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                        LayerMask mask = HelperFunctions.AllPhysicalExceptCharacter;
                        float sphereRadiusUnits = cfg.ItemPingHitboxRadiusMeters.Value / CharacterStats.unitsToMeters;
                        // QueryTriggerInteraction.Collide, not the default
                        // (global-setting-dependent) behavior: several
                        // creature hitboxes (e.g. Spider's own catch volume)
                        // are trigger colliders, not solid ones, and Unity's
                        // physics queries ignore triggers by default - a
                        // trigger-only hitbox was otherwise invisible to this
                        // raycast/spherecast no matter how wide the layer
                        // mask or how forgiving the sphere radius.
                        //
                        // *All* hits (not just the nearest), since the
                        // nearest hit widening this raycast onto the "Default"
                        // layer finds is often the local player's own
                        // first-person held-item/hand view-model sitting
                        // right in front of the camera - vanilla's own
                        // TerrainMap-only raycast never had this problem
                        // since hand/held-item colliders aren't on that
                        // layer. Skip past those to the first hit that isn't
                        // part of the local hand/held item (ISSUES.md: "you'll
                        // ping the held item instead").
                        RaycastHit[] hits = HitBuffer;
                        int hitCount = sphereRadiusUnits > 0f
                            ? Physics.SphereCastNonAlloc(ray, sphereRadiusUnits, HitBuffer, 1000f, mask, QueryTriggerInteraction.Collide)
                            : Physics.RaycastNonAlloc(ray, HitBuffer, 1000f, mask, QueryTriggerInteraction.Collide);

                        // A filled buffer means the query had more hits than it
                        // could hand back, and the ones it dropped are an
                        // arbitrary subset - not the furthest ones. Along a
                        // 1000m cast through a dense level that could quietly
                        // throw away the very hit the ping should have landed
                        // on, so fall back to the allocating all-hits form for
                        // that (rare) case rather than mispositioning the ping
                        // to save an allocation.
                        if (hitCount == HitBuffer.Length)
                        {
                            hits = sphereRadiusUnits > 0f
                                ? Physics.SphereCastAll(ray, sphereRadiusUnits, 1000f, mask, QueryTriggerInteraction.Collide)
                                : Physics.RaycastAll(ray, 1000f, mask, QueryTriggerInteraction.Collide);
                            hitCount = hits.Length;
                        }

                        Array.Sort(hits, 0, hitCount, ByDistance);

                        for (int i = 0; i < hitCount; i++)
                        {
                            RaycastHit candidate = hits[i];
                            if (!IsLocalHandOrHeldItem(candidate.collider))
                            {
                                hit = candidate;
                                __result = true;
                                return false;
                            }
                        }

                        hit = default;
                        __result = false;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _log?.LogError($"TryGetPingHitPrefix failed, falling back to vanilla ping raycast: {e}");
            }

            hit = default;
            return true;
        }

        /// <summary>
        /// Local player's own first-person view-model root is literally named
        /// "Hand" in-game (confirmed via <c>ItemPingDetector.LogNearbyUnmatched</c>,
        /// which excludes it from its debug dump the same way), so this walks
        /// up the collider's transform hierarchy looking for that name -
        /// covers a collider sitting on a nested child of the hand rig, not
        /// just directly on it. Also excludes anything under the local
        /// character's own currently-held <c>Item</c>, in case its world
        /// pickup collider (as opposed to the view-model) is what's actually
        /// being hit at point-blank range.
        /// </summary>
        private static bool IsLocalHandOrHeldItem(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            for (Transform t = collider.transform; t != null; t = t.parent)
            {
                if (t.name == "Hand")
                {
                    return true;
                }
            }

            Item heldItem = Character.localCharacter?.data?.currentItem;
            if (heldItem != null && collider.GetComponentInParent<Item>() == heldItem)
            {
                return true;
            }

            return false;
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
            if (_inCooldownGetter == null)
            {
                return true;
            }
            __result = !_inCooldownGetter(__instance);
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
            float distance = Vector3.Distance(CharacterPositions.LocalViewpoint(), __instance.transform.position);
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

        /// <summary>
        /// Wrapped in try/catch, unlike the rest of this file's patches -
        /// this one fully replaces vanilla's method body (returns false), so
        /// unlike a postfix/non-replacing prefix, any uncaught exception here
        /// would propagate out of a live `[PunRPC]` callback instead of just
        /// failing one cosmetic add-on. An RPC method throwing partway
        /// through is exactly the kind of failure that can look like
        /// "pinging stopped working entirely" rather than "one visual
        /// enhancement is missing" - so on any exception, log it once and
        /// fall back to letting vanilla's own body run (`return true`)
        /// instead of leaving the ping silently dropped.
        /// </summary>
        private static bool ReceivePointRpcPrefix(PointPinger __instance, Vector3 point, Vector3 hitNormal)
        {
            try
            {
                return ReceivePointRpcPrefixImpl(__instance, point, hitNormal);
            }
            catch (Exception e)
            {
                _log?.LogError($"PointPingerPatches.ReceivePointRpcPrefix failed, falling back to vanilla for this ping: {e}");
                return true;
            }
        }

        private static bool ReceivePointRpcPrefixImpl(PointPinger __instance, Vector3 point, Vector3 hitNormal)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            Character character = __instance.character;
            if (character == null || Character.localCharacter == null || __instance.pointPrefab == null)
            {
                return true;
            }

            if (cfg.EnablePingAntiSpam.Value)
            {
                if (!TryAdmitPing(character, cfg, out float delaySeconds))
                {
                    return false;
                }
                if (delaySeconds > 0f)
                {
                    Plugin.Instance.StartCoroutine(DelayedPingCoroutine(character, __instance, point, hitNormal, cfg, delaySeconds));
                    return false;
                }
            }

            SpawnPingNow(__instance, point, hitNormal, character, cfg);
            return false;
        }

        /// <summary>
        /// No cancellation/replacement here (unlike an earlier version of
        /// this patch) - up to <c>PingAntiSpamMaxQueueLength</c> of a
        /// spamming player's pings are genuinely queued concurrently once in
        /// slow mode, each with its own independent coroutine, so a burst of
        /// 2 queued pings both eventually show (spaced by
        /// <c>PingAntiSpamSlowModeIntervalSeconds</c> per <see
        /// cref="TryAdmitPing"/>'s own scheduling) rather than the newest one
        /// stomping the previous one's slot.
        /// </summary>
        private static System.Collections.IEnumerator DelayedPingCoroutine(Character character, PointPinger instance, Vector3 point, Vector3 hitNormal, PluginConfig cfg, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (SpamStates.TryGetValue(character, out SpamState state))
            {
                state.QueueLength = Mathf.Max(0, state.QueueLength - 1);
            }
            if (instance == null || character == null)
            {
                yield break;
            }
            try
            {
                SpawnPingNow(instance, point, hitNormal, character, cfg);
            }
            catch (Exception e)
            {
                _log?.LogError($"Delayed anti-spam ping spawn failed: {e}");
            }
        }

        private static void SpawnPingNow(PointPinger __instance, Vector3 point, Vector3 hitNormal, Character character, PluginConfig cfg)
        {
            PointPing prefabPing = __instance.pointPrefab.GetComponent<PointPing>();

            Vector3 pingerPosition = CharacterPositions.EffectivePosition(character);

            float visibility = 1f;
            if (!cfg.RemoveVisibilityCutoff.Value)
            {
                bool obstructed = Physics.Linecast(
                    pingerPosition, CharacterPositions.LocalViewpoint(), out _,
                    HelperFunctions.LayerType.TerrainMap.ToLayerMask());
                float distanceToLocal = Vector3.Distance(pingerPosition, CharacterPositions.LocalViewpoint());
                Vector2 v = prefabPing.visibilityFullNoneNoLos;
                visibility = 1f - Mathf.InverseLerp(
                    v.x, v.x + (v.y - v.x) * (obstructed ? prefabPing.NoLosVisibilityMul : 1f),
                    distanceToLocal);
                if (visibility <= 0f)
                {
                    return;
                }
            }

            if (_pingInstanceField.GetValue(__instance) is GameObject existing && existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject spawned = UnityEngine.Object.Instantiate(
                __instance.pointPrefab, point,
                Quaternion.LookRotation((point - pingerPosition).normalized, Vector3.up));
            _pingInstanceField.SetValue(__instance, spawned);

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

            // Detect/highlight items first so the generic ping's own distance
            // label can be suppressed when it did - showing both is redundant
            // (they'd sit right on top of each other) and the item's own
            // label is the more useful of the two.
            int itemPingCount = cfg.EnableItemPings.Value ? ItemPingSpawner.SpawnFor(point, pingColor, character) : 0;

            if (cfg.EnablePingOffScreenIndicator.Value || cfg.ShowPingDistanceLabel.Value || cfg.PingPlacement.Value != IndicatorPlacement.OffScreenOnly)
            {
                bool showPingDistance = cfg.ShowPingDistanceLabel.Value && itemPingCount == 0;
                PingWidgetLink.Attach(spawned, pingColor, cfg.EnablePingOffScreenIndicator.Value, showPingDistance, itemPingCount > 0);
            }

            UnityEngine.Object.Destroy(spawned, 2f);
        }

        /// <summary>
        /// The first `PingAntiSpamFreeSpamCount` pings in a row from the same
        /// player always go through instantly. Once that's exceeded, the
        /// player enters "slow mode": further pings are queued rather than
        /// shown immediately, spaced at least `PingAntiSpamSlowModeInterval-
        /// Seconds` apart (so a spammer is capped at that rate rather than
        /// being cut off) - each queued ping still eventually shows, up to
        /// `PingAntiSpamMaxQueueLength` outstanding at once; a further ping
        /// arriving once the queue's already full is dropped outright rather
        /// than growing an ever-larger backlog (ISSUES.md: the previous
        /// gradual-cooldown version could leave a burst of 20 pings all
        /// trickling in one-by-one for several seconds after the spammer
        /// stopped - capping the queue means at most
        /// `PingAntiSpamMaxQueueLength` pings are ever "in flight"). Slow
        /// mode itself only lifts once the player goes `PingAntiSpamReset-
        /// Seconds` without a new ping *and* their queue has fully drained -
        /// checked lazily on the next ping to arrive rather than via a
        /// separate timer. Never applied to the local player's own pings
        /// (maintainer feedback: this only throttles pings received from
        /// others, never the local player's own).
        /// </summary>
        private static bool TryAdmitPing(Character character, PluginConfig cfg, out float delaySeconds)
        {
            delaySeconds = 0f;

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
            state.LastPingTime = now;

            if (state.SlowMode && state.QueueLength == 0 && gap >= cfg.PingAntiSpamResetSeconds.Value)
            {
                state.SlowMode = false;
                state.FreeCount = 0;
            }

            if (!state.SlowMode)
            {
                state.FreeCount++;
                if (state.FreeCount <= cfg.PingAntiSpamFreeSpamCount.Value)
                {
                    return true;
                }
                state.SlowMode = true;
            }

            if (state.QueueLength >= cfg.PingAntiSpamMaxQueueLength.Value)
            {
                return false;
            }

            float slotTime = Mathf.Max(now, state.NextSlotTime) + cfg.PingAntiSpamSlowModeIntervalSeconds.Value;
            state.NextSlotTime = slotTime;
            state.QueueLength++;
            delaySeconds = slotTime - now;
            return true;
        }
    }
}
