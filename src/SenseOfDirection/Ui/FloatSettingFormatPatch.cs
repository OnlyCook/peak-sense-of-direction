using System;
using BepInEx.Logging;
using HarmonyLib;
using Zorro.Settings;
using Zorro.Settings.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Brings back decimals in the quick setup panel's slider rows.
    ///
    /// The game's own slider cell (<c>FloatSettingUI</c>) used to format its
    /// number box with the setting's own <c>FloatSetting.Expose</c>, which is
    /// where <see cref="ConfigFloatSetting"/> decides how many decimals to show.
    /// A game update replaced that with a private <c>GetFormattedString</c> that
    /// hardcodes <c>ToString("0")</c> for plain-number settings, so every value
    /// was rounded to a whole number (a 0-1 slider only ever read 0 or 1). The
    /// slider's real value was never affected, only what the box displayed.
    ///
    /// Only rows backed by this mod's own settings are touched; the game's own
    /// settings menu (and other mods' rows) keep the game's new formatting.
    /// </summary>
    internal static class FloatSettingFormatPatch
    {
        private static AccessTools.FieldRef<SettingInputUICell, Setting> _listeningSetting;

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var method = AccessTools.Method(typeof(FloatSettingUI), "GetFormattedString", new[] { typeof(float) });
                if (method == null)
                {
                    // Older game build: still formats through Expose, nothing to fix.
                    return;
                }

                _listeningSetting = AccessTools.FieldRefAccess<SettingInputUICell, Setting>("_listeningSetting");
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(FloatSettingFormatPatch), nameof(Prefix)));
                log.LogInfo("FloatSettingFormatPatch: patched FloatSettingUI.GetFormattedString.");
            }
            catch (Exception e)
            {
                // The game changed shape again (likely fixing this itself) - leave it be.
                log.LogWarning($"FloatSettingFormatPatch: not applied, the game's slider cell changed ({e.Message}). Harmless; slider rows use the game's own formatting.");
            }
        }

        private static bool Prefix(FloatSettingUI __instance, float value, ref string __result)
        {
            try
            {
                if (_listeningSetting != null && _listeningSetting(__instance) is FloatSetting setting && setting is IConfigBoundSetting)
                {
                    __result = setting.Expose(value);
                    return false;
                }
            }
            catch
            {
                // Fall through to the game's own formatting.
            }
            return true;
        }
    }
}
