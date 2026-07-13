using UnityEngine;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Registers the preview menu with PEAK's own window bookkeeping while it's
    /// open, which is what gives it a mouse cursor and freezes the player -
    /// no patching, no manual <see cref="Cursor"/> fiddling.
    ///
    /// The game recomputes <c>GUIManager.windowShowingCursor</c> /
    /// <c>windowBlockingInput</c> every frame by walking
    /// <see cref="MenuWindow.AllActiveWindows"/> and OR-ing together each
    /// member's <see cref="showCursorWhileOpen"/> / <see cref="blocksPlayerInput"/>.
    /// <c>CursorHandler</c> unlocks the cursor off the first flag;
    /// <c>Character.CanDoInput()</c> returns false off the second. So simply
    /// being *in that list* is the whole mechanism - hence
    /// <see cref="SetRegistered"/> rather than a call to <c>MenuWindow.Open()</c>,
    /// which is internal to Assembly-CSharp and unreachable from here anyway.
    ///
    /// This component deliberately sits on its own empty child GameObject rather
    /// than on the menu root: <c>MenuWindow</c>'s base <c>Start</c> hides
    /// <see cref="panel"/> (which defaults to its own GameObject) when it starts
    /// closed, and that must not take the actual menu UI down with it.
    /// </summary>
    internal class PreviewMenuWindow : MenuWindow
    {
        /// <summary>Never auto-open: the menu is opened by its key, and the base <c>Start</c> would otherwise open it the moment it's created.</summary>
        public override bool openOnStart => false;

        /// <summary>Both default to true on the base class already; stated explicitly because they're the entire reason this component exists.</summary>
        public override bool blocksPlayerInput => true;

        public override bool showCursorWhileOpen => true;

        /// <summary>The menu drives its own selection/closing (mouse-first), so none of the base's gamepad navigation/close-on-cancel behaviour applies.</summary>
        public override bool selectOnOpen => false;

        internal void SetRegistered(bool registered)
        {
            if (registered)
            {
                if (!AllActiveWindows.Contains(this))
                {
                    AllActiveWindows.Add(this);
                }
            }
            else
            {
                AllActiveWindows.Remove(this);
            }
        }
    }
}
