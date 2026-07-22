using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Settings;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// The config preview menu: this mod's settings, rendered with PEAK's own
    /// settings widgets, above a live preview of what each one actually does.
    ///
    /// The point of it is that nothing in the preview is a mock-up. The frame at
    /// the top is a real screenshot with the mod's *real* widget classes
    /// (<see cref="Labels.PlayerLabel"/>, <see cref="Pings.PingWidget"/>,
    /// <see cref="ItemPings.ItemPingWidget"/>,
    /// <see cref="CampfireIndicator.CampfireWidget"/>, and the real compass tape)
    /// drawn over it, positioned by the same
    /// <see cref="Indicators.IndicatorManager"/> / <see cref="Compass.CompassManager"/>
    /// code that positions them in-game - just pointed at a fake camera instead
    /// of the player's. See <see cref="PreviewScene"/>.
    ///
    /// Every change writes straight through to the <see cref="ConfigEntry{T}"/>
    /// (see <see cref="ConfigSettings"/>), so the preview, the live HUD behind
    /// the menu, and the config file on disk never disagree - moving a slider
    /// here is exactly the same act as editing the config file, just visible.
    /// </summary>
    public class PreviewMenu : MonoBehaviour
    {
        private static PreviewMenu _instance;

        public static PreviewMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PreviewMenu");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PreviewMenu>();
                }
                return _instance;
            }
        }

        public bool IsOpen { get; private set; }

        // Laid out against the canvas scaler's 1920x1080 reference, so these are
        // "design pixels" and scale with the player's real resolution.
        private const float PanelWidth = 1780f;
        private const float PanelHeight = 1010f;
        private const float PanelPadding = 34f;
        private const float TitleHeight = 44f;
        private const float TabsHeight = 52f;

        /// <summary>Breathing room under the tabs. Without it both columns start flush against the tab buttons, the one place in the panel where nothing had any margin at all.</summary>
        private const float TabsBottomGap = 20f;

        /// <summary>
        /// The native font (Daruma Drop One) carries a lot of empty space above its
        /// caps and almost none below, so text centred in a box by TMP's own metrics
        /// sits visibly low. These lift it back to where the eye expects it - the
        /// title in its band, and a key badge's caption in its chip.
        /// </summary>
        private const float TitleNudge = 15f;

        private const float KeyBadgeTextNudge = 3f;

        /// <summary>Same low-empty-descender-space quirk as <see cref="KeyBadgeTextNudge"/>, applied to the tab buttons' own labels.</summary>
        private const float TabTextNudge = 3f;

        /// <summary>Nudges the whole footer row up slightly clear of the panel's bottom edge.</summary>
        private const float FooterYNudge = 5f;

        /// <summary>Shown in the description box until a setting is actually hovered - see <see cref="SetDescription"/>. Localized, so it's a property rather than a const.</summary>
        private static string DescriptionPlaceholder => PreviewMenuLocalization.Current.DescriptionPlaceholder;

        // Preview left, settings in a full-height column on the right. The first
        // version stacked them (preview above, settings below), which left the
        // settings barely two rows tall - every row crammed against the next with
        // nowhere to breathe. Side by side, the list gets the panel's whole height
        // and the preview still gets a big 16:9 window.
        private const float ColumnGap = 26f;

        /// <summary>A true 16:9 window, so an edge-clamped indicator sits where it really would on a real screen rather than at some panel-shaped aspect that would misrepresent it.</summary>
        private const float PreviewWidth = 1040f;
        private const float PreviewHeight = PreviewWidth * 9f / 16f;

        private const float SettingsWidth = PanelWidth - PanelPadding * 2f - PreviewWidth - ColumnGap;

        /// <summary>
        /// The description block's total budget. Sized with headroom above what
        /// English needs, because <see cref="RelayoutDescriptionBlock"/> splits
        /// this dynamically per-language (a translated description can run to
        /// several more wrapped lines than its English original) rather than
        /// assuming a fixed line count.
        /// </summary>
        private const float DescriptionHeight = 132f;

        /// <summary>Height of the "default: ..." sub-line under the description text.</summary>
        private const float DefaultValueLineHeight = 20f;

        /// <summary>Between the description text and the default-value sub-line under it.</summary>
        private const float DescriptionDefaultValueGap = 4f;

        /// <summary>Floor for the description text's own height, so a single-line description doesn't collapse the split to nothing.</summary>
        private const float MinDescriptionTextHeight = 24f;

        /// <summary>Between the preview frame and the hovered setting's description under it.</summary>
        private const float PreviewDescriptionGap = 16f;

        private const float FooterHeight = 34f;

        /// <summary>Each native settings row gets a deterministic height rather than whatever the prefab's own layout resolves to inside a foreign parent - and the stacked label-above-control layout decides what that is.</summary>
        private const float SettingRowHeight = NativeSettingCells.RowHeight;

        /// <summary>Vertical breathing room between rows - the reason the first pass read as "glued together" was that this was 4px.</summary>
        private const float SettingRowSpacing = 12f;

        private const float SettingsCornerRadius = 18f;

        /// <summary>How tall the fade at a scrollable edge of the settings list is.</summary>
        private const float ScrollFadeHeight = 52f;

        /// <summary>
        /// How many pixels of hidden content bring an edge's fade to full strength.
        /// It grows in proportion to what it's actually hiding, so a list overflowing
        /// by a hair gets a hint of one rather than the full effect - but the ramp is
        /// short (well under a row's height), because the fade's job is to say "there
        /// is more", and it can't do that while it's still too faint to notice.
        /// </summary>
        private const float ScrollFadeFullAt = 18f;

        /// <summary>
        /// How long the menu is left running-but-invisible behind the loading
        /// screen before it's revealed. Frames, not seconds, because what's being
        /// waited on is frames of work: the canvas layout pass, then the detached
        /// indicator/compass managers' Update/LateUpdate placing every widget. See
        /// <see cref="OpenWhenReady"/>.
        /// </summary>
        private const int SettleFrames = 3;

        /// <summary>How long the shared dim takes to fade in. See <see cref="ShowDim"/>.</summary>
        private const float DimFadeDuration = 0.25f;

        private GameObject _root;
        private GameObject _loadingRoot;
        private GameObject _dimRoot;
        private Image _dimImage;
        private float _dimFadeElapsed;

        /// <summary>The first open of a session snaps the dim on instead of fading it - see <see cref="ShowDim"/>.</summary>
        private bool _hasOpenedBefore;

        private CanvasGroup _canvasGroup;
        private JaggedPanel _panel;
        private RectTransform _settingsContent;
        private RectTransform _settingsViewport;
        private ScrollRect _settingsScroll;
        private Image _scrollFadeTop;
        private Image _scrollFadeBottom;
        private RectTransform _tabsRow;
        private TMP_Text _titleText;
        private TMP_Text _loadingText;
        private TMP_Text _footerCloseText;
        private TMP_Text _footerChangesText;
        private TMP_Text _descriptionText;
        private TMP_Text _defaultValueText;
        private float _descriptionCentreX;
        private float _descriptionTop;
        private PreviewScene _scene;
        private PreviewMenuWindow _window;

        private readonly ConfigSettingHandler _handler = new ConfigSettingHandler();
        private readonly List<PreviewTab> _tabs = new List<PreviewTab>();
        private readonly List<TabButton> _tabButtons = new List<TabButton>();

        /// <summary>The rows currently spawned for the selected tab - rebuilt wholesale on every tab switch.</summary>
        private readonly List<IConfigBoundSetting> _boundSettings = new List<IConfigBoundSetting>();

        private int _selectedTab;

        /// <summary>One tab: a label and the config entries it shows. Order within a tab is the order they're bound in <see cref="PluginConfig"/>, which is already grouped for reading.</summary>
        private class PreviewTab
        {
            internal string Name;
            internal List<ConfigEntryBase> Entries;
        }

        private class TabButton
        {
            internal Image Background;
            internal TMP_Text Label;
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            // The menu only makes sense inside a run: it borrows the game's own
            // settings widgets (only loaded with the game UI) and its preview
            // reads unitsToMeters/native icons off live game state.
            if (Character.localCharacter == null || !NativeSettingCells.TryFindPrefab())
            {
                Plugin.Instance.Log.LogWarning("Preview menu: the game's UI isn't ready yet - not opening.");
                return;
            }

            IsOpen = true;

            ShowDim();
            StartCoroutine(OpenWhenReady());

            // Read by ShowDim to decide fade-vs-snap, so it's only flipped once the
            // decision for *this* open has been made.
            _hasOpenedBefore = true;
        }

        /// <summary>
        /// Brings the menu up hidden, and only reveals it once it has actually
        /// settled.
        ///
        /// Two different costs are being covered, and only the first one is worth a
        /// loading screen. The first open has to bake every procedural sprite and
        /// instantiate every native settings cell - heavy enough to hitch, and long
        /// enough that the player would otherwise press the key and stare at
        /// nothing. Every open after that reuses all of it, and the only wait left
        /// is a few frames of settling: the preview's widgets are not placed by us,
        /// they're placed by the real
        /// <see cref="Indicators.IndicatorManager"/> / <see cref="Compass.CompassManager"/>
        /// code running in Update/LateUpdate, which needs those frames (and the
        /// canvas's own layout pass) before every label, icon and arrow has been
        /// projected against the fake camera and nudged clear of its neighbours.
        /// Reveal before that and the settling is visible as a flicker.
        ///
        /// So a cached open waits too - it just waits behind the dim, with no
        /// loading screen. Flashing one up for ~100ms is worse than the flicker it
        /// was there to hide: the eye reads the appear-and-vanish itself as the
        /// glitch.
        ///
        /// Note what is deliberately *not* done: the menu is not deactivated while
        /// it settles. It is fully active and merely transparent
        /// (<see cref="CanvasGroup.alpha"/> 0), because that settling is exactly
        /// the Update/LateUpdate work that would not run on an inactive object -
        /// and the widgets' positions depend on it. Hiding this with SetActive
        /// would resurrect the misplaced-label bug it exists to conceal.
        /// </summary>
        private IEnumerator OpenWhenReady()
        {
            // Cold: nothing is built yet, and building it is the expensive part.
            bool cold = _root == null;

            if (cold)
            {
                EnsureLoadingUi();
                _loadingRoot.SetActive(true);

                // The loading screen has to get one frame to itself, or the heavy
                // build below runs before it has ever been drawn and it's never
                // seen at all.
                yield return null;

                if (!IsOpen)
                {
                    yield break;
                }
            }

            if (!BuildBehindLoadingScreen())
            {
                Close();
                yield break;
            }

            // Plain frame yields, not WaitForEndOfFrame: that one does not reliably
            // resume here and left the menu stuck invisible behind a loading screen
            // that never went away. A frame is a frame either way - what's being
            // waited on is Update/LateUpdate having run, which `yield return null`
            // gives us.
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;

                if (!IsOpen)
                {
                    yield break;
                }
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            if (_loadingRoot != null)
            {
                _loadingRoot.SetActive(false);
            }
        }

        /// <summary>
        /// The build, hoisted out of the coroutine so it can be wrapped in a
        /// try/catch - a coroutine can't have one across a yield.
        ///
        /// This is not defensive padding. An exception thrown in here used to kill
        /// the coroutine silently, stranding the player looking at a loading screen
        /// that would never go away, with the menu built-but-transparent behind it
        /// and the cursor already freed. Unity's own log writer doesn't reach
        /// BepInEx's log in this game (see the handoff), so it left no trace at all.
        /// False means "it failed, and it's been logged" - the caller shuts the menu
        /// back down rather than leaving it half-open.
        /// </summary>
        private bool BuildBehindLoadingScreen()
        {
            try
            {
                if (_root == null)
                {
                    BuildUi();
                }

                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;

                _root.SetActive(true);
                _window.SetRegistered(true);
                RefreshLocalizedChrome();
                ShowTab(_selectedTab);

                return true;
            }
            catch (System.Exception e)
            {
                Plugin.Instance.Log.LogError("Preview menu: failed to build - " + e);
                return false;
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            // Closing mid-warm-up is why these are guarded: the menu may not have
            // been built yet, and OpenWhenReady bails on the next frame off IsOpen.
            if (_loadingRoot != null)
            {
                _loadingRoot.SetActive(false);
            }

            if (_dimRoot != null)
            {
                _dimRoot.SetActive(false);
            }

            if (_root == null)
            {
                return;
            }

            _root.SetActive(false);

            // Dropping out of AllActiveWindows is what re-locks the cursor and
            // hands input back to the player, via the game's own per-frame
            // recompute - see PreviewMenuWindow.
            _window.SetRegistered(false);
        }

        /// <summary>
        /// The loading screen: a dim and one line of text, and deliberately nothing
        /// else. It has to be cheap enough to build and show on the very frame the
        /// key is pressed - it exists to cover the expensive work, so it can't do
        /// any of its own. (No procedural sprites, no panel chrome.)
        /// </summary>
        private void EnsureLoadingUi()
        {
            if (_loadingRoot != null)
            {
                return;
            }

            _loadingRoot = new GameObject("SoD.PreviewMenu.Loading", typeof(RectTransform));
            _loadingRoot.transform.SetParent(transform, false);

            var canvas = _loadingRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above the menu's own canvas (30000), so it covers the menu while it
            // settles rather than being covered by it.
            canvas.sortingOrder = 30050;

            var scaler = _loadingRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // No dim of its own - it shares the menu's, see EnsureDimUi.
            _loadingText = CreateText(
                (RectTransform)_loadingRoot.transform, "LoadingText", PreviewMenuLocalization.Current.Loading,
                30f, PanelChrome.TitleColor, TextAlignmentOptions.Center);

            var rect = (RectTransform)_loadingText.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _loadingRoot.SetActive(false);
        }

        private void Update()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            // A rebind row is waiting for a key: every key belongs to it, including
            // the ones below. Otherwise binding the menu's own open key would slam
            // the menu shut the instant you pressed it. See KeyRebindControl.
            if (KeyRebindControl.IsCapturing)
            {
                return;
            }

            KeyCode key = cfg.PreviewMenuKey.Value;
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                Toggle();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            TickDimFade();
            TickScrollFades();

            // The toggle key itself is not read here: PreviewScene watches it, so
            // that Toggle and Hold - which are one state machine, not two keypresses
            // - stay in one place. See PreviewScene.ComputeLabelsVisible.

            // Escape closes the menu - but it's also the game's own pause key, so
            // the same press would otherwise open the pause menu right behind us.
            // Clearing input.pauseWasPressed here does NOT work (CharacterInput
            // rewrites it from the Input System every frame); the game's own
            // UpdatePaused has to be skipped for this one frame instead. See
            // PauseSuppressPatch.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                PauseSuppressPatch.SuppressNextOpen();
            }
        }

        private void BuildTabs()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            // Each tab is its mechanic's own config section plus the one General
            // setting that belongs to it (where that mechanic gets drawn), so a
            // single question - "how do I want player labels to look" - is
            // answered without leaving the tab.
            //
            // Order within a tab is deliberate: the master switch, then *where* the
            // mechanic is drawn (its placement), then everything else. Placement is
            // the setting that most changes what the preview looks like, so it
            // belongs where it's seen rather than buried at the bottom of a scroll.
            PreviewMenuLocalization.Strings strings = PreviewMenuLocalization.Current;

            _tabs.Add(new PreviewTab
            {
                Name = strings.TabPlayerLabels,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnablePlayerLabels, cfg.PlayerLabelPlacement, cfg.PlayerLabelDisplayMode,
                    cfg.PlayerLabelToggleKey, cfg.HoldShownDuration,
                    cfg.PlayerLabelMaxDistanceMeters, cfg.PlayerLabelNameFontSize, cfg.PlayerLabelDistanceFontSize,
                    cfg.ShowPlayerLabelDistance, cfg.ShowStatusBadges, cfg.PlayerLabelBadgeSizePixels, cfg.UseCharacterColor,
                    cfg.ReplaceVanillaLabels, cfg.ShowPlayerSkeleton,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = strings.TabPings,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.RemoveVisibilityCutoff, cfg.PingPlacement, cfg.EnablePingScaling, cfg.PingScaleMultiplier,
                    cfg.EnablePingRipple, cfg.EnablePingOffScreenIndicator, cfg.ShowPingDistanceLabel,
                    cfg.EnableGhostPing,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = strings.TabItemPings,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableItemPings, cfg.ItemPingPlacement, cfg.ItemPingDurationSeconds, cfg.EnableItemPingGrouping,
                    cfg.EnableCreaturePings, cfg.UseNativeItemPingIcons, cfg.ItemPingNameMode,
                    cfg.ShowItemPingDistance, cfg.EnableItemPingOffScreenIndicator,
                    cfg.EnableLuggagePing, cfg.LuggagePingKey,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = strings.TabCampfire,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableCampfireIndicator, cfg.CampfirePlacement, cfg.ShowCampfireDistance,
                    cfg.HideCampfireName,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = strings.TabCompass,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableCompass, cfg.CompassWidthPixels, cfg.CompassFovDegrees, cfg.CompassIconSizePixels,
                    cfg.CompassMarkerGapPixels, cfg.CompassVerticalOffsetPixels, cfg.CompassHorizontalOffsetPixels,
                    cfg.CompassElevationThresholdMeters, cfg.CompassShowNames, cfg.CompassShowDistances,
                    cfg.CompassShowDegreeNumbers, cfg.CompassLineColor, cfg.CompassLineThicknessMultiplier,
                    cfg.CompassClampIconsToEdge, cfg.CompassRequiresHoldingItem,
                    cfg.EnablePirateCompassLuggageIndicator, cfg.PirateCompassLuggagePlacement,
                },
            });

            // The overlap toggle and the font scales are the two things that cut
            // across every mechanic at once, so they share the tab where the
            // preview deliberately crowds its labels together.
            _tabs.Add(new PreviewTab
            {
                Name = strings.TabGeneral,
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableLabelOverlapAvoidance, cfg.AntiOverlapAnimationSpeedMultiplier, cfg.IndicatorIconSizeMultiplier,
                    cfg.OnScreenNameFontScale, cfg.OnScreenDistanceFontScale,
                    cfg.OffScreenNameFontScale, cfg.OffScreenDistanceFontScale,
                    cfg.CompassNameFontScale, cfg.CompassDistanceFontScale,
                },
            });
        }

        private void BuildUi()
        {
            BuildTabs();

            _root = new GameObject("SoD.PreviewMenu.Root", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Well above the game's own HUD/in-world canvases: unlike the mod's
            // indicator overlay (which deliberately sits *under* the vanilla UI),
            // this is a menu and must cover everything.
            canvas.sortingOrder = 30000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Match purely on height rather than the old width/height blend. At
            // 16:9 (the reference aspect) the two are identical - width and height
            // ratios agree, so the blend was a no-op. But on anything wider (21:9,
            // 32:9) the blend let the extra width drag the canvas's *vertical*
            // reference-pixel count down with it, shrinking the canvas below the
            // panel's own 1010px height and clipping the tab row and footer.
            // Matching height alone keeps the canvas a constant 1080 reference
            // pixels tall no matter the aspect, so the panel's height always fits;
            // only the available width changes with aspect, and anything wider
            // than 16:9 only ever has more of that, never less.
            scaler.matchWidthOrHeight = 1f;

            _root.AddComponent<GraphicRaycaster>();

            // How the menu is hidden while it settles behind the loading screen -
            // transparent but still running. See OpenWhenReady.
            _canvasGroup = _root.AddComponent<CanvasGroup>();

            var windowGo = new GameObject("Window");
            windowGo.transform.SetParent(_root.transform, false);
            _window = windowGo.AddComponent<PreviewMenuWindow>();

            // No dim of its own - it shares one with the loading screen, see EnsureDimUi.
            _panel = JaggedPanel.Create((RectTransform)_root.transform, "Panel", new Vector2(PanelWidth, PanelHeight));
            var panelRect = (RectTransform)_panel.transform;

            // The height match above guarantees the panel's 1010px height always
            // fits a canvas that's a constant 1080 reference pixels tall. Width
            // isn't covered by that: it still varies with aspect, and while
            // anything 16:9 or wider only ever has *more* of it than the panel
            // needs, an unusually narrow screen (e.g. a portrait monitor) could
            // have less. Shrinking the whole panel uniformly - rather than
            // reflowing it - is a no-op at every aspect this panel was actually
            // designed for, and only kicks in for that narrow edge case.
            float canvasWidthUnits = (float)Screen.width / Screen.height * scaler.referenceResolution.y;
            float fitScale = Mathf.Min(1f, (canvasWidthUnits - PanelPadding * 2f) / PanelWidth);
            if (fitScale < 1f)
            {
                panelRect.localScale = new Vector3(fitScale, fitScale, 1f);
            }

            float contentTop = PanelHeight * 0.5f - PanelPadding;

            BuildTitle(panelRect, contentTop);
            _tabsRow = BuildTabsRow(panelRect, contentTop - TitleHeight);

            // Both columns start under the tabs and run down to just above the
            // footer. The settings list fills that whole height; the left column's
            // content (the 16:9 preview with its description under it) is shorter
            // than it, so it's centred in it rather than hung from the top - which
            // left all of the slack pooled underneath as dead space.
            float columnTop = contentTop - TitleHeight - TabsHeight - TabsBottomGap;
            float columnBottom = -PanelHeight * 0.5f + PanelPadding + FooterHeight;

            float leftCentreX = -PanelWidth * 0.5f + PanelPadding + PreviewWidth * 0.5f;
            float rightCentreX = PanelWidth * 0.5f - PanelPadding - SettingsWidth * 0.5f;

            float leftBlockHeight = PreviewHeight + PreviewDescriptionGap + DescriptionHeight;
            float leftSlack = Mathf.Max(0f, (columnTop - columnBottom) - leftBlockHeight);
            float leftTop = columnTop - leftSlack * 0.5f;

            _scene = PreviewScene.Create(
                panelRect,
                new Vector2(PreviewWidth, PreviewHeight),
                new Vector2(leftCentreX, leftTop - PreviewHeight * 0.5f));

            BuildDescription(panelRect, leftCentreX, leftTop - PreviewHeight - PreviewDescriptionGap);
            BuildSettingsScroll(panelRect, rightCentreX, columnTop, columnBottom);
            BuildFooter(panelRect);
        }

        /// <summary>
        /// The dim behind everything, and deliberately <em>one</em> object shared by
        /// the loading screen and the menu rather than one in each.
        ///
        /// A dim per root would have to be handed off at the moment the loading
        /// screen is swapped for the menu, and any disagreement between the two -
        /// a frame where one is already gone and the other not yet up, or a second
        /// fade restarting from zero - reads as a flicker across the whole screen.
        /// Owning it here sidesteps the handoff entirely: it comes up when the menu
        /// is opened and goes down when it's closed, and nothing in between touches
        /// it.
        ///
        /// It sits on its own canvas below both (they're at 30000 / 30050) and is a
        /// raycast target, so it also swallows clicks that miss the panel and keeps
        /// a stray one from reaching the game behind the menu.
        /// </summary>
        private void EnsureDimUi()
        {
            if (_dimRoot != null)
            {
                return;
            }

            _dimRoot = new GameObject("SoD.PreviewMenu.Dim", typeof(RectTransform));
            _dimRoot.transform.SetParent(transform, false);

            var canvas = _dimRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29990;

            _dimRoot.AddComponent<GraphicRaycaster>();

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            var dimRect = (RectTransform)dimGo.transform;
            dimRect.SetParent(_dimRoot.transform, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            _dimImage = dimGo.GetComponent<Image>();
            _dimImage.color = PanelChrome.DimColor;
            _dimImage.raycastTarget = true;

            _dimRoot.SetActive(false);
        }

        /// <summary>
        /// Brings the dim up: faded in, except on the very first open of a session,
        /// where it's snapped straight to full.
        ///
        /// That exception isn't arbitrary. The first open is the one that has to
        /// bake every sprite and instantiate every native cell, and a fade running
        /// across those frames would be judged against a frame rate that build is
        /// busy destroying - it stutters, visibly. Snapping it is both honest about
        /// what's happening and steadier to look at. Every later open is cheap
        /// enough to fade smoothly, so it does. (The same call this mod's sibling
        /// makes, for the same reason - see SavePicker's skipDimFade.)
        /// </summary>
        private void ShowDim()
        {
            EnsureDimUi();
            _dimRoot.SetActive(true);

            _dimFadeElapsed = _hasOpenedBefore ? 0f : DimFadeDuration;
            ApplyDimAlpha();
        }

        private void ApplyDimAlpha()
        {
            float t = DimFadeDuration <= 0f ? 1f : Mathf.Clamp01(_dimFadeElapsed / DimFadeDuration);
            Color color = PanelChrome.DimColor;
            _dimImage.color = new Color(color.r, color.g, color.b, color.a * t);
        }

        /// <summary>Unscaled: the menu freezes the game, so a scaled delta would leave the fade frozen with it.</summary>
        private void TickDimFade()
        {
            if (_dimImage == null || _dimFadeElapsed >= DimFadeDuration)
            {
                return;
            }

            _dimFadeElapsed += Time.unscaledDeltaTime;
            ApplyDimAlpha();
        }

        private void BuildTitle(RectTransform panel, float top)
        {
            _titleText = CreateText(panel, "Title", TitleText, 34f, PanelChrome.TitleColor, TextAlignmentOptions.Center);
            var rect = (RectTransform)_titleText.transform;
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2f, TitleHeight);
            rect.anchoredPosition = new Vector2(0f, top - TitleHeight * 0.5f + TitleNudge);
        }

        private static string TitleText => "SENSE OF DIRECTION  " + PreviewMenuLocalization.Current.QuickSetup;

        private RectTransform BuildTabsRow(RectTransform panel, float top)
        {
            var rowGo = new GameObject("Tabs", typeof(RectTransform));
            var row = (RectTransform)rowGo.transform;
            row.SetParent(panel, false);
            row.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2f, TabsHeight);
            row.anchoredPosition = new Vector2(0f, top - TabsHeight * 0.5f);

            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabButtons.Add(CreateTabButton(row, _tabs[i].Name, i));
            }

            return row;
        }

        private TabButton CreateTabButton(RectTransform parent, string name, int index)
        {
            var go = new GameObject("Tab." + name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 230f;
            layoutElement.preferredHeight = TabsHeight - 8f;

            var background = go.GetComponent<Image>();
            background.sprite = PanelChrome.BadgeSprite();
            background.type = Image.Type.Sliced;

            TMP_Text label = CreateText(rect, "Label", name, 20f, PanelChrome.ChipTextColor, TextAlignmentOptions.Center);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(0f, TabTextNudge);
            labelRect.offsetMax = new Vector2(0f, TabTextNudge);

            int captured = index;
            go.GetComponent<Button>().onClick.AddListener(() => ShowTab(captured));

            return new TabButton { Background = background, Label = label };
        }

        private void BuildSettingsScroll(RectTransform panel, float centreX, float top, float bottom)
        {
            float height = top - bottom;

            // A rounded, slightly darkened plate the rows sit on, so the list reads
            // as its own surface within the panel rather than as text floating on
            // the panel's grain.
            var viewportGo = new GameObject("SettingsViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            var viewport = (RectTransform)viewportGo.transform;
            _settingsViewport = viewport;
            viewport.SetParent(panel, false);
            viewport.sizeDelta = new Vector2(SettingsWidth, height);
            viewport.anchoredPosition = new Vector2(centreX, bottom + height * 0.5f);

            // Also a raycast target regardless of how transparent it is: the
            // ScrollRect needs something under the pointer to catch wheel/drag.
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.sprite = PanelChrome.MakeRoundedSprite(
                128, SettingsCornerRadius, borderThickness: 0f,
                fill: Color.white, border: Color.white);
            viewportImage.type = Image.Type.Sliced;
            viewportImage.color = new Color(0f, 0f, 0f, 0.20f);

            var contentGo = new GameObject("SettingsContent", typeof(RectTransform));
            _settingsContent = (RectTransform)contentGo.transform;
            _settingsContent.SetParent(viewport, false);
            _settingsContent.anchorMin = new Vector2(0f, 1f);
            _settingsContent.anchorMax = new Vector2(1f, 1f);
            _settingsContent.pivot = new Vector2(0.5f, 1f);
            _settingsContent.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = SettingRowSpacing;
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            // Heights come from each row's own LayoutElement (set when the row is
            // spawned), not from the layout group: the native cell prefab is
            // authored for the game's own settings list and resolves to nothing
            // sensible if left to size itself inside a foreign parent.
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _settingsScroll = viewportGo.AddComponent<ScrollRect>();
            _settingsScroll.viewport = viewport;
            _settingsScroll.content = _settingsContent;
            _settingsScroll.horizontal = false;
            _settingsScroll.vertical = true;
            _settingsScroll.scrollSensitivity = 30f;
            _settingsScroll.movementType = ScrollRect.MovementType.Clamped;

            // Created after the content, so they're later siblings and therefore
            // drawn over the rows they're fading out rather than under them.
            _scrollFadeTop = CreateScrollFade(viewport, "ScrollFadeTop", top: true);
            _scrollFadeBottom = CreateScrollFade(viewport, "ScrollFadeBottom", top: false);
        }

        /// <summary>
        /// One edge's fade. Both edges share a single baked sprite (opaque at its
        /// bottom, clear at its top) - the top edge's copy is simply scaled to -1 on
        /// Y, which flips the ramp rather than baking a mirrored second texture.
        /// </summary>
        private static Image CreateScrollFade(RectTransform viewport, string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(viewport, false);

            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, ScrollFadeHeight);

            // Pivot at the strip's centre, not at the edge it's anchored to, precisely
            // because of the Y flip below: a flip is about the pivot, so an edge pivot
            // would throw the whole strip outside the viewport - where the RectMask2D
            // clips it, and the top fade is simply never seen. Centre-pivoted, it
            // flips in place.
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, top ? -ScrollFadeHeight * 0.5f : ScrollFadeHeight * 0.5f);
            rect.localScale = top ? new Vector3(1f, -1f, 1f) : Vector3.one;

            var image = go.GetComponent<Image>();
            image.sprite = PanelChrome.ScrollFadeSprite(
                Mathf.RoundToInt(SettingsWidth), Mathf.RoundToInt(ScrollFadeHeight), SettingsCornerRadius);
            image.type = Image.Type.Simple;

            // Never a raycast target: it lies over the rows, and a setting under it
            // still has to be clickable and still has to show its description.
            image.raycastTarget = false;

            // Starts invisible - TickScrollFades decides, on the first frame the list
            // has actually been laid out, whether there's anything to fade at all.
            image.color = new Color(PanelChrome.ScrollFadeColor.r, PanelChrome.ScrollFadeColor.g, PanelChrome.ScrollFadeColor.b, 0f);

            return image;
        }

        /// <summary>
        /// Fades each edge in proportion to how much content is actually hidden past
        /// it, rather than snapping it on the moment the list overflows by a hair.
        ///
        /// Run every frame instead of off <see cref="ScrollRect.onValueChanged"/>,
        /// because scrolling is not the only thing that changes the answer: switching
        /// tabs swaps the whole row set (and its height) without the scroll position
        /// moving at all, and the content's height itself only becomes readable a
        /// frame after the rows are spawned, once the ContentSizeFitter has run.
        /// </summary>
        private void TickScrollFades()
        {
            if (_scrollFadeTop == null || _settingsScroll == null)
            {
                return;
            }

            float overflow = _settingsContent.rect.height - _settingsViewport.rect.height;

            float hiddenBelow = 0f;
            float hiddenAbove = 0f;
            if (overflow > 0f)
            {
                // 1 = scrolled to the top, 0 = to the bottom.
                float position = Mathf.Clamp01(_settingsScroll.verticalNormalizedPosition);
                hiddenAbove = (1f - position) * overflow;
                hiddenBelow = position * overflow;
            }

            ApplyScrollFade(_scrollFadeTop, hiddenAbove);
            ApplyScrollFade(_scrollFadeBottom, hiddenBelow);
        }

        private static void ApplyScrollFade(Image fade, float hiddenPixels)
        {
            Color color = PanelChrome.ScrollFadeColor;
            color.a *= Mathf.Clamp01(hiddenPixels / ScrollFadeFullAt);
            fade.color = color;
        }

        /// <summary>
        /// Sits directly under the preview, filling the left column's remaining
        /// height: each config entry already carries a written-out explanation, and
        /// this is where the hovered row's shows up. Without it the menu would be a
        /// wall of uppercase keys telling you no more than the config file does.
        /// </summary>
        private void BuildDescription(RectTransform panel, float centreX, float top)
        {
            _descriptionCentreX = centreX;
            _descriptionTop = top;

            _descriptionText = CreateText(panel, "Description", DescriptionPlaceholder, 18f, PanelChrome.PlaceholderTextColor, TextAlignmentOptions.TopLeft);
            _descriptionText.enableWordWrapping = true;

            _defaultValueText = CreateText(panel, "DescriptionDefaultValue", string.Empty, 15f, PanelChrome.DefaultValueTextColor, TextAlignmentOptions.TopLeft);

            RelayoutDescriptionBlock();
        }

        /// <summary>
        /// Splits the description block's fixed <see cref="DescriptionHeight"/>
        /// budget between the description text and the default-value line below
        /// it, based on the tallest a description actually gets in the current
        /// language - not a guess. A fixed split (tuned against English) is what
        /// let a longer German/Russian/etc. description's third or fourth
        /// wrapped line run straight through "DEFAULT: ..." instead of sitting
        /// above it. Re-run by <see cref="RefreshLocalizedChrome"/> on every
        /// open, so it stays correct for whatever language the game is actually
        /// running in this session - not just whatever it was when the menu was
        /// first built.
        ///
        /// Deliberately one fixed split for the whole menu, not one per
        /// setting: the default-value line has to sit in the same place no
        /// matter which row is hovered, or it would hop up and down as the
        /// player moves the pointer down the list - see the original bug
        /// report. Measuring the single longest description (across every tab,
        /// not just the visible one - the split can't refresh mid-hover as the
        /// player switches tabs) is what makes one fixed position safe for all
        /// of them at once.
        /// </summary>
        private void RelayoutDescriptionBlock()
        {
            if (_descriptionText == null || _defaultValueText == null)
            {
                return;
            }

            float maxTextHeight = MeasureMaxDescriptionHeight();
            float textHeight = Mathf.Clamp(
                maxTextHeight, MinDescriptionTextHeight, DescriptionHeight - DefaultValueLineHeight - DescriptionDefaultValueGap);

            var rect = (RectTransform)_descriptionText.transform;
            rect.sizeDelta = new Vector2(PreviewWidth, textHeight);
            rect.anchoredPosition = new Vector2(_descriptionCentreX, _descriptionTop - textHeight * 0.5f);

            var defaultRect = (RectTransform)_defaultValueText.transform;
            defaultRect.sizeDelta = new Vector2(PreviewWidth, DefaultValueLineHeight);
            float defaultTop = _descriptionTop - textHeight - DescriptionDefaultValueGap;
            defaultRect.anchoredPosition = new Vector2(_descriptionCentreX, defaultTop - DefaultValueLineHeight * 0.5f);
        }

        /// <summary>The tallest any setting's description wraps to, at the description box's own width/font - see <see cref="RelayoutDescriptionBlock"/>.</summary>
        private float MeasureMaxDescriptionHeight()
        {
            float max = 0f;
            foreach (PreviewTab tab in _tabs)
            {
                foreach (ConfigEntryBase entry in tab.Entries)
                {
                    string tooltip = ConfigSettingNaming.Tooltip(entry);
                    if (string.IsNullOrEmpty(tooltip))
                    {
                        continue;
                    }

                    Vector2 size = _descriptionText.GetPreferredValues(tooltip, PreviewWidth, 0f);
                    if (size.y > max)
                    {
                        max = size.y;
                    }
                }
            }

            return max;
        }

        /// <summary>
        /// The footer's keys are drawn as real key badges rather than spelled out in
        /// the text, matching how `peak-checkpoint-save`'s own menus render their
        /// hints - same rounded chip, same fill/border/text colors (see
        /// <see cref="PanelChrome"/>), so the two mods' menus read as one family.
        /// </summary>
        private void BuildFooter(RectTransform panel)
        {
            var rowGo = new GameObject("Footer", typeof(RectTransform));
            var row = (RectTransform)rowGo.transform;
            row.SetParent(panel, false);
            row.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2f, FooterHeight);
            row.anchoredPosition = new Vector2(0f, -PanelHeight * 0.5f + PanelPadding * 0.5f + FooterHeight * 0.5f + FooterYNudge);

            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            // One badge, not two: both keys do the same thing, and two adjacent
            // badges read as two separate hints whose captions went missing.
            PreviewMenuLocalization.Strings strings = PreviewMenuLocalization.Current;
            string openKey = Plugin.Instance.Cfg.PreviewMenuKey.Value.ToString().ToUpperInvariant();
            AddKeyBadge(row, "ESC / " + openKey);
            _footerCloseText = AddFooterLabel(row, strings.FooterClose);

            // The leading spaces are a manual extra gap ahead of the row's normal
            // 8px spacing - plain whitespace, so it renders the same width
            // regardless of which language's string follows it.
            _footerChangesText = AddFooterLabel(row, "     " + strings.FooterChangesSaveInstantly);
        }

        private void AddKeyBadge(RectTransform parent, string key)
        {
            var go = new GameObject("KeyBadge." + key, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = PanelChrome.BadgeSprite();
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            TMP_Text label = CreateText(rect, "Key", key, 16f, PanelChrome.ChipTextColor, TextAlignmentOptions.Center, applyOutlineMaterial: false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, KeyBadgeTextNudge);
            labelRect.offsetMax = new Vector2(-10f, KeyBadgeTextNudge);

            // The badge hugs its own text: a two-character key and a longer one
            // (ESC vs. F8) shouldn't force each other to a common width.
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = label.GetPreferredValues(key).x + 24f;
            layoutElement.preferredHeight = FooterHeight - 4f;
        }

        private TMP_Text AddFooterLabel(RectTransform parent, string content)
        {
            TMP_Text text = CreateText(parent, "FooterLabel", content, 16f, PanelChrome.FooterColor, TextAlignmentOptions.MidlineLeft);

            var layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = text.GetPreferredValues(content).x;
            layoutElement.preferredHeight = FooterHeight;
            return text;
        }

        /// <summary>
        /// Re-applies every piece of this menu's own chrome that isn't already
        /// refreshed some other way, to the current language - run on every
        /// open, not just the menu's first build.
        ///
        /// <see cref="ShowTab"/> already rebuilds each row's own name/
        /// description fresh on every open, so those two never went stale. The
        /// menu's surrounding chrome (title, tab labels, footer, the preview
        /// scene's item-ping names) is built exactly once, in <see cref="BuildUi"/>,
        /// the very first time the menu is opened in a session - which used to
        /// mean whatever language happened to be active at that one moment
        /// (not necessarily the player's actual one, if this mod's Awake runs
        /// before the game has finished settling on it) stuck for the rest of
        /// the session. This is the fix: cheap enough (a handful of text
        /// assignments plus one small preview rebuild) to just always re-run
        /// on open rather than try to detect whether the language actually
        /// changed since the last one.
        /// </summary>
        private void RefreshLocalizedChrome()
        {
            PreviewMenuLocalization.Strings strings = PreviewMenuLocalization.Current;

            if (_titleText != null)
            {
                _titleText.text = TitleText;
            }

            if (_loadingText != null)
            {
                _loadingText.text = strings.Loading;
            }

            string[] tabNames =
            {
                strings.TabPlayerLabels, strings.TabPings, strings.TabItemPings,
                strings.TabCampfire, strings.TabCompass, strings.TabGeneral,
            };
            for (int i = 0; i < _tabButtons.Count && i < tabNames.Length; i++)
            {
                _tabButtons[i].Label.text = tabNames[i];
                if (i < _tabs.Count)
                {
                    _tabs[i].Name = tabNames[i];
                }
            }

            if (_footerCloseText != null)
            {
                _footerCloseText.text = strings.FooterClose;
            }

            if (_footerChangesText != null)
            {
                _footerChangesText.text = "     " + strings.FooterChangesSaveInstantly;
            }

            RelayoutDescriptionBlock();

            _scene?.RefreshLocalizedNames();
        }

        /// <summary>Tears down the previous tab's rows and spawns the selected one's. Cheap enough to do wholesale - a tab is a dozen rows at most, and only on an explicit click.</summary>
        private void ShowTab(int index)
        {
            _selectedTab = Mathf.Clamp(index, 0, _tabs.Count - 1);

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool selected = i == _selectedTab;
                _tabButtons[i].Background.color = selected ? PanelChrome.SelectedFillColor : Color.white;
                _tabButtons[i].Label.color = selected ? PanelChrome.SelectedTextColor : PanelChrome.ChipTextColor;
            }

            for (int i = _settingsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_settingsContent.GetChild(i).gameObject);
            }
            _boundSettings.Clear();
            SetDescription(null, null);

            foreach (ConfigEntryBase entry in _tabs[_selectedTab].Entries)
            {
                // A keybind has no native widget to borrow, so it gets our own
                // click-to-rebind row - and no IConfigBoundSetting, because there's
                // no Zorro Setting behind it to bind. See NativeSettingCells.
                if (entry is ConfigEntry<KeyCode> keyEntry)
                {
                    SizeRow(NativeSettingCells.CreateKeyBindRow(_settingsContent, keyEntry, SetDescription));
                    continue;
                }

                IConfigBoundSetting bound = ConfigSettingFactory.Create(entry, _handler);
                if (bound == null)
                {
                    continue;
                }

                GameObject row = NativeSettingCells.CreateRow(_settingsContent, bound, _handler, SetDescription, (RectTransform)_root.transform);
                if (row == null)
                {
                    continue;
                }

                SizeRow(row);
                _boundSettings.Add(bound);
            }
        }

        /// <summary>Rows are sized here rather than by the layout group - see SettingRowHeight.</summary>
        private static void SizeRow(GameObject row)
        {
            if (row == null)
            {
                return;
            }

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = SettingRowHeight;
            layoutElement.minHeight = SettingRowHeight;
        }

        private void SetDescription(string description, string defaultValueText)
        {
            if (_descriptionText == null)
            {
                return;
            }

            bool hasDescription = !string.IsNullOrEmpty(description);
            _descriptionText.text = hasDescription ? description : DescriptionPlaceholder;
            _descriptionText.color = hasDescription ? PanelChrome.FooterColor : PanelChrome.PlaceholderTextColor;

            _defaultValueText.text = hasDescription && !string.IsNullOrEmpty(defaultValueText)
                ? PreviewMenuLocalization.Current.DefaultValuePrefix + " " + defaultValueText
                : string.Empty;
        }

        /// <summary>Text in the game's own chunky display font, so the menu reads as part of PEAK rather than as a debug overlay.</summary>
        /// <param name="applyOutlineMaterial">
        /// peak-checkpoint-save's SavePicker footer key badges deliberately keep
        /// their text on the font's plain default material - only the chrome
        /// labels around them (title, footer captions) get the borrowed
        /// outline+shadow material. That outline material's face dilate reads as
        /// unreadable faux-bold at the key badge's small size, so this mirrors
        /// that: false for key badges, true (the default) everywhere else.
        /// </param>
        private static TMP_Text CreateText(RectTransform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment, bool applyOutlineMaterial = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;

            if (NativeAssets.Font != null)
            {
                text.font = NativeAssets.Font;
            }
            if (applyOutlineMaterial && NativeAssets.OutlineMaterial != null)
            {
                text.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            return text;
        }
    }
}
