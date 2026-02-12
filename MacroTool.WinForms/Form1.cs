using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Application.Services;
using MacroTool.WinForms.Core;
using MacroTool.WinForms.Settings;
using System.ComponentModel;
using DomainKeyDown = MacroTool.Domain.Macros.KeyDown;
using DomainKeyUp = MacroTool.Domain.Macros.KeyUp;
using DomainMacroAction = MacroTool.Domain.Macros.MacroAction;
using DomainMouseClick = MacroTool.Domain.Macros.MouseClick;

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
    private readonly System.Windows.Forms.Timer _statusTimer = new();

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
    }

    private void UpdateButtons()
    {
        var state = _app.State;
        bool hasMacro = _app.ActionCount > 0;

        bool recordEnabled = state == MacroTool.Application.AppState.Stopped;
        bool playEnabled = state == MacroTool.Application.AppState.Stopped && hasMacro;
        bool stopEnabled = state != MacroTool.Application.AppState.Stopped;

        SetToolStripEnabled(tsbRecord, recordEnabled);
        SetToolStripEnabled(tsbPlay, playEnabled);
        SetToolStripEnabled(tsbStop, stopEnabled);
        tsbDelete.Enabled = state == MacroTool.Application.AppState.Stopped && gridActions.SelectedRows.Count > 0;
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

    // ===== Inner types =====
    private sealed class ActionRow
    {
        public int No { get; set; }
        public string IconKey { get; set; } = "";
        public string Action { get; set; } = "";
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public string Comment { get; set; } = "";

        public static ActionRow FromDomain(int no, MacroTool.Domain.Macros.MacroStep step)
        {
            return new ActionRow
            {
                No = no,
                IconKey = ToIconKey(step.Action),
                Action = step.Action.Kind,
                Value = step.Action.DisplayValue,
                Label = step.Label ?? "",
                Comment = step.Comment ?? ""
            };
        }

        private static string ToIconKey(MacroTool.Domain.Macros.MacroAction action)
        {
            return action switch
            {
                DomainMouseClick => "Mouse",
                DomainKeyDown or DomainKeyUp => "Keyboard",
                _ => "Misc"
            };
        }
    }
}
