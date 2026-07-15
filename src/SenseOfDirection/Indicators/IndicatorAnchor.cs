using System;
using SenseOfDirection.Compass;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// A single world-space thing tracked by <see cref="IndicatorManager"/>:
    /// a world-position source (a live player, a ping, a campfire, or a fixed
    /// dummy test point) plus the UI widget that represents it on screen.
    ///
    /// Generic on purpose - Mechanic 1 (player labels), Mechanic 2 (pings),
    /// and the campfire indicator all register their own widget hierarchy and
    /// let the shared manager handle the screen-space placement math.
    ///
    /// The Compass-prefixed fields below (Phase 7) are optional metadata read
    /// by <see cref="Compass.CompassManager"/> so it can render its own
    /// top-of-screen marker for this same anchor without each mechanic having
    /// to register twice - defaults mean "not shown on the compass" (CompassKind
    /// stays None) until a mechanic opts in.
    /// </summary>
    public class IndicatorAnchor
    {
        public readonly Func<Vector3> GetWorldPosition;

        /// <summary>Widget hidden entirely (position not updated) when this returns false.</summary>
        public Func<bool> IsActive = () => true;

        public float EdgeMarginPixels = 48f;

        /// <summary>
        /// Approximate on-screen footprint (width, height in canvas pixels)
        /// used by <see cref="LabelOverlapResolver"/> to keep this anchor's
        /// label from overlapping another's. Zero (default) opts this anchor
        /// out of overlap resolution entirely - e.g. anchors with no visible
        /// name/distance text worth avoiding.
        /// </summary>
        public Vector2 OverlapSize = Vector2.zero;

        /// <summary>
        /// Where this anchor's <see cref="OverlapSize"/> box actually sits
        /// relative to the tracked point. Most widgets aren't centred on it - a
        /// player label runs from its crown badge (+42) down to its status badge
        /// (-48), an item ping from its name (+38) down to its distance line
        /// (-30) - and treating them as if they were made overlap resolution
        /// both miss real collisions and invent ones that weren't there. Zero
        /// (default) means "box is centred on the tracked point".
        /// </summary>
        public Vector2 OverlapCenterOffset = Vector2.zero;

        /// <summary>
        /// How far this anchor's label may be nudged from its tracked position
        /// to clear an overlap. Anchors that move as a whole (a player label,
        /// the campfire - see <see cref="LabelWidget"/>) can afford more than one
        /// whose text slides away from an arrow/crosshair left standing at the
        /// real position, and a stack of 90px-tall player labels needs more than
        /// 56px each just to clear its neighbour.
        /// </summary>
        public float MaxOverlapOffset = LabelOverlapResolver.MaxOffsetMagnitude;

        /// <summary>
        /// When true, <see cref="IndicatorManager.ResolveLabelOverlaps"/> never
        /// lets this anchor's resolved vertical offset go positive (upward).
        /// <c>ItemPings.ItemPingWidget</c> sets this while showing a native item
        /// icon as its crosshair: its distance sub-line sits just below that
        /// icon and its name sits just above, so an upward nudge (clearing an
        /// overlap with another label above it) closes the distance line's own
        /// gap to the icon instead, and a big enough native icon can make that
        /// gap negative - the distance label ends up drawn over the icon. A
        /// downward nudge closes the name's gap to the icon instead, which
        /// reads far better if it has to happen at all (ISSUES.md: "if
        /// something should be overlapped there then the title not the
        /// distance label"). Left off (false) by default - only widgets with
        /// this specific above-icon-below layout need it.
        /// </summary>
        public bool OverlapOffsetDownwardOnly;

        /// <summary>
        /// Optional child of <see cref="Widget"/>, sitting at local (0,0),
        /// that holds just this anchor's informational text (name/distance)
        /// - not its arrow/crosshair. When set, overlap resolution nudges
        /// <em>this</em> transform's local <c>anchoredPosition</c> instead of
        /// <see cref="Widget"/>'s own, so an arrow/crosshair that needs to
        /// stay exactly on the tracked position (e.g. <c>Pings.PingWidget</c>/
        /// <c>ItemPings.ItemPingWidget</c>'s off-screen arrow) never moves,
        /// while its label text is free to shift slightly to avoid
        /// overlapping a neighboring label. Null (default) means overlap
        /// resolution nudges <see cref="Widget"/> itself instead - fine for
        /// anchors with no separate direction-indicating element (e.g.
        /// <c>Labels.PlayerLabel</c>, which just clamps quietly to the edge).
        /// </summary>
        public RectTransform LabelWidget;

        /// <summary>Root widget, repositioned every frame to the on-screen or clamped-edge point.</summary>
        public readonly RectTransform Widget;

        /// <summary>
        /// How off-screen this anchor currently is: 0 while its tracked point is
        /// on screen, 1 while it's clamped to the edge, and easing between the
        /// two for the duration of an on/off-screen flip - the exact same curve
        /// <see cref="IndicatorManager"/> slides the widget's position along, so
        /// anything keyed off this moves in lockstep with it.
        ///
        /// Written by <see cref="IndicatorManager"/> every frame; widgets only
        /// read it. Its one consumer today is per-area font scaling
        /// (<see cref="Common.HudFontScale"/>): a label is one widget that
        /// passes *through* both states rather than two separate widgets, so
        /// on-screen and off-screen font scales have to be blended, not picked.
        /// </summary>
        public float OffScreenBlend;

        /// <summary>
        /// What <see cref="IndicatorManager.UnregisterAnchor"/> should do with
        /// <see cref="Widget"/> when this anchor goes away. Null (default) means
        /// destroy it, which is right for a widget that's built once and lives
        /// as long as the thing it tracks (a player label, the campfire).
        /// Ping/item-ping widgets set this instead, to return the whole widget
        /// hierarchy to their own pool: they're created and thrown away
        /// constantly (one per ping, one per pinged item group), and building
        /// one means a handful of GameObjects with TMP text and Image
        /// components on them - the kind of churn that shows up as a hitch when
        /// several land in the same frame (ISSUES.md: "especially multiple
        /// items").
        /// </summary>
        public Action ReleaseWidget;

        /// <summary>
        /// Optional child shown only while off-screen and rotated to point
        /// toward the tracked world position. Null if this anchor has no
        /// off-screen arrow (e.g. it's only ever meaningful on-screen).
        /// </summary>
        public readonly RectTransform ArrowWidget;

        /// <summary>
        /// Whether <see cref="ArrowWidget"/> is spun to point at the tracked
        /// position (true - it's a directional arrow) or left upright (false).
        /// Item pings set this to false while showing the pinged item's own
        /// icon there instead of the dart (<c>use-native-item-ping-icons</c>):
        /// an item icon has no "point" to aim, and rotating one would just
        /// leave e.g. a bandage lying on its side. The clamped edge position
        /// still carries the direction, same as the widgets that never show an
        /// arrow at all (player labels, the campfire).
        /// </summary>
        public bool RotateArrowWidget = true;

        /// <summary>
        /// Optional child shown only while on-screen (the exact opposite of
        /// <see cref="ArrowWidget"/>) - e.g. the item-ping crosshair, which
        /// only makes sense overlaid on the actually-visible object, not
        /// while the off-screen arrow is pointing toward it instead. Null if
        /// this anchor has no such widget.
        /// </summary>
        public readonly RectTransform OnScreenOnlyWidget;

        /// <summary>None means this anchor never shows up on the compass tape.</summary>
        public CompassMarkerKind CompassKind = CompassMarkerKind.None;

        /// <summary>
        /// When true, <see cref="Compass.CompassManager.UpdateMarkers"/> snaps
        /// this anchor's marker straight to alpha 0 the instant it stops being
        /// structurally shown (<see cref="IsActive"/>/<see cref="IsCompassVisible"/>
        /// false, or <see cref="GetPlacement"/> no longer wants the compass at
        /// all), instead of gradually fading it out. False (default) keeps the
        /// existing gradual fade every other mechanic relies on (ISSUES.md:
        /// "fade out compass icons/labels on disappear and appear").
        ///
        /// The gradual fade leaves a marker visible, frozen at its last tracked
        /// position, for the whole fade window - fine for a player/campfire/ping
        /// anchor that rarely flips active twice within under a second, but a
        /// mechanic whose active state toggles on a single discrete
        /// equip/unequip action (<c>PirateCompass.PirateCompassLuggageIndicatorController</c>)
        /// can trivially reactivate mid-fade, and the stale frozen position
        /// (from before the player turned) is then visible for a moment before
        /// that frame's fresh position takes over - read in-game as the marker
        /// briefly popping in at the wrong spot before "catching up" to the
        /// right one. An instant snap to 0 means there is no partially-faded,
        /// stale-positioned frame to ever see: by the time it's active again,
        /// alpha is already 0, and the ordinary fade-in path (unaffected by
        /// this flag) takes it from there at the correct, freshly-computed
        /// position.
        /// </summary>
        public bool CompassInstantHide;

        /// <summary>Governs whether <see cref="Widget"/>/<see cref="ArrowWidget"/> vs. the compass marker (or both) are shown for this anchor.</summary>
        public Func<IndicatorPlacement> GetPlacement = () => IndicatorPlacement.OffScreenOnly;

        /// <summary>Extra compass-only visibility gate (e.g. player labels' own toggle-key/distance-gate state) on top of <see cref="IsActive"/>.</summary>
        public Func<bool> IsCompassVisible = () => true;

        public Func<Color> GetCompassColor = () => Color.white;

        /// <summary>Null/empty means no name text is available for this anchor (e.g. a generic point ping).</summary>
        public Func<string> GetCompassLabel = () => null;

        /// <summary>
        /// Overrides the icon <see cref="CompassKind"/> would otherwise pick,
        /// with one of the game's own sprites (item pings' <c>use-native-item-
        /// ping-icons</c>: the pinged item's real inventory icon). Such a
        /// sprite is real, pre-colored art rather than the mod's tintable
        /// white-fill/black-outline shapes, so <see cref="CompassMarkerWidget"/>
        /// draws it untinted - the same way it already treats the campfire's
        /// native HUD icon. Null (default, and whenever the pinged thing has no
        /// icon of its own) means "use the kind's own icon".
        /// </summary>
        public Func<Sprite> GetCompassIcon = () => null;

        public Func<bool> GetIsDead = () => false;
        public Func<bool> GetIsUnconscious = () => false;

        public IndicatorAnchor(Func<Vector3> getWorldPosition, RectTransform widget, RectTransform arrowWidget = null, RectTransform onScreenOnlyWidget = null)
        {
            GetWorldPosition = getWorldPosition;
            Widget = widget;
            ArrowWidget = arrowWidget;
            OnScreenOnlyWidget = onScreenOnlyWidget;
        }
    }
}
