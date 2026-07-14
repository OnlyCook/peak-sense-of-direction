using System;
using BepInEx.Configuration;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zorro.Core;
using Zorro.Settings;
using Zorro.Settings.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Builds one settings row per config entry: our own row shell (plate +
    /// label) wrapped around PEAK's *own* control widget.
    ///
    /// The control genuinely is the game's: <c>Setting.GetSettingUICell()</c>
    /// resolves to the matching prefab on Zorro's <c>InputCellMapper</c> singleton
    /// asset (float -> slider + number box, enum -> dropdown), and
    /// <c>SettingInputUICell.Setup</c> wires it to the setting. So the sliders and
    /// dropdowns here are not lookalikes, and this needs no PEAKLib dependency.
    ///
    /// The row *shell*, though, is ours. The game's own "SettingsCell" prefab was
    /// used at first and had to be abandoned: it lays the label and the control
    /// out side by side, which works on the game's full-width settings page but
    /// not in this menu's narrower column, where the controls' fixed minimum width
    /// simply overruns the label. Re-anchoring the prefab's own label/content
    /// transforms didn't work either - they aren't direct children of the row
    /// root, so the new rects landed in the wrong coordinate space and the label
    /// disappeared outright. Rebuilding the shell (a rounded plate, a label across
    /// the top, the control stretched across the bottom) is both simpler and fully
    /// under our control, and it's styled off the same palette
    /// (<see cref="PanelChrome"/>) and native font
    /// (<see cref="NativeAssets"/>) the rest of the menu uses, so it still reads
    /// as PEAK's own.
    /// </summary>
    internal static class NativeSettingCells
    {
        private const float LabelInset = 18f;
        private const float LabelHeight = 30f;
        private const float LabelTopPadding = 8f;
        private const float ControlHeight = 52f;
        private const float ControlBottomPadding = 12f;

        /// <summary>Total height of a row built by <see cref="CreateRow"/> - the menu sizes each row's LayoutElement to this.</summary>
        internal const float RowHeight = LabelTopPadding + LabelHeight + ControlHeight + ControlBottomPadding + 6f;

        /// <summary>Vertical padding between the row's control area and the dropdown's white box - see <see cref="FitDropdownBox"/>.</summary>
        private const float DropdownBoxPadding = 4f;

        private const float RowCornerRadius = 12f;

        /// <summary>Debug hierarchy dump: only ever done for the first dropdown row built. See <see cref="UiDebugDump"/>.</summary>
        private static bool _dumpedOneDropdown;

        /// <summary>The row plate: a shade darker than the panel, like the game's own settings rows.</summary>
        private static readonly Color RowPlateColor = new Color(0x2A / 255f, 0x38 / 255f, 0x7E / 255f, 0.92f);


        /// <summary>
        /// Whether the game's control prefabs are loaded yet. Only false very
        /// early in startup; retried on every call, the same lazy pattern
        /// <see cref="NativeAssets"/> uses.
        /// </summary>
        internal static bool TryFindPrefab()
        {
            InputCellMapper mapper = SingletonAsset<InputCellMapper>.Instance;
            return mapper != null && mapper.EnumSettingCell != null && mapper.FloatSettingCell != null;
        }

        /// <summary>
        /// Builds one settings row for <paramref name="bound"/> under
        /// <paramref name="parent"/>. Null if the setting's type has no native
        /// widget - the caller skips it rather than rendering a broken row.
        /// </summary>
        internal static GameObject CreateRow(RectTransform parent, IConfigBoundSetting bound, ISettingHandler handler, Action<string, string> onHover, RectTransform rootCanvas)
        {
            GameObject controlPrefab = bound.Setting.GetSettingUICell();
            if (controlPrefab == null)
            {
                return null;
            }

            GameObject rowGo = CreatePlate(parent, bound.DisplayName);
            var rowRect = (RectTransform)rowGo.transform;

            BuildLabel(rowRect, bound.DisplayName);
            BuildControl(rowRect, controlPrefab, bound, handler, rootCanvas);
            AttachHoverDescription(rowGo, bound.Tooltip, bound.DefaultValueText, onHover);

            return rowGo;
        }

        /// <summary>
        /// A rebind row for a <c>ConfigEntry&lt;KeyCode&gt;</c>: the same plate and
        /// label as any other row, with a click-to-rebind button where the native
        /// control would be.
        ///
        /// The button is ours because PEAK has nothing to borrow. <c>InputCellMapper</c>
        /// has a <c>KeyCodeSettingCell</c> field, but Zorro ships no
        /// <c>KeyCodeSettingUI</c> to drive it - the settings-UI scripts stop at
        /// float/int/enum/string/button. (PEAKLib hits the same wall and builds its
        /// own rebind cell out of a mutilated FloatSettingCell.) Ours is styled off
        /// the dropdown's white box so the two read as the same family.
        /// </summary>
        internal static GameObject CreateKeyBindRow(RectTransform parent, ConfigEntry<KeyCode> entry, Action<string, string> onHover)
        {
            string displayName = ConfigSettingNaming.DisplayName(entry);

            GameObject rowGo = CreatePlate(parent, displayName);
            var rowRect = (RectTransform)rowGo.transform;

            BuildLabel(rowRect, displayName);
            BuildKeyBindControl(rowRect, entry);
            AttachHoverDescription(rowGo, ConfigSettingNaming.Tooltip(entry), entry.DefaultValue.ToString().ToUpperInvariant(), onHover);

            return rowGo;
        }

        private static GameObject CreatePlate(RectTransform parent, string displayName)
        {
            var rowGo = new GameObject("SoD.SettingRow." + displayName, typeof(RectTransform), typeof(Image));
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.SetParent(parent, false);

            var plate = rowGo.GetComponent<Image>();
            plate.sprite = PanelChrome.MakeRoundedSprite(64, RowCornerRadius, borderThickness: 0f, fill: Color.white, border: Color.white);
            plate.type = Image.Type.Sliced;
            plate.color = RowPlateColor;

            // Also the row's hover area: a raycast target spanning the whole plate,
            // so moving the pointer anywhere over the row (not just exactly over the
            // label or the control) surfaces its description.
            plate.raycastTarget = true;

            return rowGo;
        }

        /// <summary>
        /// The rebind button, built by cannibalising a real <c>FloatSettingCell</c> -
        /// the same trick PEAKLib.ModConfig uses, and for the same reason.
        ///
        /// Building the box ourselves was the mistake. Nothing in PEAK's UI has a
        /// clean edge: every white box is drawn with a hand-torn, jagged one, and
        /// no sprite we bake reproduces that. Copying the dropdown's sprite onto a
        /// box of our own didn't work either - that one is the dropdown's flat
        /// blurred background, which is why it came out soft-edged rather than
        /// jagged.
        ///
        /// The jagged box the eye is actually matching against is the *number box*
        /// on the slider row, sitting two rows up. So we take the prefab it comes
        /// from, keep that box exactly as it is - art, material, torn edge and all -
        /// and throw away the parts that make it a number field: the slider, the
        /// placeholder, and the TMP_InputField itself. What's left is a box with the
        /// game's own art and a text child in it, which is precisely a button.
        /// </summary>
        private static void BuildKeyBindControl(RectTransform row, ConfigEntry<KeyCode> entry)
        {
            var holderGo = new GameObject("Control", typeof(RectTransform));
            var holder = (RectTransform)holderGo.transform;
            holder.SetParent(row, false);

            holder.anchorMin = new Vector2(0f, 0f);
            holder.anchorMax = new Vector2(1f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.offsetMin = new Vector2(LabelInset, ControlBottomPadding);
            holder.offsetMax = new Vector2(-LabelInset, ControlBottomPadding + ControlHeight);

            InputCellMapper mapper = SingletonAsset<InputCellMapper>.Instance;
            if (mapper == null || mapper.FloatSettingCell == null)
            {
                return;
            }

            GameObject cell = UnityEngine.Object.Instantiate(mapper.FloatSettingCell, holder);
            cell.SetActive(true);

            var cellRect = (RectTransform)cell.transform;
            cellRect.anchorMin = Vector2.zero;
            cellRect.anchorMax = Vector2.one;
            cellRect.offsetMin = Vector2.zero;
            cellRect.offsetMax = Vector2.zero;

            var cellUi = cell.GetComponent<FloatSettingUI>();
            if (cellUi == null || cellUi.inputField == null)
            {
                UnityEngine.Object.Destroy(cell);
                return;
            }

            TMP_InputField field = cellUi.inputField;
            GameObject boxGo = field.gameObject;

            // Grabbed before the input field is destroyed - it's the field's own
            // caption, and it stays behind as a plain text child once the field
            // that drove it is gone.
            TMP_Text text = field.textComponent;

            if (field.placeholder != null)
            {
                UnityEngine.Object.DestroyImmediate(field.placeholder.gameObject);
            }

            if (cellUi.slider != null)
            {
                UnityEngine.Object.DestroyImmediate(cellUi.slider.gameObject);
            }

            // The cell's own script would otherwise keep trying to drive a slider
            // and a field that no longer exist.
            UnityEngine.Object.DestroyImmediate(field);
            UnityEngine.Object.DestroyImmediate(cellUi);

            // The box was authored to sit beside a slider, so it's narrow and
            // left-hugging; here it's the whole control.
            var boxRect = (RectTransform)boxGo.transform;
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.one;
            boxRect.offsetMin = new Vector2(20f, DropdownBoxPadding);
            boxRect.offsetMax = new Vector2(-20f, -DropdownBoxPadding);

            if (text != null)
            {
                var textRect = (RectTransform)text.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(14f, 0f);
                textRect.offsetMax = new Vector2(-14f, 0f);

                // Centred, like PEAKLib.ModConfig's rebind button - a lone key name
                // reads as a value, not as the start of a sentence.
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 26f;
                text.raycastTarget = false;
            }

            var control = boxGo.AddComponent<KeyRebindControl>();
            control.Entry = entry;
            control.Label = text;
            control.ShowCurrentKey();

            var button = boxGo.GetComponent<Button>();
            if (button == null)
            {
                button = boxGo.AddComponent<Button>();
            }

            button.onClick.AddListener(control.BeginListening);
        }

        private static void BuildLabel(RectTransform row, string displayName)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(row, false);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(LabelInset, -LabelTopPadding - LabelHeight);
            rect.offsetMax = new Vector2(-LabelInset, -LabelTopPadding);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = displayName;
            text.fontSize = 21f;
            text.color = PanelChrome.BodyColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;

            // Shrink rather than clip: a long key ("ENABLE OFFSCREEN INDICATOR")
            // stays fully readable instead of being cut off mid-word, which is what
            // the game's own fixed-width label did to it.
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 21f;
            text.raycastTarget = false;

            if (NativeAssets.Font != null)
            {
                text.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null)
            {
                text.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }
        }

        private static void BuildControl(RectTransform row, GameObject controlPrefab, IConfigBoundSetting bound, ISettingHandler handler, RectTransform rootCanvas)
        {
            var holderGo = new GameObject("Control", typeof(RectTransform));
            var holder = (RectTransform)holderGo.transform;
            holder.SetParent(row, false);

            holder.anchorMin = new Vector2(0f, 0f);
            holder.anchorMax = new Vector2(1f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.offsetMin = new Vector2(LabelInset, ControlBottomPadding);
            holder.offsetMax = new Vector2(-LabelInset, ControlBottomPadding + ControlHeight);

            var control = UnityEngine.Object.Instantiate(controlPrefab, holder).GetComponent<SettingInputUICell>();
            control.gameObject.SetActive(true);

            // Every control prefab, dropdown or slider, is stretched to fill the row.
            var controlRect = (RectTransform)control.transform;
            controlRect.anchorMin = Vector2.zero;
            controlRect.anchorMax = Vector2.one;
            controlRect.offsetMin = Vector2.zero;
            controlRect.offsetMax = Vector2.zero;

            var dropdown = control.GetComponentInChildren<TMP_Dropdown>(includeInactive: true);
            if (dropdown != null)
            {
                FitDropdownBox(dropdown);
            }

            // The line that makes the widget live: reads the setting's current
            // value/range into the slider/dropdown and subscribes to its changes.
            // Our handler is inert - the write to the ConfigEntry happens in the
            // setting's own ApplyValue. See ConfigSettingHandler.
            control.Setup(bound.Setting, handler);

            // A dropdown's flyout would otherwise open inside the scrolling list's
            // mask and be clipped away entirely - see DropdownOverlayFix.
            DropdownOverlayFix.Attach(control.gameObject, rootCanvas);

            // One row is enough to see the prefab's structure, and dumping every
            // dropdown in the menu would bury it.
            if (dropdown != null && !_dumpedOneDropdown)
            {
                _dumpedOneDropdown = true;
                UiDebugDump.DumpDeferred(row.gameObject, "dropdown row: " + bound.DisplayName);
            }
        }

        /// <summary>
        /// Gives the dropdown's white box - and so its caption, the selected value
        /// that is the entire point of the control - a usable height.
        ///
        /// The box is not the control root: it's a child ("Dropdown") stretched to
        /// the root but inset by a hardcoded <c>sizeDelta</c> of (-40, -30). That
        /// -30 is the bug this method exists for. It's authored against the tall
        /// row of PEAK's own full-width settings page; against this menu's 52px
        /// control area it leaves a 22px box, far too short for the 26pt caption
        /// inside it, which then fails to render at all - a box that looks empty
        /// rather than merely cramped. Resizing the *root* can never fix that,
        /// because the inset is subtracted from whatever the root's height is.
        ///
        /// The horizontal inset is left as authored; only the vertical one is
        /// overridden, to a small even padding.
        /// </summary>
        private static void FitDropdownBox(TMP_Dropdown dropdown)
        {
            var boxRect = (RectTransform)dropdown.transform;
            boxRect.sizeDelta = new Vector2(boxRect.sizeDelta.x, -DropdownBoxPadding * 2f);
            boxRect.anchoredPosition = new Vector2(boxRect.anchoredPosition.x, 0f);

            TMP_Text caption = dropdown.captionText;
            if (caption == null)
            {
                return;
            }

            caption.gameObject.SetActive(true);

            // Right inset leaves room for the dropdown's own arrow glyph.
            var rect = (RectTransform)caption.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 0f);
            rect.offsetMax = new Vector2(-34f, 0f);

            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.enableWordWrapping = false;

            // The prefab's caption auto-sizes but is authored at 26pt with no room
            // to shrink; a longer value ("MARKER + LABEL") in this menu's narrower
            // column needs one.
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 14f;
            caption.fontSizeMax = 26f;
        }

        /// <summary>
        /// Each config entry already carries a written-out explanation; hovering
        /// a row surfaces it in the menu's shared description line rather than
        /// leaving the user to guess from the key name alone (or go read the
        /// config file, which the whole menu exists to avoid).
        /// </summary>
        private static void AttachHoverDescription(GameObject rowGo, string tooltip, string defaultValueText, Action<string, string> onHover)
        {
            if (string.IsNullOrEmpty(tooltip) || onHover == null)
            {
                return;
            }

            var hover = rowGo.AddComponent<SettingRowHover>();
            hover.Tooltip = tooltip;
            hover.DefaultValueText = defaultValueText;
            hover.OnHover = onHover;
        }
    }

    /// <summary>
    /// The click-to-rebind button: shows the bound key, and on click listens for
    /// the next one and writes it to the config entry.
    ///
    /// The key is read from <c>Event.current</c> in OnGUI rather than by polling
    /// <c>Input.GetKeyDown</c> over every <see cref="KeyCode"/>, which is both the
    /// cheaper way to ask "what was just pressed" and the only one that reports the
    /// physical key rather than guessing at it.
    /// </summary>
    internal class KeyRebindControl : MonoBehaviour
    {
        internal ConfigEntry<KeyCode> Entry;
        internal TMP_Text Label;

        /// <summary>
        /// Whether *any* rebind button is currently waiting for a key. The menu
        /// checks this before acting on its own hotkeys: while a rebind is armed,
        /// Escape has to mean "cancel this rebind", and the toggle/open keys have
        /// to be bindable rather than firing what they're bound to.
        /// </summary>
        internal static bool IsCapturing { get; private set; }

        internal void BeginListening()
        {
            IsCapturing = true;
            SetCaption(PreviewMenuLocalization.Current.PressAKey);
        }

        internal void ShowCurrentKey() => SetCaption(Entry.Value.ToString().ToUpperInvariant());

        private void SetCaption(string caption)
        {
            if (Label != null)
            {
                Label.text = caption;
            }
        }

        private void OnGUI()
        {
            if (!IsCapturing || Label == null)
            {
                return;
            }

            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None)
            {
                return;
            }

            IsCapturing = false;

            // Escape cancels rather than binding itself: it's the menu's own close
            // key, and binding it would leave the player unable to shut the menu.
            if (e.keyCode != KeyCode.Escape)
            {
                Entry.Value = e.keyCode;
            }

            ShowCurrentKey();
            e.Use();
        }

        private void OnDisable()
        {
            // A row destroyed mid-rebind (tab switch, menu close) would otherwise
            // leave the flag stuck on and the menu's own keys dead.
            IsCapturing = false;
        }
    }

    /// <summary>Reports its row's config description to the menu's description line while the pointer is over it.</summary>
    internal class SettingRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal string Tooltip;
        internal string DefaultValueText;
        internal Action<string, string> OnHover;

        public void OnPointerEnter(PointerEventData eventData) => OnHover?.Invoke(Tooltip, DefaultValueText);

        public void OnPointerExit(PointerEventData eventData) => OnHover?.Invoke(null, null);
    }
}
