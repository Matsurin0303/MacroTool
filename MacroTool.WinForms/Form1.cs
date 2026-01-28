using System.IO;
using MacroTool.WinForms.Core; // 追加（置いた場所に合わせる）
using MacroTool.Application.Services;
using MacroTool.Domain.Macros;
using MacroTool.WinForms.Core;
using System;
using System.Drawing;
using System.ComponentModel;
using DomainMacroAction = MacroTool.Domain.Macros.MacroAction;
using DomainMouseClick = MacroTool.Domain.Macros.MouseClick;
using DomainKeyDown = MacroTool.Domain.Macros.KeyDown;
using DomainKeyUp = MacroTool.Domain.Macros.KeyUp;

namespace MacroTool.WinForms;

public partial class Form1 : Form
{

    private const int HOTKEY_ID_STOP = 1; // Esc停止用

    // Fileタブ（Backstage）用
    private string? _currentMacroPath;

    // Core
    private readonly MacroAppService _app;
    private readonly BindingList<ActionRow> _rows = new();
    private readonly RecentFilesStore _recentFiles = new(appName: "MacroTool", maxItems: 10);

    // Recent Files の識別用タグ（メニューに差し込む項目だけ消せるように）

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


    public Form1(MacroAppService app)
    {
        //コンストラクタ
        InitializeComponent();
        ApplyToolStripIcons();
        recentFilesToolStripMenuItem.DropDownOpening += (_, __) => RebuildRecentFilesMenu();

        settingsToolStripMenuItem.Click += (_, __) =>
            MessageBox.Show(this, "Settings は未実装です。", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        _app = app;

        _app.UserNotification += OnUserNotification;
        // --- StatusStripに「状態」「経過」を追加（Designerを汚さない） ---
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

        // --- Coreエンジン ---
        _app.StateChanged += (_, __) => BeginInvoke(new Action(UpdateUi));
        _app.MacroChanged += (_, __) => BeginInvoke(new Action(() =>
        {
            if (!_suppressDirty) _isDirty = true;
            RefreshGridFromDomain();
            UpdateUi();
        }));



        // --- 一覧（Designerで作成したgridActionsを使う） ---
        gridActions.AutoGenerateColumns = false;
        gridActions.DataSource = _rows;
        ConfigureGridColumns();
        gridActions.CellFormatting += GridActions_CellFormatting;
        gridActions.RowTemplate.Height = 22; // アイコン見やすく（任意）


        // --- ToolStrip（Record/Stop/Play） ---
        HookToolStripButtons();

        // --- ステータス更新タイマー ---
        _statusTimer.Interval = 250;
        _statusTimer.Tick += (_, __) => UpdateStatusBar();
        _statusTimer.Start();

        UpdateUi();
    }

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
        // 例: MacroTool - 4.mcr
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

        switch (result)
        {
            case DialogResult.Yes:
                // 保存に成功したら続行、失敗/キャンセルなら中止
                return TrySaveWithPrompt(false);

            case DialogResult.No:
                // 保存せず続行
                return true;

            default:
                // Cancel（保存せず、続行もしない）
                return false;
        }
    }

    // ===== ToolStrip (Record/Stop/Play) =====
    private void HookToolStripButtons()
    {
        static void Bind(ToolStripItem item, Action handler)
        {
            if (item is ToolStripSplitButton sb)
                sb.ButtonClick += (_, __) => handler();
            else
                item.Click += (_, __) => handler();
        }

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

        // 停止：録画/再生できる、停止は不要
        // 録画中：停止できる、再生は不可、録画は不可
        // 再生中：停止できる、録画は不可、再生は不可（多重再生防止）
        bool hasMacro = _app.ActionCount > 0;

        bool recordEnabled = state == MacroTool.Application.AppState.Stopped;
        bool playEnabled = state == MacroTool.Application.AppState.Stopped && hasMacro;
        bool stopEnabled = state != MacroTool.Application.AppState.Stopped;

        SetToolStripEnabled(tsbRecord, recordEnabled);
        SetToolStripEnabled(tsbPlay, playEnabled);
        SetToolStripEnabled(tsbStop, stopEnabled);
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
        _app.StopAll();          // 再生/録画停止だけは明示
        _app.Dispose();
        _statusTimer.Stop();
        base.OnFormClosing(e);
    }
    // ===== MenuStrip(File) =====
    private void openToolStripMenuItem_Click(object sender, EventArgs e) => LoadMacroFromFile();
    private void saveToolStripMenuItem_Click(object sender, EventArgs e) => SaveMacro();
    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) => SaveMacroAs();
    private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    private sealed class ActionRow
    {
        public int No { get; set; }
        public string IconKey { get; set; } = "";
        public string Action { get; set; } = "";
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public string Comment { get; set; } = "";
    }

    private void RefreshGridFromDomain()
    {
        _rows.RaiseListChangedEvents = false;
        _rows.Clear();

        int i = 1;
        foreach (var step in _app.CurrentMacro.Steps)
        {
            _rows.Add(new ActionRow
            {
                No = i++,
                IconKey = ToIconKey(step.Action),
                Action = step.Action.Kind,
                Value = step.Action.DisplayValue,
                Label = "",    // まだドメインに無ければ空でOK
                Comment = ""   // まだドメインに無ければ空でOK
            });
        }

        _rows.RaiseListChangedEvents = true;
        _rows.ResetBindings();
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
        // ヘッダ行などは無視
        if (e.RowIndex < 0) return;

        // 2列目のアイコン列だけ処理（列名が違う場合はここを変更）
        if (gridActions.Columns[e.ColumnIndex].Name != "colIcon") return;

        // バインドされている行データを取得
        if (gridActions.Rows[e.RowIndex].DataBoundItem is not ActionRow row) return;

        // row.Action は "MouseClick" / "KeyDown" / "KeyUp" 等が入っている前提
        // ImageListのName(Key)を "mouse" "keyboard" にしている場合
        if (!string.IsNullOrWhiteSpace(row.IconKey) && imageList1.Images.ContainsKey(row.IconKey))
            e.Value = imageList1.Images[row.IconKey];
        else
            e.Value = null;

        e.FormattingApplied = true;
    }
    private void ConfigureGridColumns()
    {
        gridActions.AutoGenerateColumns = false;
        gridActions.AllowUserToAddRows = false;
        gridActions.AllowUserToDeleteRows = false;
        gridActions.ReadOnly = true;
        gridActions.MultiSelect = false;
        gridActions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        // 行ヘッダの余白がいらなければ
        gridActions.RowHeadersVisible = false;

        // #列
        if (gridActions.Columns["colNo"] is DataGridViewColumn colNo)
        {
            colNo.DataPropertyName = nameof(ActionRow.No);
            colNo.Width = 40;
            colNo.Resizable = DataGridViewTriState.False;
        }

        // アイコン列（ImageColumn）
        if (gridActions.Columns["colIcon"] is DataGridViewImageColumn colIcon)
        {
            colIcon.Width = 26;
            colIcon.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colIcon.Resizable = DataGridViewTriState.False;
            colIcon.DataPropertyName = ""; // バインドしない（CellFormattingで埋める）
        }

        // Action
        if (gridActions.Columns["colAction"] is DataGridViewColumn colAction)
        {
            colAction.DataPropertyName = nameof(ActionRow.Action);
            colAction.Width = 160;
        }

        // Value（伸びる）
        if (gridActions.Columns["colValue"] is DataGridViewColumn colValue)
        {
            colValue.DataPropertyName = nameof(ActionRow.Value);
            colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colValue.FillWeight = 35;
        }

        // Label
        if (gridActions.Columns["colLabel"] is DataGridViewColumn colLabel)
        {
            colLabel.DataPropertyName = nameof(ActionRow.Label);
            colLabel.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLabel.FillWeight = 20;
        }

        // Comment
        if (gridActions.Columns["colComment"] is DataGridViewColumn colComment)
        {
            colComment.DataPropertyName = nameof(ActionRow.Comment);
            colComment.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colComment.FillWeight = 45;
        }

        // 既定エラーダイアログ抑制（保険）
        gridActions.DataError += (_, e) => e.ThrowException = false;
    }
    private static class IconKeys
    {
        public const string Mouse = "Mouse";
        public const string Keyboard = "Keyboard";
        public const string Misc = "Misc";
    }

    private static string ToIconKey(DomainMacroAction action)
    {
        return action switch
        {
            DomainMouseClick => IconKeys.Mouse,
            DomainKeyDown or DomainKeyUp => IconKeys.Keyboard,
            _ => IconKeys.Misc
        };
    }
    private void UpdateUi()
    {
        UpdateCurrentFileTitle();
        UpdateStatusBar();
        UpdateButtons();
    }
    private void ApplyToolStripIcons()
    {
        // ToolStrip側の表示サイズ（任意：見た目調整）
        tsRecordEdit.ImageScalingSize = new System.Drawing.Size(32, 32);

        SetIcon(tsbPlay, "Play");
        SetIcon(tsbRecord, "Record");
        SetIcon(tsbStop, "Stop");

        // 必要なら追加
        // SetIcon(tsbMouse, "Mouse");
        // SetIcon(tsbTextKey, "Keyboard");
        // SetIcon(tsbWait, "Wait");
        // SetIcon(tsbImageOcr, "Image");
        // SetIcon(tsbMisc, "Misc");
        // SetIcon(tsbEdit, "Edit");
        // SetIcon(tsbDelete, "Delete");
        // SetIcon(tsbSearchReplace, "Search");
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
        // 未保存確認（仕様：Yes/No/Cancel）
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

            _recentFiles.Add(path); // 先頭へ移動
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
            RebuildRecentFilesMenu(); // すぐ反映
        };

        recentFilesToolStripMenuItem.DropDownItems.Add(clear);
    }


}
