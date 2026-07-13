using System;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zorro.Core;
using Zorro.Settings;

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

        private const float RowCornerRadius = 12f;

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
        internal static GameObject CreateRow(RectTransform parent, IConfigBoundSetting bound, ISettingHandler handler, Action<string> onHover, RectTransform rootCanvas)
        {
            GameObject controlPrefab = bound.Setting.GetSettingUICell();
            if (controlPrefab == null)
            {
                return null;
            }

            var rowGo = new GameObject("SoD.SettingRow." + bound.DisplayName, typeof(RectTransform), typeof(Image));
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

            BuildLabel(rowRect, bound.DisplayName);
            BuildControl(rowRect, controlPrefab, bound, handler, rootCanvas);
            AttachHoverDescription(rowGo, bound.Tooltip, onHover);

            return rowGo;
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

            var controlRect = (RectTransform)control.transform;

            // Authored height, read before anything is re-anchored.
            float authoredHeight = controlRect.rect.height;

            // A dropdown is stretched horizontally only, and keeps the height it was
            // authored at. Stretching it vertically as well (which the slider is
            // perfectly happy with) collapses its internals: its background and its
            // value label are anchored for the prefab's own height, so at any other
            // height the box goes thin and the current value - the one thing the
            // control exists to show - stops being visible at all.
            var dropdown = control.GetComponentInChildren<TMP_Dropdown>(includeInactive: true);

            // A fitter on the control root would re-derive its size from its content
            // on the next layout pass and undo whatever is set below.
            foreach (var fitter in control.GetComponents<ContentSizeFitter>())
            {
                UnityEngine.Object.DestroyImmediate(fitter);
            }

            if (dropdown != null)
            {
                // Clamped, not taken verbatim: if the prefab happens to be authored
                // short, the box would stay unreadably thin - and if it's authored
                // taller than the row, it would spill out of it.
                float height = Mathf.Clamp(authoredHeight, 40f, ControlHeight);

                controlRect.anchorMin = new Vector2(0f, 0.5f);
                controlRect.anchorMax = new Vector2(1f, 0.5f);
                controlRect.pivot = new Vector2(0.5f, 0.5f);
                controlRect.sizeDelta = new Vector2(0f, height);
                controlRect.anchoredPosition = Vector2.zero;

                StretchCaption(dropdown);
            }
            else
            {
                controlRect.anchorMin = Vector2.zero;
                controlRect.anchorMax = Vector2.one;
                controlRect.offsetMin = Vector2.zero;
                controlRect.offsetMax = Vector2.zero;
            }

            // The line that makes the widget live: reads the setting's current
            // value/range into the slider/dropdown and subscribes to its changes.
            // Our handler is inert - the write to the ConfigEntry happens in the
            // setting's own ApplyValue. See ConfigSettingHandler.
            control.Setup(bound.Setting, handler);

            // A dropdown's flyout would otherwise open inside the scrolling list's
            // mask and be clipped away entirely - see DropdownOverlayFix.
            DropdownOverlayFix.Attach(control.gameObject, rootCanvas);
        }

        /// <summary>
        /// Makes sure the dropdown's caption - the currently selected value, which
        /// is the whole point of the control - actually fills the box and is
        /// visible. It's anchored for the prefab's own dimensions, and the box is
        /// no longer at those dimensions once it's been widened to this menu's row.
        /// </summary>
        private static void StretchCaption(TMP_Dropdown dropdown)
        {
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
        }

        /// <summary>
        /// Each config entry already carries a written-out explanation; hovering
        /// a row surfaces it in the menu's shared description line rather than
        /// leaving the user to guess from the key name alone (or go read the
        /// config file, which the whole menu exists to avoid).
        /// </summary>
        private static void AttachHoverDescription(GameObject rowGo, string tooltip, Action<string> onHover)
        {
            if (string.IsNullOrEmpty(tooltip) || onHover == null)
            {
                return;
            }

            var hover = rowGo.AddComponent<SettingRowHover>();
            hover.Tooltip = tooltip;
            hover.OnHover = onHover;
        }
    }

    /// <summary>Reports its row's config description to the menu's description line while the pointer is over it.</summary>
    internal class SettingRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal string Tooltip;
        internal Action<string> OnHover;

        public void OnPointerEnter(PointerEventData eventData) => OnHover?.Invoke(Tooltip);

        public void OnPointerExit(PointerEventData eventData) => OnHover?.Invoke(null);
    }
}
