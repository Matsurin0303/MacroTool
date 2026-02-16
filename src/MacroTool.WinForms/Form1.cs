using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Application.Services;
using MacroTool.WinForms.Core;
using MacroTool.WinForms.Settings;
using System.ComponentModel;
using MacroTool.Domain.Macros;
using MacroTool.WinForms.Editors;
using Dialogs = MacroTool.WinForms.Dialogs;

namespace MacroTool.WinForms;

public partial class Form1 : Form
{
    private const int HOTKEY_ID_STOP = 1; // Esc停止用

    // Fileタブ（Backstage）用
    private string? _currentMacroPath;

    // Core
    private readonly MacroAppService _app;
    private readonly IPlaybackOptionsAccessor _playbackOptionsAccessor;

    private readonly BindingList<ActionRow> _rows = new();
    private readonly RecentFilesStore _recentFiles = new(appName: "MacroTool", maxItems: 10);

    // Settings
    private readonly SettingsStore _settingsStore = new(SettingsStore.DefaultPath());
    private AppSettings _settings = new();

    // Status
    private ToolStripStatusLabel _lblState = null!;
    private ToolStripStatusLabel _lblElapsed = null!;
    private ToolStripStatusLabel _lblSchedule = null!;
    private readonly System.Windows.Forms.Timer _statusTimer = new();

    // Scheduler (v1.0: アプリ稼働中のみ)
    private System.Threading.Timer? _scheduleTimer;
    private DateTime? _scheduledAt;

    // v1.0 追加メニュー
    private ToolStripMenuItem? _miExportCsv;
    private ToolStripMenuItem? _miScheduleMacro;
    private ToolStripMenuItem? _miPlayUntilSelected;
    private ToolStripMenuItem? _miPlayFromSelected;
    private ToolStripMenuItem? _miPlaySelectedOnly;

    // Ribbon: Playback tab UI（docs/images に寄せる／一部は UI のみ）
    private ToolStrip? _tsPlayback;
    private ToolStripSplitButton? _tsbPlayPb;
    private ToolStripSplitButton? _tsbRecordPb;
    private ToolStripButton? _tsbStopPb;
    private ToolStripMenuItem? _miPlayUntilSelectedPb;
    private ToolStripMenuItem? _miPlayFromSelectedPb;
    private ToolStripMenuItem? _miPlaySelectedOnlyPb;

    // Ribbon: View tab UI
    private ToolStrip? _tsView;
    private ToolStripButton? _tsbShowLineNumbers;
    private ToolStripButton? _tsbVariableExplorer;
    private ToolStripButton? _tsbShowActionOverlays;

    // Dialog
    private readonly OpenFileDialog _openMacroDialog = new()
    {
        Filter = "Macro file (*.mcr)|*.mcr|JSON file (*.json)|*.json|All files (*.*)|*.*",
        Title = "マクロを開く"
    };

    private readonly SaveFileDialog _saveMacroDialog = new()
    {
        Filter = "Macro file (*.mcr)|*.mcr|JSON file (*.json)|*.json|All files (*.*)|*.*",
        Title = "マクロを保存",
        DefaultExt = "mcr",
        AddExtension = true,
        FileName = "macro.mcr"
    };

    private bool _isDirty = false;
    private bool _suppressDirty = false;

    private bool _suppressGridCommit;

    public Form1(MacroAppService app, IPlaybackOptionsAccessor playbackOptionsAccessor)
    {
        InitializeComponent();

        _app = app;
        _playbackOptionsAccessor = playbackOptionsAccessor;

        // PropertyGrid: GoToTarget を「Start/Next/End/Label一覧」で選べるようにする
        GoToTargetEditor.Register();
        GoToTargetEditor.LabelsProvider = () =>
            _app.CurrentMacro.Steps
                .Select(s => (s.Label ?? string.Empty).Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        // Settings 読み込み（起動時に即反映）
        _settings = _settingsStore.Load();
        ApplyUiSettings();
        ApplyPlaybackSettingsToAccessor();

        ApplyToolStripIcons();

        // 画像UI（docs/images）に寄せる：上段メニューをタブとして使い、下段はリボン（TabControlのヘッダを隠す）
        ConfigureRibbonTabs();

        // Recent Files
        recentFilesToolStripMenuItem.DropDownOpening += (_, __) => RebuildRecentFilesMenu();

        // 列の編集可否（Designerで列が存在している前提）
        colLabel.ReadOnly = false;
        colComment.ReadOnly = false;

        colNo.ReadOnly = true;
        colIcon.ReadOnly = true;
        colAction.ReadOnly = true;
        colValue.ReadOnly = true;

        // Notification
        _app.UserNotification += OnUserNotification;
        _app.StepExecuting += OnStepExecuting;

        // StatusStripに「状態」「経過」を追加（Designerを汚さない）
        _lblState = new ToolStripStatusLabel
        {
            Name = "lblState",
            Text = "状態: 停止",
            AutoSize = true
        };
        statusStrip1.Items.Insert(0, _lblState);

        _lblElapsed = new ToolStripStatusLabel
        {
            Name = "lblElapsed",
            Text = "経過 00:00:00",
            AutoSize = true
        };
        statusStrip1.Items.Add(_lblElapsed);

        _lblSchedule = new ToolStripStatusLabel
        {
            Name = "lblSchedule",
            Text = "スケジュール: なし",
            AutoSize = true
        };
        statusStrip1.Items.Add(_lblSchedule);

        // Coreイベント
        _app.StateChanged += (_, __) => BeginInvoke(new Action(UpdateUi));
        _app.MacroChanged += (_, __) => BeginInvoke(new Action(() =>
        {
            if (!_suppressDirty) _isDirty = true;
            RefreshGridFromDomainPreserveSelection();
            UpdateUi();
        }));

        // Grid
        gridActions.AutoGenerateColumns = false;
        gridActions.DataSource = _rows;
        gridActions.CellFormatting += GridActions_CellFormatting;
        gridActions.RowTemplate.Height = 22;

        gridActions.CellEndEdit += GridActions_CellEndEdit;
        gridActions.KeyDown += GridActions_KeyDown;
        gridActions.CellBeginEdit += GridActions_CellBeginEdit;
        gridActions.CellMouseDown += GridActions_CellMouseDown;

        ConfigureGridColumns();

        // 右クリックメニュー（Designerで cmsActions / mnuCopy 等がある前提）
        mnuCopy.Click += (_, __) => CopySelectedRows();
        mnuCut.Click += (_, __) => CutSelectedRows();
        mnuPaste.Click += (_, __) => PasteRows();
        mnuDelete.Click += (_, __) => DeleteSelectedRows();

        cmsActions.Opening += (_, __) =>
        {
            bool stopped = _app.State == MacroTool.Application.AppState.Stopped;
            mnuCopy.Enabled = stopped && gridActions.SelectedRows.Count > 0;
            mnuCut.Enabled = stopped && gridActions.SelectedRows.Count > 0;
            mnuPaste.Enabled = stopped && Clipboard.ContainsText();
            mnuDelete.Enabled = stopped && gridActions.SelectedRows.Count > 0;
        };

        // Settings（※「未実装」Clickハンドラは絶対に残さないこと）
        // もしDesigner側で Click が紐付いている場合は、Designerの Click も削除/差し替えしてください。
        settingsToolStripMenuItem.Click += (_, __) => OpenSettings();

        // ToolStrip（Record/Stop/Play/Delete）
        HookToolStripButtons();

        // v1.0: 追加のメニュー（CSV出力/スケジュール/各アクション追加/範囲再生）
        InitializeFileMenuExtras();
        InitializeActionToolStrips();

        // docs/images: Playback/View タブのリボンを配置（未実装は UI のみ）
        InitializePlaybackTabUi();
        InitializeViewTabUi();

        // Status更新タイマー
        _statusTimer.Interval = 250;
        _statusTimer.Tick += (_, __) => UpdateStatusBar();
        _statusTimer.Start();

        UpdateUi();
        RefreshGridFromDomain();
    }

    private void ConfigureRibbonTabs()
    {
        // TabControlのタブ見出しを非表示（MenuStrip側を“タブ見出し”として使う）
        tabRibbon.Appearance = TabAppearance.FlatButtons;
        tabRibbon.ItemSize = new Size(0, 1);
        tabRibbon.SizeMode = TabSizeMode.Fixed;

        // “未実装”はクリックさせない（仕様: 将来実装予定は処理しない）
        registerLicenseKeyToolStripMenuItem.Enabled = false;

        // MenuStrip のタブ切替（チェックで疑似的に選択表示）
        recordAndEditToolStripMenuItem.CheckOnClick = true;
        playbackToolStripMenuItem.CheckOnClick = true;
        viewToolStripMenuItem.CheckOnClick = true;
        helpToolStripMenuItem.CheckOnClick = true;

        recordAndEditToolStripMenuItem.Click += (_, __) => SelectRibbonTab(tabPage2, recordAndEditToolStripMenuItem);
        playbackToolStripMenuItem.Click += (_, __) => SelectRibbonTab(tabPage3, playbackToolStripMenuItem);
        viewToolStripMenuItem.Click += (_, __) => SelectRibbonTab(tabPage4, viewToolStripMenuItem);
        helpToolStripMenuItem.Click += (_, __) => SelectRibbonTab(tabPage5, helpToolStripMenuItem);

        // 初期表示
        SelectRibbonTab(tabPage2, recordAndEditToolStripMenuItem);

        // PhraseExpress 連携は将来機能（UIのみ）
        tsbSendToPhraseExpress.Enabled = false;
    }

    private void SelectRibbonTab(TabPage page, ToolStripMenuItem selected)
    {
        tabRibbon.SelectedTab = page;

        // File は“タブ”ではないので除外
        var tabs = new[]
        {
            recordAndEditToolStripMenuItem,
            playbackToolStripMenuItem,
            viewToolStripMenuItem,
            helpToolStripMenuItem
        };
        foreach (var mi in tabs)
            mi.Checked = mi == selected;
    }

    // ===== Settings =====
    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_settingsStore);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        _settings = _settingsStore.Load();

        // UI設定を即時反映
        ApplyUiSettings();

        // Playback設定を即時反映（ただし再生中はSendInputPlayer側がスナップショットなので次回再生から）
        ApplyPlaybackSettingsToAccessor();

        MessageBox.Show(this, "設定を保存しました。", "Info",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyUiSettings()
    {
        // 現状は DeleteSelectedRows() 内で _settings.Ui.ConfirmDelete を参照
    }

    private void ApplyPlaybackSettingsToAccessor()
    {
        _playbackOptionsAccessor.Update(new PlaybackOptions
        {
            EnableStabilizeWait = _settings.Playback.EnableStabilizeWait,
            CursorSettleDelayMs = _settings.Playback.CursorSettleDelayMs,
            ClickHoldDelayMs = _settings.Playback.ClickHoldDelayMs
        });
    }

    // ===== File/Open/Save =====
    private void LoadMacroFromFile()
    {
        if (!ConfirmSaveIfDirty("読み込み"))
            return;

        if (_openMacroDialog.ShowDialog(this) != DialogResult.OK)
            return;

        LoadMacroFromPath(_openMacroDialog.FileName);
    }

    private void UpdateCurrentFileTitle()
    {
        Text = string.IsNullOrWhiteSpace(_currentMacroPath)
            ? "MacroTool"
            : $"MacroTool - {System.IO.Path.GetFileName(_currentMacroPath)}";
    }

    private void SaveMacro() => TrySaveWithPrompt(false);
    private void SaveMacroAs() => TrySaveWithPrompt(true);

    private bool TrySaveWithPrompt(bool forceSaveAs)
    {
        try
        {
            if (forceSaveAs || string.IsNullOrWhiteSpace(_currentMacroPath))
            {
                if (_saveMacroDialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                _currentMacroPath = _saveMacroDialog.FileName;
            }

            _app.Save(_currentMacroPath!);
            _isDirty = false;
            _recentFiles.Add(_currentMacroPath!);

            UpdateUi();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"保存に失敗しました。\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool ConfirmSaveIfDirty(string actionName)
    {
        if (!_isDirty) return true;

        var result = MessageBox.Show(
            this,
            $"未保存の変更があります。\n保存して {actionName} しますか？",
            "確認",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => TrySaveWithPrompt(false),
            DialogResult.No => true,
            _ => false
        };
    }

    // ===== ToolStrip (Record/Stop/Play/Delete) =====
    private void HookToolStripButtons()
    {
        static void Bind(ToolStripItem item, Action handler)
        {
            if (item is ToolStripSplitButton sb)
                sb.ButtonClick += (_, __) => handler();
            else
                item.Click += (_, __) => handler();
        }

        Bind(tsbDelete, DeleteSelectedRows);
        Bind(tsbRecord, StartRecording);
        Bind(tsbStop, () => _app.StopAll());
        Bind(tsbPlay, () =>
        {
            if (_app.State != MacroTool.Application.AppState.Stopped) return;
            if (_app.ActionCount == 0) return;
            _app.Play();
        });
    }

    // ===== v1.0: Fileメニュー拡張（CSV出力/スケジュール） =====
    private void InitializeFileMenuExtras()
    {
        if (_miExportCsv != null || _miScheduleMacro != null)
        {
            // Already initialized
            return;
        }

        // v1.0 では未対応だが UI 画像には存在するため、表示だけ（無効化）
        var miTriggerMacroByHotkey = new ToolStripMenuItem("Trigger macro by hotkey")
        {
            Name = "miTriggerMacroByHotkey",
            Enabled = false
        };

        // v1.0 仕様（MacroTool_MacroSpecification_v1.0.md 3.1 File）に合わせる
        _miExportCsv = new ToolStripMenuItem("Export to CSV")
        {
            Name = "miExportCsv"
        };
        _miExportCsv.Click += (_, __) => ExportToCsv();

        _miScheduleMacro = new ToolStripMenuItem("Schedule macro")
        {
            Name = "miScheduleMacro"
        };
        _miScheduleMacro.Click += (_, __) => ScheduleMacro();

        var sepBetweenCsvAndSchedule = new ToolStripSeparator { Name = "miSepCsvSchedule" };

        // 既存の区切り線（SaveAs と Settings の間）より前に差し込む
        var insertBefore = fileToolStripMenuItem.DropDownItems.IndexOf(toolStripMenuItem2);
        if (insertBefore < 0) insertBefore = fileToolStripMenuItem.DropDownItems.Count;
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore, miTriggerMacroByHotkey);
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore + 1, _miExportCsv);
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore + 2, sepBetweenCsvAndSchedule);
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore + 3, _miScheduleMacro);
    }

    // ===== v1.0: ToolStrip 拡張（各アクション追加 / 範囲再生 / 編集） =====
    private void InitializeActionToolStrips()
    {
        InitializePlayDropDown();
        InitializeActionDropDowns();

        // Edit
        tsbEdit.Click += (_, __) => EditSelectedAction();

        // Search/Replace (簡易: 検索のみ)
        tsbSearchReplace.Click += (_, __) => ShowSearchDialog();
    }

    private void InitializePlayDropDown()
    {
        tsbPlay.DropDownItems.Clear();

        _miPlayUntilSelected = new ToolStripMenuItem("Play until selected")
        {
            Name = "miPlayUntilSelected"
        };
        _miPlayUntilSelected.Click += (_, __) => PlayUntilSelected();

        _miPlayFromSelected = new ToolStripMenuItem("Play from selected")
        {
            Name = "miPlayFromSelected"
        };
        _miPlayFromSelected.Click += (_, __) => PlayFromSelected();

        _miPlaySelectedOnly = new ToolStripMenuItem("Play selected")
        {
            Name = "miPlaySelected"
        };
        _miPlaySelectedOnly.Click += (_, __) => PlaySelectedOnly();

        tsbPlay.DropDownItems.Add(_miPlayUntilSelected);
        tsbPlay.DropDownItems.Add(_miPlayFromSelected);
        tsbPlay.DropDownItems.Add(_miPlaySelectedOnly);
    }

    private void InitializeActionDropDowns()
    {
        // docs/images に合わせた表示（将来実装予定は無効化して表示のみ）

        // Mouse
        tsdMouse.DropDownItems.Clear();
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Click [C]", null, (_, __) => AddMouseClick()));
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Move [M]", null, (_, __) => AddMouseMove()));
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Wheel [V]", null, (_, __) => AddMouseWheel()));

        // Text/Key
        tsdTextKey.DropDownItems.Clear();
        tsdTextKey.DropDownItems.Add(new ToolStripMenuItem("Key press [K]", null, (_, __) => AddKeyPress()));

        // Text は将来実装予定（UIのみ）
        tsdTextKey.DropDownItems.Add(new ToolStripMenuItem("Text [T]") { Enabled = false });

        // Hotkey は v1.0 対応（複数キー入力の支援）
        tsdTextKey.DropDownItems.Add(new ToolStripSeparator());
        tsdTextKey.DropDownItems.Add(new ToolStripMenuItem("Hotkey", null, (_, __) => AddHotkey()));

        // Wait
        tsdWait.DropDownItems.Clear();
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait [W]", null, (_, __) => AddWaitTime()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for pixel color [P]", null, (_, __) => AddWaitForPixelColor()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for screen changes", null, (_, __) => AddWaitForScreenChange()));

        // 将来実装予定（UIのみ）
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for hotkey press") { Enabled = false });

        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for text input", null, (_, __) => AddWaitForTextInput()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for file") { Enabled = false });

        // Image/OCR
        tsdImageOcr.DropDownItems.Clear();
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Find image [I]", null, (_, __) => AddFindImage()));
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Find text (OCR)", null, (_, __) => AddFindTextOcr()));
        tsdImageOcr.DropDownItems.Add(new ToolStripSeparator());

        // Capture 系は UI 画像には存在するが、このブランチでは未実装のため無効化
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Capture text (OCR)") { Enabled = false });
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Capture image (Screenshot)") { Enabled = false });

        // Misc / Control Flow
        tsdMisc.DropDownItems.Clear();
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Repeat [L]", null, (_, __) => AddRepeat()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("GoTo [G]", null, (_, __) => AddGoTo()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Condition", null, (_, __) => AddIf()));
        tsdMisc.DropDownItems.Add(new ToolStripSeparator());
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Embed macro file", null, (_, __) => AddEmbedMacroFile()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Execute program [E]", null, (_, __) => AddExecuteProgram()));

        // 以下は将来実装予定（UIのみ）
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Window focus [F]") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripSeparator());
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Show notification [N]") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Show message box") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Beep [B]") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripSeparator());
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Set variable") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Set variable from data list") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Save variable") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripSeparator());
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Calculate expression") { Enabled = false });
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Extract from Web Site") { Enabled = false });
    }


    // ===== docs/images: Playback タブ UI（リボン配置：一部は UI のみ） =====
    private void InitializePlaybackTabUi()
    {
        if (_tsPlayback != null) return;

        // Playback タブはリボンのみ（中身は将来拡張）
        tabPage3.Controls.Clear();

        var ts = new ToolStrip
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(32, 32),
            Padding = new Padding(2, 2, 2, 2)
        };

        _tsPlayback = ts;

        _tsbPlayPb = new ToolStripSplitButton
        {
            AutoSize = false,
            Size = new Size(80, 70),
            Text = "Play",
            Image = Properties.Resources.Play,
            TextImageRelation = TextImageRelation.ImageAboveText
        };

        _tsbRecordPb = new ToolStripSplitButton
        {
            AutoSize = false,
            Size = new Size(80, 70),
            Text = "Record",
            Image = Properties.Resources.Record,
            TextImageRelation = TextImageRelation.ImageAboveText
        };

        _tsbStopPb = new ToolStripButton
        {
            AutoSize = false,
            Size = new Size(80, 70),
            Text = "Stop",
            Image = Properties.Resources.Stop,
            TextImageRelation = TextImageRelation.ImageAboveText
        };

        BuildPlayDropDown(_tsbPlayPb, isPlaybackTab: true);

        // ButtonClick は SplitButton の本体クリックに合わせる
        _tsbPlayPb.ButtonClick += (_, __) =>
        {
            if (_app.State != MacroTool.Application.AppState.Stopped) return;
            if (_app.ActionCount == 0) return;
            _app.Play();
        };
        _tsbRecordPb.ButtonClick += (_, __) => StartRecording();
        _tsbStopPb.Click += (_, __) => _app.StopAll();

        ts.Items.Add(_tsbPlayPb);
        ts.Items.Add(_tsbRecordPb);
        ts.Items.Add(_tsbStopPb);
        ts.Items.Add(new ToolStripSeparator());

        // === Playback Properties（UIのみ） ===
        var propsPanel = BuildPlaybackPropertiesPanel();
        ts.Items.Add(CreateHost(propsPanel));

        ts.Items.Add(new ToolStripSeparator());

        // === After playback（UIのみ） ===
        var afterPanel = BuildAfterPlaybackPanel();
        ts.Items.Add(CreateHost(afterPanel));

        ts.Items.Add(new ToolStripSeparator());

        // === Playback filter（UIのみ） ===
        var filterPanel = BuildPlaybackFilterPanel();
        ts.Items.Add(CreateHost(filterPanel));

        tabPage3.Controls.Add(ts);

        // local helpers
        static ToolStripControlHost CreateHost(Control c)
        {
            var host = new ToolStripControlHost(c)
            {
                AutoSize = false,
                Size = new Size(c.Width, c.Height),
                Margin = new Padding(6, 0, 6, 0)
            };
            return host;
        }
    }

    private void BuildPlayDropDown(ToolStripSplitButton playButton, bool isPlaybackTab)
    {
        playButton.DropDownItems.Clear();

        // Record&Edit の既存メニューとは別インスタンス（同じ ToolStripItem を複数箇所に付けられないため）
        var miUntil = new ToolStripMenuItem("Play until selected") { Name = isPlaybackTab ? "miPlayUntilSelectedPb" : "miPlayUntilSelected" };
        miUntil.Click += (_, __) => PlayUntilSelected();

        var miFrom = new ToolStripMenuItem("Play from selected") { Name = isPlaybackTab ? "miPlayFromSelectedPb" : "miPlayFromSelected" };
        miFrom.Click += (_, __) => PlayFromSelected();

        var miSelected = new ToolStripMenuItem("Play selected") { Name = isPlaybackTab ? "miPlaySelectedPb" : "miPlaySelected" };
        miSelected.Click += (_, __) => PlaySelectedOnly();

        playButton.DropDownItems.Add(miUntil);
        playButton.DropDownItems.Add(miFrom);
        playButton.DropDownItems.Add(miSelected);

        if (isPlaybackTab)
        {
            _miPlayUntilSelectedPb = miUntil;
            _miPlayFromSelectedPb = miFrom;
            _miPlaySelectedOnlyPb = miSelected;
        }
    }

    private static Panel BuildPlaybackPropertiesPanel()
    {
        var panel = new Panel
        {
            Width = 320,
            Height = 70,
            Padding = new Padding(6, 4, 6, 2)
        };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tlp.Controls.Add(new Label { Text = "Playback speed:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        tlp.Controls.Add(new TextBox { Text = "100", Width = 60, Anchor = AnchorStyles.Left }, 1, 0);

        tlp.Controls.Add(new Label { Text = "Mouse path:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        var cmbPath = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Anchor = AnchorStyles.Left };
        cmbPath.Items.AddRange(new object[] { "As recorded" });
        cmbPath.SelectedIndex = 0;
        tlp.Controls.Add(cmbPath, 1, 1);

        tlp.Controls.Add(new Label { Text = "Repeat:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        tlp.Controls.Add(new TextBox { Text = "1", Width = 60, Anchor = AnchorStyles.Left }, 1, 2);

        var lbl = new Label
        {
            Text = "Playback Properties",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        };
        tlp.Controls.Add(lbl, 0, 3);
        tlp.SetColumnSpan(lbl, 2);

        panel.Controls.Add(tlp);

        // v1.0: まだ反映先がないため UI のみ
        panel.Enabled = false;
        return panel;
    }

    private static Panel BuildAfterPlaybackPanel()
    {
        var panel = new Panel
        {
            Width = 220,
            Height = 70,
            Padding = new Padding(6, 4, 6, 2)
        };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tlp.Controls.Add(new Label { Text = "After playback:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Anchor = AnchorStyles.Left };
        cmb.Items.AddRange(new object[] { "No action" });
        cmb.SelectedIndex = 0;
        tlp.Controls.Add(cmb, 1, 0);

        var lbl = new Label
        {
            Text = "After playback",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        };
        tlp.Controls.Add(lbl, 0, 2);
        tlp.SetColumnSpan(lbl, 2);

        panel.Controls.Add(tlp);
        panel.Enabled = false;
        return panel;
    }

    private static Panel BuildPlaybackFilterPanel()
    {
        var panel = new Panel
        {
            Width = 260,
            Height = 70,
            Padding = new Padding(6, 4, 6, 2)
        };

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        flow.Controls.Add(new CheckBox { Text = "Mouse moves", Checked = true, AutoSize = true });
        flow.Controls.Add(new CheckBox { Text = "Mouse clicks", Checked = true, AutoSize = true });
        flow.Controls.Add(new CheckBox { Text = "Key presses", Checked = true, AutoSize = true });
        flow.Controls.Add(new CheckBox { Text = "Wait times", Checked = true, AutoSize = true });
        flow.Controls.Add(new CheckBox { Text = "Focus changes", Checked = false, AutoSize = true });
        flow.Controls.Add(new CheckBox { Text = "Show notifications", Checked = false, AutoSize = true });

        outer.Controls.Add(flow, 0, 0);
        outer.Controls.Add(new Label
        {
            Text = "Playback filter",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText
        }, 0, 1);

        panel.Controls.Add(outer);
        panel.Enabled = false;
        return panel;
    }

    // ===== docs/images: View タブ UI（リボン配置） =====
    private void InitializeViewTabUi()
    {
        if (_tsView != null) return;

        tabPage4.Controls.Clear();

        var ts = new ToolStrip
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(32, 32),
            Padding = new Padding(2, 2, 2, 2)
        };

        _tsView = ts;

        _tsbShowLineNumbers = new ToolStripButton
        {
            AutoSize = false,
            Size = new Size(120, 70),
            Text = "Show line numbers",
            TextImageRelation = TextImageRelation.ImageAboveText,
            CheckOnClick = true,
            Checked = colNo.Visible
        };
        _tsbShowLineNumbers.Click += (_, __) =>
        {
            colNo.Visible = _tsbShowLineNumbers.Checked;
        };

        _tsbVariableExplorer = new ToolStripButton
        {
            AutoSize = false,
            Size = new Size(120, 70),
            Text = "Variable explorer",
            TextImageRelation = TextImageRelation.ImageAboveText,
            Enabled = false
        };

        _tsbShowActionOverlays = new ToolStripButton
        {
            AutoSize = false,
            Size = new Size(140, 70),
            Text = "Show action overlays",
            TextImageRelation = TextImageRelation.ImageAboveText,
            Enabled = false
        };

        ts.Items.Add(_tsbShowLineNumbers);
        ts.Items.Add(_tsbVariableExplorer);
        ts.Items.Add(_tsbShowActionOverlays);

        tabPage4.Controls.Add(ts);
    }

    private int[] GetSelectedStepIndices()
        => gridActions.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.Index)
            .OrderBy(i => i)
            .ToArray();

    private int GetInsertIndexForNewAction()
    {
        var selected = GetSelectedStepIndices();
        if (selected.Length == 0) return _app.CurrentMacro.Count;
        return Math.Min(_app.CurrentMacro.Count, selected.Max() + 1);
    }

    private void SelectRowSafely(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= gridActions.Rows.Count) return;
        gridActions.ClearSelection();
        gridActions.Rows[rowIndex].Selected = true;
        gridActions.CurrentCell = gridActions.Rows[rowIndex].Cells[0];
    }

    private void InsertAction(MacroAction action)
    {
        var idx = GetInsertIndexForNewAction();
        _app.InsertStep(idx, new MacroStep(action));
        BeginInvoke(() => SelectRowSafely(idx));
    }

    private void InsertActions(IEnumerable<MacroAction> actions)
    {
        var idx = GetInsertIndexForNewAction();
        var steps = actions.Select(a => new MacroStep(a)).ToList();
        _app.InsertSteps(idx, steps);
        BeginInvoke(() => SelectRowSafely(idx));
    }

    private void PlayUntilSelected()
    {
        var selected = GetSelectedStepIndices();
        if (selected.Length == 0) return;
        _app.PlayUntil(selected.Max());
    }

    private void PlayFromSelected()
    {
        var selected = GetSelectedStepIndices();
        if (selected.Length == 0) return;
        _app.PlayFrom(selected.Min());
    }

    private void PlaySelectedOnly()
    {
        var selected = GetSelectedStepIndices();
        if (selected.Length == 0) return;
        _app.PlaySelected(selected);
    }

    private void AddActionWithEditor(MacroAction action)
    {
        var edited = Dialogs.ActionEditorForm.EditAction(this, action);
        if (edited is null) return;
        InsertAction(edited);
    }

    private void AddMouseClick()
    {
        var p = Cursor.Position;
        var initial = new MouseClickAction
        {
            X = p.X,
            Y = p.Y,
            Relative = false,
            Button = MouseButton.Left,
            Action = MouseClickType.Click,
            // 互換: ActionEditor が表示する場合に備えて同期
            ClickType = MouseClickType.Click
        };

        var action = Dialogs.MouseClickDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddMouseMove()
    {
        var p = Cursor.Position;
        var initial = new MouseMoveAction
        {
            Relative = false,
            StartX = p.X,
            StartY = p.Y,
            EndX = p.X,
            EndY = p.Y,
            DurationMs = 50
        };

        var action = Dialogs.MouseMoveDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddMouseWheel()
    {
        var initial = new MouseWheelAction
        {
            Orientation = WheelOrientation.Vertical,
            Value = 0
        };

        var action = Dialogs.MouseWheelDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddKeyPress()
    {
        var action = Dialogs.KeyPressDialog.Show(this, initial: null);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddHotkey()
    {
        var actions = Dialogs.HotkeyDialog.Show(this);
        if (actions is null || actions.Count == 0) return;
        InsertActions(actions);
    }

    private void AddWaitTime()
    {
        var initial = new WaitTimeAction { Milliseconds = 1000 };
        var action = Dialogs.WaitTimeDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddWaitForPixelColor()
    {
        var p = Cursor.Position;
        var color = GetPixelColorAt(p);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        var initial = new WaitForPixelColorAction
        {
            X = p.X,
            Y = p.Y,
            ColorHex = hex,
            TolerancePercent = 10,
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 120_000,
            FalseGoTo = GoToTarget.End()
        };

        var action = Dialogs.WaitForPixelColorDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddWaitForScreenChange()
    {
        var initial = new WaitForScreenChangeAction
        {
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 120_000,
            FalseGoTo = GoToTarget.End()
        };

        var action = Dialogs.WaitForScreenChangeDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddWaitForTextInput()
    {
        var initial = new WaitForTextInputAction
        {
            TextToWaitFor = "OK",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 120_000,
            FalseGoTo = GoToTarget.End()
        };

        var action = Dialogs.WaitForTextInputDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddFindImage()
    {
        var initial = new FindImageAction
        {
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            ColorTolerancePercent = 0,
            Template = new ImageTemplate { Kind = ImageTemplateKind.FilePath, FilePath = string.Empty },
            MouseActionEnabled = true,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = MacroTool.Domain.Macros.MousePosition.Center,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 120_000,
            FalseGoTo = GoToTarget.End()
        };

        var action = Dialogs.FindImageDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddFindImageFromFile()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "テンプレート画像を選択",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        var action = new FindImageAction
        {
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            ColorTolerancePercent = 10,
            Template = new ImageTemplate { Kind = ImageTemplateKind.FilePath, FilePath = ofd.FileName },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = MacroTool.Domain.Macros.MousePosition.Center,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 5000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddFindImageFromCapture()
    {
        using var cap = new Dialogs.ScreenRegionCaptureForm();
        if (cap.ShowDialog(this) != DialogResult.OK) return;

        var bytes = cap.CapturedPngBytes;
        if (bytes is null || bytes.Length == 0) return;

        var action = new FindImageAction
        {
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            ColorTolerancePercent = 10,
            Template = new ImageTemplate { Kind = ImageTemplateKind.EmbeddedPng, PngBytes = bytes },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = MacroTool.Domain.Macros.MousePosition.Center,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 5000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddFindTextOcr()
    {
        var initial = new FindTextOcrAction
        {
            TextToSearchFor = "",
            Language = OcrLanguage.English,
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            MouseActionEnabled = true,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = MacroTool.Domain.Macros.MousePosition.Center,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 120_000,
            FalseGoTo = GoToTarget.End()
        };

        var action = Dialogs.FindTextOcrDialog.Show(this, initial);
        if (action is null) return;
        InsertAction(action);
    }

    private void AddRepeat()
    {
        var action = new RepeatAction
        {
            StartLabel = "",
            Condition = new RepeatCondition { Kind = RepeatConditionKind.Repetitions, Repetitions = 2 },
            AfterRepeatGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddGoTo()
    {
        var action = new GoToAction { Target = GoToTarget.Next() };
        AddActionWithEditor(action);
    }

    private void AddIf()
    {
        var action = new IfAction
        {
            VariableName = "Var1",
            Condition = IfConditionKind.ValueDefined,
            CompareValue = "",
            TrueGoTo = GoToTarget.Next(),
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddEmbedMacroFile()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "埋め込みマクロファイルを選択",
            Filter = "Macro files|*.json;*.macro;*.mcr|All files|*.*"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        var action = new EmbedMacroFileAction { MacroFilePath = ofd.FileName };
        AddActionWithEditor(action);
    }

    private void AddExecuteProgram()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "実行するプログラムを選択",
            Filter = "Executables|*.exe;*.bat;*.cmd|All files|*.*"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        var action = new ExecuteProgramAction { ProgramPath = ofd.FileName };
        AddActionWithEditor(action);
    }

    private void EditSelectedAction()
    {
        var selected = GetSelectedStepIndices();
        if (selected.Length == 0) return;
        var idx = selected[0];
        if (idx < 0 || idx >= _app.CurrentMacro.Steps.Count) return;

        var step = _app.CurrentMacro.Steps[idx];
        MacroAction? edited = step.Action switch
        {
            MouseClickAction mc => Dialogs.MouseClickDialog.Show(this, mc),
            MouseMoveAction mm => Dialogs.MouseMoveDialog.Show(this, mm),
            MouseWheelAction mw => Dialogs.MouseWheelDialog.Show(this, mw),
            KeyPressAction kp => Dialogs.KeyPressDialog.Show(this, kp),
            WaitTimeAction wt => Dialogs.WaitTimeDialog.Show(this, wt),
            WaitForPixelColorAction wpc => Dialogs.WaitForPixelColorDialog.Show(this, wpc),
            WaitForScreenChangeAction wsc => Dialogs.WaitForScreenChangeDialog.Show(this, wsc),
            WaitForTextInputAction wti => Dialogs.WaitForTextInputDialog.Show(this, wti),
            FindImageAction fia => Dialogs.FindImageDialog.Show(this, fia),
            FindTextOcrAction fto => Dialogs.FindTextOcrDialog.Show(this, fto),
            _ => Dialogs.ActionEditorForm.EditAction(this, step.Action)
        };

        if (edited is null) return;
        _app.ReplaceAction(idx, edited);
        BeginInvoke(() => SelectRowSafely(idx));
    }

    private void ShowSearchDialog()
    {
        var query = Dialogs.SimpleTextPrompt.Show(this, title: "Search", message: "検索文字列を入力してください:");
        if (string.IsNullOrWhiteSpace(query)) return;

        var startIndex = gridActions.CurrentCell?.RowIndex ?? 0;
        for (int offset = 1; offset <= _rows.Count; offset++)
        {
            var i = (startIndex + offset) % _rows.Count;
            var row = _rows[i];
            if ((row.Action?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Value?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Label?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Comment?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                SelectRowSafely(i);
                return;
            }
        }

        MessageBox.Show(this, "見つかりませんでした。", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static Color GetPixelColorAt(Point p)
    {
        using var bmp = new Bitmap(1, 1);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(p.X, p.Y, 0, 0, new Size(1, 1));
        }
        return bmp.GetPixel(0, 0);
    }

    private void ExportToCsv()
    {
        if (_app.CurrentMacro.Count == 0)
        {
            MessageBox.Show(this, "Export対象のアクションがありません。", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Title = "Export CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            AddExtension = true
        };

        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        static string Csv(string? s)
        {
            s ??= "";
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            {
                return '"' + s.Replace("\"", "\"\"") + '"';
            }
            return s;
        }

        var lines = new List<string>
        {
            "No,Label,Action,Value,Comment"
        };

        for (int i = 0; i < _app.CurrentMacro.Steps.Count; i++)
        {
            var step = _app.CurrentMacro.Steps[i];
            var action = step.Action;
            lines.Add(string.Join(",",
                (i + 1).ToString(),
                Csv(step.Label),
                Csv(action.Kind),
                Csv(action.DisplayValue),
                Csv(step.Comment)));
        }

        File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
        MessageBox.Show(this, "CSVを出力しました。", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ScheduleMacro()
    {
        var result = Dialogs.ScheduleMacroDialog.Show(this, _scheduledAt);
        if (result is null) return;

        if (result.Clear)
        {
            ClearSchedule();
            return;
        }

        if (result.RunAt is null) return;

        var runAt = result.RunAt.Value;
        var due = runAt - DateTime.Now;
        if (due <= TimeSpan.Zero)
        {
            MessageBox.Show(this, "未来の時刻を指定してください。", "Schedule", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ClearSchedule();
        _scheduledAt = runAt;
        _scheduleTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                BeginInvoke(() =>
                {
                    // 1回実行
                    ClearSchedule();
                    if (_app.State == MacroTool.Application.AppState.Stopped)
                    {
                        _app.Play();
                    }
                });
            }
            catch
            {
                // ignore
            }
        }, null, due, Timeout.InfiniteTimeSpan);
    }

    private void ClearSchedule()
    {
        _scheduleTimer?.Dispose();
        _scheduleTimer = null;
        _scheduledAt = null;
    }

    private void StartRecording()
    {
        if (_app.State != MacroTool.Application.AppState.Stopped) return;

        bool ok = _app.StartRecording(clearExisting: true);
        if (!ok)
        {
            MessageBox.Show(
                "録画開始に失敗しました。\n管理者権限が必要な場合があります。",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // ===== Status =====
    private void UpdateStatusBar()
    {
        var state = _app.State;

        _lblState.Text = state switch
        {
            MacroTool.Application.AppState.Recording => "状態: 録画中（Escで停止）",
            MacroTool.Application.AppState.Playing => "状態: 再生中（Escで停止）",
            _ => "状態: 停止"
        };

        lblCount.Text = $"{_app.ActionCount} actions";
        lblTime.Text = $"完了まで {FormatHms(_app.UntilDone())}";
        _lblElapsed.Text = $"経過 {FormatHms(_app.Elapsed())}";

        _lblSchedule.Text = _scheduledAt is null
            ? "スケジュール: なし"
            : $"スケジュール: {_scheduledAt:yyyy/MM/dd HH:mm:ss}";
    }

    private void UpdateButtons()
    {
        var state = _app.State;
        bool hasMacro = _app.ActionCount > 0;

        bool stopped = state == MacroTool.Application.AppState.Stopped;
        bool hasSelection = gridActions.SelectedRows.Count > 0;

        bool recordEnabled = stopped;
        bool playEnabled = stopped && hasMacro;
        bool stopEnabled = !stopped;

        SetToolStripEnabled(tsbRecord, recordEnabled);
        SetToolStripEnabled(tsbPlay, playEnabled);
        SetToolStripEnabled(tsbStop, stopEnabled);

        // Playback タブ側の同等ボタン
        if (_tsbRecordPb != null) SetToolStripEnabled(_tsbRecordPb, recordEnabled);
        if (_tsbPlayPb != null) SetToolStripEnabled(_tsbPlayPb, playEnabled);
        if (_tsbStopPb != null) SetToolStripEnabled(_tsbStopPb, stopEnabled);

        // Edit/Delete
        tsbDelete.Enabled = stopped && hasSelection;
        tsbEdit.Enabled = stopped && gridActions.SelectedRows.Count == 1;

        // Add action menus (将来実装予定の項目は表示しない)
        tsdMouse.Enabled = stopped;
        tsdTextKey.Enabled = stopped;
        tsdWait.Enabled = stopped;
        tsdImageOcr.Enabled = stopped;
        tsdMisc.Enabled = stopped;

        // Search
        tsbSearchReplace.Enabled = stopped && hasMacro;

        // File menu extras
        if (_miExportCsv != null) _miExportCsv.Enabled = stopped && hasMacro;
        if (_miScheduleMacro != null) _miScheduleMacro.Enabled = stopped;

        // Range play
        if (_miPlayUntilSelected != null) _miPlayUntilSelected.Enabled = playEnabled && hasSelection;
        if (_miPlayFromSelected != null) _miPlayFromSelected.Enabled = playEnabled && hasSelection;
        if (_miPlaySelectedOnly != null) _miPlaySelectedOnly.Enabled = playEnabled && hasSelection;
        if (_miPlayUntilSelectedPb != null) _miPlayUntilSelectedPb.Enabled = playEnabled && hasSelection;
        if (_miPlayFromSelectedPb != null) _miPlayFromSelectedPb.Enabled = playEnabled && hasSelection;
        if (_miPlaySelectedOnlyPb != null) _miPlaySelectedOnlyPb.Enabled = playEnabled && hasSelection;
    }

    private static void SetToolStripEnabled(ToolStripItem item, bool enabled)
    {
        item.Enabled = enabled;

        if (item is ToolStripSplitButton sb)
        {
            foreach (ToolStripItem mi in sb.DropDownItems)
                mi.Enabled = enabled;
        }
    }

    private static string FormatHms(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
        int hours = (int)ts.TotalHours;
        return $"{hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }

    // ===== HotKey (Esc) =====
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (!NativeMethods.RegisterHotKey(Handle, HOTKEY_ID_STOP, NativeMethods.MOD_NOREPEAT, (uint)Keys.Escape))
        {
            _lblState.Text = "状態: 停止（Esc停止の登録に失敗）";
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        NativeMethods.UnregisterHotKey(Handle, HOTKEY_ID_STOP);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID_STOP)
        {
            _app.StopAll();
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            if (!ConfirmSaveIfDirty("終了"))
            {
                e.Cancel = true;
                return;
            }
        }

        _app.UserNotification -= OnUserNotification;
        _app.StepExecuting -= OnStepExecuting;
        _app.StopAll();
        _statusTimer.Stop();
        base.OnFormClosing(e);
    }

    // ===== MenuStrip(File) =====
    private void openToolStripMenuItem_Click(object sender, EventArgs e) => LoadMacroFromFile();
    private void saveToolStripMenuItem_Click(object sender, EventArgs e) => SaveMacro();
    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) => SaveMacroAs();
    private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    // ===== Grid =====
    private void RefreshGridFromDomain()
    {
        _suppressGridCommit = true;
        try
        {
            _rows.Clear();

            int no = 1;
            foreach (var step in _app.CurrentMacro.Steps)
                _rows.Add(ActionRow.FromDomain(no++, step));
        }
        finally
        {
            _suppressGridCommit = false;
        }
    }

    private void RefreshGridFromDomainPreserveSelection()
    {
        int? rowIndex = null;
        string? colName = null;
        int firstDisplayed = -1;

        if (gridActions.CurrentCell is DataGridViewCell cell)
        {
            rowIndex = cell.RowIndex;
            colName = gridActions.Columns[cell.ColumnIndex].Name;
        }

        if (gridActions.RowCount > 0)
        {
            try { firstDisplayed = gridActions.FirstDisplayedScrollingRowIndex; } catch { }
        }

        RefreshGridFromDomain();

        if (rowIndex is int r && r >= 0 && r < gridActions.RowCount)
        {
            int c = 0;
            if (!string.IsNullOrEmpty(colName) && gridActions.Columns.Contains(colName))
                c = gridActions.Columns[colName].Index;

            gridActions.CurrentCell = gridActions.Rows[r].Cells[c];
            gridActions.Rows[r].Selected = true;
        }

        if (firstDisplayed >= 0 && firstDisplayed < gridActions.RowCount)
        {
            try { gridActions.FirstDisplayedScrollingRowIndex = firstDisplayed; } catch { }
        }
    }

    private void OnUserNotification(object? sender, string msg)
    {
        if (IsDisposed) return;

        BeginInvoke(new Action(() =>
            MessageBox.Show(this, msg, "Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)));
    }

    private void GridActions_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (gridActions.Columns[e.ColumnIndex].Name != "colIcon") return;
        if (gridActions.Rows[e.RowIndex].DataBoundItem is not ActionRow row) return;

        if (!string.IsNullOrWhiteSpace(row.IconKey) && imageList1.Images.ContainsKey(row.IconKey))
            e.Value = imageList1.Images[row.IconKey];
        else
            e.Value = null;

        e.FormattingApplied = true;
    }

    private void GridActions_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C) { CopySelectedRows(); e.Handled = true; return; }
        if (e.Control && e.KeyCode == Keys.X) { CutSelectedRows(); e.Handled = true; return; }
        if (e.Control && e.KeyCode == Keys.V) { PasteRows(); e.Handled = true; return; }
        if (e.Control && e.KeyCode == Keys.Z) { _app.Undo(); e.Handled = true; return; }
        if (e.Control && e.KeyCode == Keys.Y) { _app.Redo(); e.Handled = true; return; }

        if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedRows();
            e.Handled = true;
        }
    }

    private void GridActions_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        var colName = gridActions.Columns[e.ColumnIndex].Name;
        if (colName != nameof(colLabel) && colName != nameof(colComment))
            return;

        var row = gridActions.Rows[e.RowIndex];
        gridActions.ClearSelection();
        row.Selected = true;
    }

    private void ConfigureGridColumns()
    {
        gridActions.AutoGenerateColumns = false;
        gridActions.AllowUserToAddRows = false;
        gridActions.AllowUserToDeleteRows = false;
        gridActions.ReadOnly = false;
        gridActions.MultiSelect = true;
        gridActions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridActions.RowHeadersVisible = false;

        if (gridActions.Columns["colNo"] is DataGridViewColumn colNo)
        {
            colNo.DataPropertyName = nameof(ActionRow.No);
            colNo.Width = 40;
            colNo.Resizable = DataGridViewTriState.False;
        }

        if (gridActions.Columns["colIcon"] is DataGridViewImageColumn colIcon)
        {
            colIcon.Width = 26;
            colIcon.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colIcon.Resizable = DataGridViewTriState.False;
            colIcon.DataPropertyName = "";
        }

        if (gridActions.Columns["colAction"] is DataGridViewColumn colAction)
        {
            colAction.DataPropertyName = nameof(ActionRow.Action);
            colAction.Width = 160;
        }

        if (gridActions.Columns["colValue"] is DataGridViewColumn colValue)
        {
            colValue.DataPropertyName = nameof(ActionRow.Value);
            colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colValue.FillWeight = 35;
        }

        if (gridActions.Columns["colLabel"] is DataGridViewColumn colLabelCol)
        {
            colLabelCol.DataPropertyName = nameof(ActionRow.Label);
            colLabelCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLabelCol.FillWeight = 20;
        }

        if (gridActions.Columns["colComment"] is DataGridViewColumn colCommentCol)
        {
            colCommentCol.DataPropertyName = nameof(ActionRow.Comment);
            colCommentCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colCommentCol.FillWeight = 45;
        }

        gridActions.DataError += (_, e) => e.ThrowException = false;
    }

    // ===== Edit -> Domain反映 =====
    private void GridActions_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressGridCommit) return;
        if (e.RowIndex < 0) return;
        if (e.RowIndex >= _rows.Count) return;

        var column = gridActions.Columns[e.ColumnIndex];
        if (column.Name != nameof(colLabel) && column.Name != nameof(colComment))
            return;

        var row = _rows[e.RowIndex];
        var label = row.Label ?? "";
        var comment = row.Comment ?? "";
        _app.UpdateStepMetadata(e.RowIndex, label, comment);
    }

    // ===== Delete / Copy / Cut / Paste =====
    private void DeleteSelectedRows()
    {
        if (_app.State != MacroTool.Application.AppState.Stopped)
            return;

        if (gridActions.SelectedRows.Count == 0)
            return;

        if (_settings.Ui.ConfirmDelete)
        {
            var result = MessageBox.Show(
                this,
                $"選択した {gridActions.SelectedRows.Count} 行を削除しますか？",
                "確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;
        }

        int? currentRow = gridActions.CurrentCell?.RowIndex;
        string? currentColName = gridActions.CurrentCell is null
            ? null
            : gridActions.Columns[gridActions.CurrentCell.ColumnIndex].Name;

        int firstDisplayed = -1;
        try { firstDisplayed = gridActions.FirstDisplayedScrollingRowIndex; } catch { }

        var indices = gridActions.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.Index)
            .ToArray();

        _app.DeleteSteps(indices);

        BeginInvoke(new Action(() =>
        {
            if (gridActions.RowCount == 0) return;

            int targetRow = 0;
            if (currentRow is int r)
                targetRow = Math.Min(r, gridActions.RowCount - 1);

            int targetCol = 0;
            if (!string.IsNullOrEmpty(currentColName) && gridActions.Columns.Contains(currentColName))
                targetCol = gridActions.Columns[currentColName].Index;

            gridActions.CurrentCell = gridActions.Rows[targetRow].Cells[targetCol];
            gridActions.Rows[targetRow].Selected = true;

            if (firstDisplayed >= 0 && firstDisplayed < gridActions.RowCount)
            {
                try { gridActions.FirstDisplayedScrollingRowIndex = firstDisplayed; } catch { }
            }
        }));
    }

    private int GetInsertIndexForPaste()
    {
        if (gridActions.CurrentCell is null) return _rows.Count;
        int idx = gridActions.CurrentCell.RowIndex;
        if (idx < 0) return _rows.Count;
        if (idx > _rows.Count) return _rows.Count;
        return idx;
    }

    private int[] GetSelectedRowIndices()
    {
        return gridActions.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.Index)
            .OrderBy(i => i)
            .ToArray();
    }

    private void SelectRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= gridActions.RowCount) return;

        int colIndex = gridActions.CurrentCell?.ColumnIndex ?? 0;
        colIndex = Math.Min(colIndex, gridActions.ColumnCount - 1);

        gridActions.CurrentCell = gridActions.Rows[rowIndex].Cells[colIndex];
        gridActions.Rows[rowIndex].Selected = true;

        try
        {
            if (rowIndex < gridActions.FirstDisplayedScrollingRowIndex ||
                rowIndex >= gridActions.FirstDisplayedScrollingRowIndex + gridActions.DisplayedRowCount(false))
            {
                gridActions.FirstDisplayedScrollingRowIndex = rowIndex;
            }
        }
        catch { }
    }

    private void CopySelectedRows()
    {
        if (_app.State != MacroTool.Application.AppState.Stopped) return;

        var indices = GetSelectedRowIndices();
        if (indices.Length == 0) return;

        try
        {
            var text = _app.CopyStepsToClipboardText(indices);
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CutSelectedRows()
    {
        if (_app.State != MacroTool.Application.AppState.Stopped) return;

        var indices = GetSelectedRowIndices();
        if (indices.Length == 0) return;

        var r = MessageBox.Show(this, $"選択した {indices.Length} 行を切り取りますか？",
            "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (r != DialogResult.Yes) return;

        try
        {
            var text = _app.CutStepsToClipboardText(indices);
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);

            BeginInvoke(new Action(() =>
            {
                if (gridActions.RowCount == 0) return;
                int target = Math.Min(indices.Min(), gridActions.RowCount - 1);
                SelectRow(target);
            }));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PasteRows()
    {
        if (_app.State != MacroTool.Application.AppState.Stopped) return;
        if (!Clipboard.ContainsText()) return;

        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text)) return;

        int insertIndex = GetInsertIndexForPaste();

        try
        {
            int added = _app.PasteStepsFromClipboardText(insertIndex, text);
            if (added <= 0) return;

            BeginInvoke(new Action(() =>
            {
                if (gridActions.RowCount == 0) return;
                SelectRow(Math.Min(insertIndex, gridActions.RowCount - 1));
            }));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "貼り付けエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GridActions_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        if (e.RowIndex < 0) return;

        if (!gridActions.Rows[e.RowIndex].Selected)
        {
            gridActions.ClearSelection();
            gridActions.CurrentCell = gridActions.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
            gridActions.Rows[e.RowIndex].Selected = true;
        }
    }

    // ===== UI =====
    private void UpdateUi()
    {
        UpdateCurrentFileTitle();
        UpdateStatusBar();
        UpdateButtons();
    }

    private void ApplyToolStripIcons()
    {
        tsRecordEdit.ImageScalingSize = new System.Drawing.Size(32, 32);

        SetIcon(tsbPlay, "Play");
        SetIcon(tsbRecord, "Record");
        SetIcon(tsbStop, "Stop");
    }

    private void SetIcon(ToolStripItem item, string key)
    {
        if (!imageListToolStrip.Images.ContainsKey(key))
            return;

        item.Image = imageListToolStrip.Images[key];
        item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        item.TextImageRelation = TextImageRelation.ImageAboveText;
    }

    private void RunWithoutDirty(Action action)
    {
        _suppressDirty = true;
        try { action(); }
        finally { _suppressDirty = false; }
    }

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!ConfirmSaveIfDirty("新規作成"))
            return;

        RunWithoutDirty(() => _app.New());

        _currentMacroPath = null;
        _isDirty = false;

        RefreshGridFromDomain();
        UpdateUi();
    }

    private void LoadMacroFromPath(string path)
    {
        if (!ConfirmSaveIfDirty("読み込み"))
            return;

        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"ファイルが見つかりません。\n{path}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            _recentFiles.Remove(path);
            return;
        }

        try
        {
            _suppressDirty = true;
            _app.Load(path);
            _suppressDirty = false;

            _currentMacroPath = path;
            _isDirty = false;

            _recentFiles.Add(path);
            UpdateUi();
        }
        catch (Exception ex)
        {
            _suppressDirty = false;

            MessageBox.Show(this, $"読み込みに失敗しました。\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RebuildRecentFilesMenu()
    {
        recentFilesToolStripMenuItem.DropDownItems.Clear();

        var recents = _recentFiles.Items;

        if (recents.Count == 0)
        {
            recentFilesToolStripMenuItem.DropDownItems.Add(
                new ToolStripMenuItem("(none)") { Enabled = false }
            );
            return;
        }

        int n = 1;
        foreach (var path in recents)
        {
            var text = $"{n}. {Path.GetFileName(path)}";

            var item = new ToolStripMenuItem(text)
            {
                ToolTipText = path,
                Tag = path
            };

            item.Click += (_, __) => LoadMacroFromPath(path);

            recentFilesToolStripMenuItem.DropDownItems.Add(item);
            n++;
        }

        recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

        var clear = new ToolStripMenuItem("Clear Recent Files");
        clear.Click += (_, __) =>
        {
            _recentFiles.Clear();
            RebuildRecentFilesMenu();
        };

        recentFilesToolStripMenuItem.DropDownItems.Add(clear);
    }
    private void OnStepExecuting(object? sender, StepExecutingEventArgs e)
    {
        if (IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            if (_app.State != MacroTool.Application.AppState.Playing) return;
            SelectRowSafely(e.StepIndex);
            try { gridActions.FirstDisplayedScrollingRowIndex = e.StepIndex; } catch { }
            }));
    }

}
