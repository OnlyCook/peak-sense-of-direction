using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Makes a <see cref="TMP_Dropdown"/> inside the menu's scrolling settings
    /// list actually show its flyout.
    ///
    /// When a dropdown opens, TMP parents the spawned "Dropdown List" to the
    /// template's own parent - i.e. *inside* the dropdown control, which here
    /// sits inside the settings list's mask. The list expands downward past the
    /// bottom of that mask and is clipped away, so clicking a dropdown appears to
    /// do nothing at all.
    ///
    /// Rather than fight the mask, the list is lifted out of it: the moment it
    /// appears it's reparented to the menu's root canvas, keeping its world
    /// position (so it still sits exactly under its own dropdown) and made the
    /// last sibling so it draws above the blocker TMP puts down to catch
    /// click-outside. TMP destroys the list itself on close regardless of where it
    /// ended up, so nothing here has to clean up after it.
    /// </summary>
    internal class DropdownOverlayFix : MonoBehaviour
    {
        /// <summary>The name TMP gives the instantiated list. Matched by name because TMP exposes no hook or event for "the list just opened".</summary>
        private const string DropdownListName = "Dropdown List";

        private TMP_Dropdown _dropdown;
        private RectTransform _rootCanvas;

        internal static void Attach(GameObject control, RectTransform rootCanvas)
        {
            var dropdown = control.GetComponentInChildren<TMP_Dropdown>(includeInactive: true);
            if (dropdown == null)
            {
                return;
            }

            var fix = control.AddComponent<DropdownOverlayFix>();
            fix._dropdown = dropdown;
            fix._rootCanvas = rootCanvas;
        }

        private void LateUpdate()
        {
            if (_dropdown == null)
            {
                return;
            }

            Transform list = _dropdown.transform.Find(DropdownListName);
            if (list == null)
            {
                return;
            }

            // worldPositionStays: the list has already been positioned under its
            // dropdown by TMP, and that placement is what we want to preserve -
            // we're only changing which subtree (and therefore which mask) it
            // belongs to.
            list.SetParent(_rootCanvas, worldPositionStays: true);
            list.SetAsLastSibling();

            // The list carries its own Canvas so TMP can sort it above the rest of
            // the UI; left as-is it inherits the root's order and can end up behind
            // the blocker. Forcing it above keeps its options clickable.
            var canvas = list.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 30100;

                // A Canvas that overrides sorting needs its own raycaster, or its
                // options simply don't receive clicks once it's no longer part of
                // the menu canvas's own raycast tree.
                if (list.GetComponent<GraphicRaycaster>() == null)
                {
                    list.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
        }
    }
}
