using System;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Hides dead-player labels left behind once their segment unloads. Patches
    /// the private Campfire.Light_Rpc (not GoToSegment) since it's
    /// the last place the lit campfire's own position (__instance) is
    /// still around -- [PunRPC] means this fires on every client right when
    /// their own game lights that fire, no extra sync needed.
    /// </summary>
    public static class DeadLabelCullPatches
    {
        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var lightRpc = AccessTools.Method(typeof(Campfire), "Light_Rpc");
                harmony.Patch(lightRpc, postfix: new HarmonyMethod(typeof(DeadLabelCullPatches), nameof(LightRpcPostfix)));

                log.LogInfo("DeadLabelCullPatches: patched Campfire.Light_Rpc.");
            }
            catch (Exception e)
            {
                log.LogError($"DeadLabelCullPatches.Apply failed (non-fatal, dead labels won't be culled on segment advance): {e}");
            }
        }

        // updateSegment is false for a mini-run-ending campfire, which doesn't advance the run
        // nothing to cull for those
        private static void LightRpcPostfix(Campfire __instance, bool updateSegment)
        {
            if (!updateSegment || __instance == null)
            {
                return;
            }

            Common.Safe.Run(
                "DeadLabelCullPatches.LightRpcPostfix (culling distant dead labels)",
                () => PlayerLabelController.Instance.CullDistantDeadLabels(__instance.transform.position));
        }
    }
}
