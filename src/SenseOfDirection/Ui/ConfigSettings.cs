using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Settings;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// A BepInEx <see cref="ConfigEntry{T}"/> dressed up as one of PEAK's own
    /// <see cref="Setting"/>s, so the game's *real* settings widgets (the slider
    /// + number box, the dropdown - see <see cref="NativeSettingCells"/>) can be
    /// pointed straight at this mod's config with no PEAKLib dependency.
    ///
    /// Every implementation below writes to its <see cref="ConfigEntry{T}"/> the
    /// moment the widget changes (<c>ApplyValue</c>), which is the whole point:
    /// the preview, the live HUD behind the menu, and the config file on disk
    /// all follow one source of truth. Nothing here ever touches PlayerPrefs or
    /// registers with the game's own settings handler, so none of it leaks into
    /// PEAK's settings menu - see <see cref="ConfigSettingHandler"/>.
    /// </summary>
    internal interface IConfigBoundSetting
    {
        /// <summary>The widget's label, e.g. "SHOW DISTANCE" for <c>show-distance</c>.</summary>
        string DisplayName { get; }

        /// <summary>The config entry's own description, shown as a hover tooltip.</summary>
        string Tooltip { get; }

        Setting Setting { get; }

        /// <summary>Writes the entry's shipped default back and pushes it to the widget.</summary>
        void ResetToDefault();

        /// <summary>Pulls the entry's current value back into the widget (e.g. after a reset elsewhere).</summary>
        void RefreshFromConfig();
    }

    /// <summary>
    /// The <see cref="ISettingHandler"/> handed to every settings widget we
    /// spawn. Deliberately inert: PEAK's widgets call
    /// <see cref="SaveSetting"/> whenever the user moves a slider, and the real
    /// persistence already happened in the setting's own <c>ApplyValue</c>
    /// (which writes the <see cref="ConfigEntry{T}"/>, and BepInEx flushes that
    /// to disk itself).
    ///
    /// Passing our own handler rather than
    /// <c>GameHandler.Instance.SettingsHandler</c> is what keeps this mod's
    /// settings out of the game's own settings list entirely - they're never
    /// registered anywhere, they just exist for as long as the menu is open.
    /// </summary>
    internal class ConfigSettingHandler : ISettingHandler
    {
        public void SaveSetting(Setting setting)
        {
        }

        public T GetSetting<T>() where T : Setting => null;

        public IEnumerable<Setting> GetAllSettings() => Array.Empty<Setting>();
    }

    /// <summary>Shared naming/tooltip derivation, so every setting type reads the same.</summary>
    internal static class ConfigSettingNaming
    {
        /// <summary>
        /// <c>enable-player-labels</c> -> <c>ENABLE PLAYER LABELS</c>. Uppercase
        /// to match how PEAK renders its own setting labels (and how
        /// PEAKLib.ModConfig renders these same keys), hyphens to spaces because
        /// the key convention exists precisely so multi-word keys stay readable.
        /// </summary>
        internal static string DisplayName(ConfigEntryBase entry) =>
            entry.Definition.Key.Replace('-', ' ').ToUpperInvariant();

        internal static string Tooltip(ConfigEntryBase entry) => entry.Description?.Description ?? string.Empty;
    }

    /// <summary>Backs a <c>ConfigEntry&lt;float&gt;</c> with the game's slider + number-box cell.</summary>
    internal class ConfigFloatSetting : FloatSetting, IConfigBoundSetting
    {
        private readonly ConfigEntry<float> _entry;
        private readonly ISettingHandler _handler;

        public string DisplayName { get; }
        public string Tooltip { get; }
        public Setting Setting => this;

        internal ConfigFloatSetting(ConfigEntry<float> entry, ISettingHandler handler)
        {
            _entry = entry;
            _handler = handler;
            DisplayName = ConfigSettingNaming.DisplayName(entry);
            Tooltip = ConfigSettingNaming.Tooltip(entry);

            // The slider's range comes from the entry's own AcceptableValueRange
            // rather than a second, hand-maintained table that could drift out of
            // sync with what the config actually accepts.
            float2 range = GetMinMaxValue();
            MinValue = range.x;
            MaxValue = range.y;
            Value = Mathf.Clamp(entry.Value, MinValue, MaxValue);
        }

        /// <summary>The one direction that matters: widget -> config -> live HUD + preview.</summary>
        public override void ApplyValue() => _entry.Value = Value;

        /// <summary>No-op: <see cref="ApplyValue"/> already wrote the config entry, and BepInEx persists that itself.</summary>
        public override void Save(ISettingsSaveLoad saver)
        {
        }

        /// <summary>Never read from PlayerPrefs - the config entry is the only source of truth.</summary>
        public override void Load(ISettingsSaveLoad loader) => RefreshFromConfig();

        protected override float GetDefaultValue() => (float)_entry.DefaultValue;

        protected override float2 GetMinMaxValue()
        {
            if (_entry.Description?.AcceptableValues is AcceptableValueRange<float> range)
            {
                return new float2(range.MinValue, range.MaxValue);
            }

            // Every float in the previewable sections ships with a range today;
            // this is only a floor so an un-ranged one added later still renders
            // a usable slider instead of a degenerate zero-width one.
            float value = (float)_entry.DefaultValue;
            return new float2(0f, Mathf.Max(1f, value * 2f));
        }

        /// <summary>
        /// How the number box renders the value. The base class formats
        /// everything as "F" (two decimals), which reads badly for the wide
        /// ranges in this mod ("640.00" pixels, "1000.00" meters); anything on a
        /// range wider than ~20 units is shown as a whole number instead, and
        /// only genuinely fine-grained dials (scale multipliers, 0.5-3) keep
        /// their decimals.
        /// </summary>
        public override string Expose(float result) =>
            MaxValue - MinValue > 20f
                ? Mathf.RoundToInt(result).ToString()
                : result.ToString("0.00");

        public void ResetToDefault() => SetValue(GetDefaultValue(), _handler, fromUI: false);

        public void RefreshFromConfig() => SetValue(_entry.Value, _handler, fromUI: false);
    }

    /// <summary>
    /// Backs a <c>ConfigEntry&lt;bool&gt;</c> with the game's own OFF/ON
    /// dropdown - vanilla has no checkbox widget, every boolean setting in PEAK
    /// is an <see cref="OffOnSetting"/>, so this matches by construction.
    /// </summary>
    internal class ConfigBoolSetting : OffOnSetting, IConfigBoundSetting
    {
        private readonly ConfigEntry<bool> _entry;
        private readonly ISettingHandler _handler;

        public string DisplayName { get; }
        public string Tooltip { get; }
        public Setting Setting => this;

        internal ConfigBoolSetting(ConfigEntry<bool> entry, ISettingHandler handler)
        {
            _entry = entry;
            _handler = handler;
            DisplayName = ConfigSettingNaming.DisplayName(entry);
            Tooltip = ConfigSettingNaming.Tooltip(entry);
            Value = entry.Value ? OffOnMode.ON : OffOnMode.OFF;
        }

        public override void ApplyValue() => _entry.Value = Value == OffOnMode.ON;

        public override void Save(ISettingsSaveLoad saver)
        {
        }

        public override void Load(ISettingsSaveLoad loader) => RefreshFromConfig();

        protected override OffOnMode GetDefaultValue() =>
            (bool)_entry.DefaultValue ? OffOnMode.ON : OffOnMode.OFF;

        /// <summary>
        /// Null, not a list: <c>EnumSettingUI</c> only asks for localized choices
        /// when the setting implements <c>ILocalizedEnumSetting</c> (which this
        /// deliberately doesn't), and otherwise falls back to the plain enum
        /// names - "OFF"/"ON", exactly what vanilla's own toggles show.
        /// </summary>
        public override List<UnityEngine.Localization.LocalizedString> GetLocalizedChoices() => null;

        public void ResetToDefault() => SetValue((int)GetDefaultValue(), _handler, fromUI: false);

        public void RefreshFromConfig() => SetValue(_entry.Value ? 1 : 0, _handler, fromUI: false);
    }

    /// <summary>Backs a <c>ConfigEntry&lt;TEnum&gt;</c> with the game's dropdown cell.</summary>
    internal class ConfigEnumSetting<T> : EnumSetting<T>, IConfigBoundSetting where T : unmanaged, Enum
    {
        private readonly ConfigEntry<T> _entry;
        private readonly ISettingHandler _handler;

        public string DisplayName { get; }
        public string Tooltip { get; }
        public Setting Setting => this;

        internal ConfigEnumSetting(ConfigEntry<T> entry, ISettingHandler handler)
        {
            _entry = entry;
            _handler = handler;
            DisplayName = ConfigSettingNaming.DisplayName(entry);
            Tooltip = ConfigSettingNaming.Tooltip(entry);
            Value = entry.Value;
        }

        public override void ApplyValue() => _entry.Value = Value;

        public override void Save(ISettingsSaveLoad saver)
        {
        }

        public override void Load(ISettingsSaveLoad loader) => RefreshFromConfig();

        protected override T GetDefaultValue() => (T)_entry.DefaultValue;

        /// <summary>See <see cref="ConfigBoolSetting.GetLocalizedChoices"/> - the plain enum names are what we want.</summary>
        public override List<UnityEngine.Localization.LocalizedString> GetLocalizedChoices() => null;

        public void ResetToDefault() => SetValue(Convert.ToInt32(GetDefaultValue()), _handler, fromUI: false);

        public void RefreshFromConfig() => SetValue(Convert.ToInt32(_entry.Value), _handler, fromUI: false);
    }

    /// <summary>Builds the right <see cref="IConfigBoundSetting"/> for whatever type a config entry happens to be.</summary>
    internal static class ConfigSettingFactory
    {
        /// <summary>
        /// Null for a type the game has no widget for (only <c>KeyCode</c> today,
        /// which is deliberately left to the config file / PEAKLib.ModConfig -
        /// the preview menu's own open key can't be safely rebound from inside
        /// the menu it opens).
        /// </summary>
        internal static IConfigBoundSetting Create(ConfigEntryBase entry, ISettingHandler handler)
        {
            switch (entry)
            {
                case ConfigEntry<float> floatEntry:
                    return new ConfigFloatSetting(floatEntry, handler);
                case ConfigEntry<bool> boolEntry:
                    return new ConfigBoolSetting(boolEntry, handler);
            }

            Type type = entry.SettingType;
            if (type.IsEnum)
            {
                // Reflection only because ConfigEnumSetting<T> is generic over
                // the enum and the entry's type isn't known until runtime; the
                // constructed instance is a plain object afterwards.
                Type settingType = typeof(ConfigEnumSetting<>).MakeGenericType(type);
                return (IConfigBoundSetting)Activator.CreateInstance(settingType, entry, handler);
            }

            return null;
        }
    }
}
