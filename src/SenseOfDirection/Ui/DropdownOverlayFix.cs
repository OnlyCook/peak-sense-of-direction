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

        /// <summary>The name TMP gives the full-screen click-catcher it spawns behind an open list.</summary>
        private const string BlockerName = "Blocker";

        /// <summary>Above the menu's own canvas (30000), so the open list draws over the panel it came from.</summary>
        private const int ListSortingOrder = 30100;

        /// <summary>Between the menu and the open list: over everything the user could misclick, under the list's own options.</summary>
        private const int BlockerSortingOrder = ListSortingOrder - 1;

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

            RaiseBlocker();

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
                canvas.sortingOrder = ListSortingOrder;

                // A Canvas that overrides sorting needs its own raycaster, or its
                // options simply don't receive clicks once it's no longer part of
                // the menu canvas's own raycast tree.
                if (list.GetComponent<GraphicRaycaster>() == null)
                {
                    list.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
        }

        /// <summary>
        /// Makes clicking anywhere outside an open list close it, which is what
        /// TMP's blocker is *for* - it's a full-screen transparent Button wired to
        /// the dropdown's own Hide().
        ///
        /// It doesn't work here out of the box because TMP hardcodes the open
        /// list's canvas to sorting order 30000 and gives the blocker one less -
        /// 29999, which lands it *below* this menu's own canvas (also 30000). The
        /// blocker is then behind the panel, so every click outside the list hits
        /// the panel instead and the list just stays open. Lifting the blocker
        /// between the menu and the list restores the intended behaviour: clicks
        /// on the panel, on the dropdown's own button, or on empty screen all
        /// reach the blocker and close the list, while the options themselves
        /// still sit above it and stay clickable.
        /// </summary>
        private void RaiseBlocker()
        {
            // Parented to the dropdown's root canvas - which, while the menu is
            // open, is the menu's own root - not to the dropdown.
            Transform blocker = _rootCanvas.Find(BlockerName);
            if (blocker == null)
            {
                return;
            }

            var canvas = blocker.GetComponent<Canvas>();
            if (canvas == null || canvas.sortingOrder == BlockerSortingOrder)
            {
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = BlockerSortingOrder;
        }
    }
}
