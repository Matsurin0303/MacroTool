using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Application.Services;
using MacroTool.WinForms.Core;
using MacroTool.WinForms.Settings;
using System.ComponentModel;
using MacroTool.Domain.Macros;
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

        // Settings 読み込み（起動時に即反映）
        _settings = _settingsStore.Load();
        ApplyUiSettings();
        ApplyPlaybackSettingsToAccessor();

        ApplyToolStripIcons();

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

        // Status更新タイマー
        _statusTimer.Interval = 250;
        _statusTimer.Tick += (_, __) => UpdateStatusBar();
        _statusTimer.Start();

        UpdateUi();
        RefreshGridFromDomain();
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

        _miExportCsv = new ToolStripMenuItem("Export CSV...")
        {
            Name = "miExportCsv"
        };
        _miExportCsv.Click += (_, __) => ExportToCsv();

        _miScheduleMacro = new ToolStripMenuItem("Schedule Macro...")
        {
            Name = "miScheduleMacro"
        };
        _miScheduleMacro.Click += (_, __) => ScheduleMacro();

        // 既存の区切り線（SaveAs と Settings の間）より前に差し込む
        var insertBefore = fileToolStripMenuItem.DropDownItems.IndexOf(toolStripMenuItem2);
        if (insertBefore < 0) insertBefore = fileToolStripMenuItem.DropDownItems.Count;
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore, _miExportCsv);
        fileToolStripMenuItem.DropDownItems.Insert(insertBefore + 1, _miScheduleMacro);
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
        // Mouse
        tsdMouse.DropDownItems.Clear();
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Click...", null, (_, __) => AddMouseClick()));
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Move...", null, (_, __) => AddMouseMove()));
        tsdMouse.DropDownItems.Add(new ToolStripMenuItem("Wheel...", null, (_, __) => AddMouseWheel()));

        // Text/Key
        tsdTextKey.DropDownItems.Clear();
        tsdTextKey.DropDownItems.Add(new ToolStripMenuItem("Key press...", null, (_, __) => AddKeyPress()));
        tsdTextKey.DropDownItems.Add(new ToolStripMenuItem("Hotkey...", null, (_, __) => AddHotkey()));

        // Wait
        tsdWait.DropDownItems.Clear();
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait...", null, (_, __) => AddWaitTime()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for pixel color...", null, (_, __) => AddWaitForPixelColor()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for screen change...", null, (_, __) => AddWaitForScreenChange()));
        tsdWait.DropDownItems.Add(new ToolStripMenuItem("Wait for text input...", null, (_, __) => AddWaitForTextInput()));

        // Image/OCR
        tsdImageOcr.DropDownItems.Clear();
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Find image (file)...", null, (_, __) => AddFindImageFromFile()));
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Find image (capture)...", null, (_, __) => AddFindImageFromCapture()));
        tsdImageOcr.DropDownItems.Add(new ToolStripMenuItem("Find text (OCR)...", null, (_, __) => AddFindTextOcr()));

        // Misc / Control Flow
        tsdMisc.DropDownItems.Clear();
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Repeat...", null, (_, __) => AddRepeat()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Go to...", null, (_, __) => AddGoTo()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("If...", null, (_, __) => AddIf()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Embed macro file...", null, (_, __) => AddEmbedMacroFile()));
        tsdMisc.DropDownItems.Add(new ToolStripMenuItem("Execute program...", null, (_, __) => AddExecuteProgram()));
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
        var action = new MouseClickAction
        {
            X = p.X,
            Y = p.Y,
            Relative = false,
            Button = MouseButton.Left,
            ClickType = MouseClickType.Click
        };
        AddActionWithEditor(action);
    }

    private void AddMouseMove()
    {
        var p = Cursor.Position;
        var action = new MouseMoveAction
        {
            Relative = false,
            StartX = p.X,
            StartY = p.Y,
            EndX = p.X,
            EndY = p.Y,
            DurationMs = 200
        };
        AddActionWithEditor(action);
    }

    private void AddMouseWheel()
    {
        var action = new MouseWheelAction
        {
            Orientation = WheelOrientation.Vertical,
            Value = 120
        };
        AddActionWithEditor(action);
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
        var action = new WaitTimeAction { Milliseconds = 1000 };
        AddActionWithEditor(action);
    }

    private void AddWaitForPixelColor()
    {
        var p = Cursor.Position;
        var color = GetPixelColorAt(p);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        var action = new WaitForPixelColorAction
        {
            X = p.X,
            Y = p.Y,
            ColorHex = hex,
            TolerancePercent = 10,
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 5000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddWaitForScreenChange()
    {
        var action = new WaitForScreenChangeAction
        {
            Area = new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 5000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
    }

    private void AddWaitForTextInput()
    {
        var action = new WaitForTextInputAction
        {
            TextToWaitFor = "OK",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 30000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
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
            MousePosition = MousePosition.Center,
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
            MousePosition = MousePosition.Center,
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
        var action = new FindTextOcrAction
        {
            TextToSearchFor = "",
            Language = OcrLanguage.Japanese,
            Area = new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = MousePosition.Center,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            TimeoutMs = 5000,
            FalseGoTo = GoToTarget.Next()
        };
        AddActionWithEditor(action);
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
            KeyPressAction kp => Dialogs.KeyPressDialog.Show(this, kp),
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

}
