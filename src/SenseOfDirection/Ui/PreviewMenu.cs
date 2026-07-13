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

        private const float DescriptionHeight = 96f;
        private const float FooterHeight = 34f;

        /// <summary>Each native settings row gets a deterministic height rather than whatever the prefab's own layout resolves to inside a foreign parent - and the stacked label-above-control layout decides what that is.</summary>
        private const float SettingRowHeight = NativeSettingCells.RowHeight;

        /// <summary>Vertical breathing room between rows - the reason the first pass read as "glued together" was that this was 4px.</summary>
        private const float SettingRowSpacing = 12f;

        private const float SettingsCornerRadius = 18f;

        private GameObject _root;
        private JaggedPanel _panel;
        private RectTransform _settingsContent;
        private RectTransform _tabsRow;
        private TMP_Text _descriptionText;
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

            if (_root == null)
            {
                BuildUi();
            }

            IsOpen = true;
            _root.SetActive(true);
            _window.SetRegistered(true);
            ShowTab(_selectedTab);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _root.SetActive(false);

            // Dropping out of AllActiveWindows is what re-locks the cursor and
            // hands input back to the player, via the game's own per-frame
            // recompute - see PreviewMenuWindow.
            _window.SetRegistered(false);
        }

        private void Update()
        {
            KeyCode key = Plugin.Instance.Cfg.PreviewMenuKey.Value;
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                Toggle();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

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
            // answered without leaving the tab. The keybind entries are left out
            // on purpose: PEAK has no rebind widget we can clone here, and they're
            // already editable in the config file / PEAKLib.ModConfig.
            _tabs.Add(new PreviewTab
            {
                Name = "PLAYER LABELS",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnablePlayerLabels, cfg.PlayerLabelDisplayMode, cfg.HoldShownDuration,
                    cfg.PlayerLabelMaxDistanceMeters, cfg.PlayerLabelNameFontSize, cfg.PlayerLabelDistanceFontSize,
                    cfg.ShowPlayerLabelDistance, cfg.ShowStatusBadges, cfg.UseCharacterColor,
                    cfg.ReplaceVanillaLabels, cfg.PlayerLabelPlacement,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = "PINGS",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.RemoveVisibilityCutoff, cfg.EnablePingScaling, cfg.PingScaleMultiplier,
                    cfg.EnablePingRipple, cfg.EnablePingOffScreenIndicator, cfg.ShowPingDistanceLabel,
                    cfg.EnableGhostPing, cfg.PingPlacement,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = "ITEM PINGS",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableItemPings, cfg.ItemPingDurationSeconds, cfg.EnableItemPingGrouping,
                    cfg.EnableCreaturePings, cfg.UseNativeItemPingIcons, cfg.ItemPingNameMode,
                    cfg.ShowItemPingDistance, cfg.EnableItemPingOffScreenIndicator, cfg.ItemPingPlacement,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = "CAMPFIRE",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableCampfireIndicator, cfg.ShowCampfireDistance, cfg.CampfirePlacement,
                },
            });

            _tabs.Add(new PreviewTab
            {
                Name = "COMPASS",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableCompass, cfg.CompassWidthPixels, cfg.CompassFovDegrees, cfg.CompassIconSizePixels,
                    cfg.CompassMarkerGapPixels, cfg.CompassVerticalOffsetPixels, cfg.CompassHorizontalOffsetPixels,
                    cfg.CompassElevationThresholdMeters, cfg.CompassShowNames, cfg.CompassShowDistances,
                    cfg.CompassShowDegreeNumbers, cfg.CompassLineColor, cfg.CompassLineThicknessMultiplier,
                    cfg.CompassClampIconsToEdge, cfg.CompassRequiresHoldingItem,
                },
            });

            // The overlap toggle and the font scales are the two things that cut
            // across every mechanic at once, so they share the tab where the
            // preview deliberately crowds its labels together.
            _tabs.Add(new PreviewTab
            {
                Name = "GENERAL",
                Entries = new List<ConfigEntryBase>
                {
                    cfg.EnableLabelOverlapAvoidance,
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
            scaler.matchWidthOrHeight = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            var windowGo = new GameObject("Window");
            windowGo.transform.SetParent(_root.transform, false);
            _window = windowGo.AddComponent<PreviewMenuWindow>();

            BuildDim((RectTransform)_root.transform);

            _panel = JaggedPanel.Create((RectTransform)_root.transform, "Panel", new Vector2(PanelWidth, PanelHeight));
            var panelRect = (RectTransform)_panel.transform;

            float contentTop = PanelHeight * 0.5f - PanelPadding;

            BuildTitle(panelRect, contentTop);
            _tabsRow = BuildTabsRow(panelRect, contentTop - TitleHeight);

            // Both columns start under the tabs and run down to just above the
            // footer; the left one is only as tall as its 16:9 preview needs, and
            // gives the rest of its height to the description text.
            float columnTop = contentTop - TitleHeight - TabsHeight;
            float columnBottom = -PanelHeight * 0.5f + PanelPadding + FooterHeight;

            float leftCentreX = -PanelWidth * 0.5f + PanelPadding + PreviewWidth * 0.5f;
            float rightCentreX = PanelWidth * 0.5f - PanelPadding - SettingsWidth * 0.5f;

            _scene = PreviewScene.Create(
                panelRect,
                new Vector2(PreviewWidth, PreviewHeight),
                new Vector2(leftCentreX, columnTop - PreviewHeight * 0.5f));

            BuildDescription(panelRect, leftCentreX, columnTop - PreviewHeight - 16f);
            BuildSettingsScroll(panelRect, rightCentreX, columnTop, columnBottom);
            BuildFooter(panelRect);
        }

        private void BuildDim(RectTransform parent)
        {
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            var dimRect = (RectTransform)dimGo.transform;
            dimRect.SetParent(parent, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            // A raycast target as well as a visual: it swallows clicks that miss
            // the panel, so a stray click can't reach the game behind the menu.
            var dim = dimGo.GetComponent<Image>();
            dim.color = PanelChrome.DimColor;
            dim.raycastTarget = true;
        }

        private void BuildTitle(RectTransform panel, float top)
        {
            TMP_Text title = CreateText(panel, "Title", "SENSE OF DIRECTION", 34f, PanelChrome.TitleColor, TextAlignmentOptions.Center);
            var rect = (RectTransform)title.transform;
            rect.sizeDelta = new Vector2(PanelWidth - PanelPadding * 2f, TitleHeight);
            rect.anchoredPosition = new Vector2(0f, top - TitleHeight * 0.5f);
        }

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
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

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

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _settingsContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        /// <summary>
        /// Sits directly under the preview, filling the left column's remaining
        /// height: each config entry already carries a written-out explanation, and
        /// this is where the hovered row's shows up. Without it the menu would be a
        /// wall of uppercase keys telling you no more than the config file does.
        /// </summary>
        private void BuildDescription(RectTransform panel, float centreX, float top)
        {
            _descriptionText = CreateText(panel, "Description", string.Empty, 18f, PanelChrome.FooterColor, TextAlignmentOptions.TopLeft);
            var rect = (RectTransform)_descriptionText.transform;
            rect.sizeDelta = new Vector2(PreviewWidth, DescriptionHeight);
            rect.anchoredPosition = new Vector2(centreX, top - DescriptionHeight * 0.5f);
            _descriptionText.enableWordWrapping = true;
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
            row.anchoredPosition = new Vector2(0f, -PanelHeight * 0.5f + PanelPadding * 0.5f + FooterHeight * 0.5f);

            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            AddKeyBadge(row, "ESC");
            AddKeyBadge(row, Plugin.Instance.Cfg.PreviewMenuKey.Value.ToString().ToUpperInvariant());
            AddFooterLabel(row, "CLOSE");
            AddFooterLabel(row, "     HOVER A SETTING FOR ITS DESCRIPTION     CHANGES SAVE INSTANTLY");
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

            TMP_Text label = CreateText(rect, "Key", key, 16f, PanelChrome.ChipTextColor, TextAlignmentOptions.Center);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            // The badge hugs its own text: a two-character key and a longer one
            // (ESC vs. F8) shouldn't force each other to a common width.
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = label.GetPreferredValues(key).x + 24f;
            layoutElement.preferredHeight = FooterHeight - 4f;
        }

        private void AddFooterLabel(RectTransform parent, string content)
        {
            TMP_Text text = CreateText(parent, "FooterLabel", content, 16f, PanelChrome.FooterColor, TextAlignmentOptions.MidlineLeft);

            var layoutElement = text.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = text.GetPreferredValues(content).x;
            layoutElement.preferredHeight = FooterHeight;
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
            SetDescription(null);

            foreach (ConfigEntryBase entry in _tabs[_selectedTab].Entries)
            {
                IConfigBoundSetting bound = ConfigSettingFactory.Create(entry, _handler);
                if (bound == null)
                {
                    // A type PEAK has no widget for (only KeyCode today). Skipped
                    // rather than rendered as a dead row.
                    continue;
                }

                GameObject row = NativeSettingCells.CreateRow(_settingsContent, bound, _handler, SetDescription, (RectTransform)_root.transform);
                if (row == null)
                {
                    continue;
                }

                LayoutElement layoutElement = row.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = row.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = SettingRowHeight;
                layoutElement.minHeight = SettingRowHeight;

                _boundSettings.Add(bound);
            }
        }

        private void SetDescription(string description)
        {
            if (_descriptionText != null)
            {
                _descriptionText.text = description ?? string.Empty;
            }
        }

        /// <summary>Text in the game's own chunky display font, so the menu reads as part of PEAK rather than as a debug overlay.</summary>
        private static TMP_Text CreateText(RectTransform parent, string name, string content, float fontSize, Color color, TextAlignmentOptions alignment)
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
            if (NativeAssets.OutlineMaterial != null)
            {
                text.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            return text;
        }
    }
}
