using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacroTool.Domain.Macros;

// WinForms の Control.MousePosition（Point）と、ドメイン enum の MousePosition が衝突するため別名を付ける
using DomainMousePosition = MacroTool.Domain.Macros.MousePosition;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Find image（2-7-1）設定ダイアログ。
/// UI仕様の見た目に寄せつつ、
/// - マクロ追加前の Test
/// - Area of desktop / Area of focused window の Define / Confirm Area
/// を追加。
/// </summary>
public sealed class FindImageDialog : Form
{
    // --- UI ---
    private readonly PictureBox _picTemplate;
    private readonly Button _btnCapture;
    private readonly Button _btnOpen;
    private readonly Button _btnClear;

    private readonly ComboBox _cmbArea;
    private readonly Button _btnDefineArea;
    private readonly Button _btnConfirmArea;

    private readonly NumericUpDown _numTolerance;
    private readonly Label _lblPercent;
    private readonly Button _btnTest;

    private readonly CheckBox _chkMouseAction;
    private readonly ComboBox _cmbMouseAction;
    private readonly ComboBox _cmbMousePos;

    private readonly CheckBox _chkSaveCoord;
    private readonly TextBox _txtSaveX;
    private readonly TextBox _txtSaveY;

    private readonly ComboBox _cmbTrueGoTo;

    private readonly NumericUpDown _numTimeoutSec;
    private readonly ComboBox _cmbFalseGoTo;

    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    // --- state ---
    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };
    private Rectangle _definedScreenRect = Rectangle.Empty; // Confirm Area 用（画面座標）

    private ImageTemplate _template = new();
    private Image? _preview;

    // Test 実行中ガード
    private CancellationTokenSource? _testCts;
    private bool _testing;
    private bool _savedControlBox;

    public FindImageAction Result { get; private set; } = new();

    public static FindImageAction? Show(IWin32Window owner, FindImageAction? initial)
    {
        using var dlg = new FindImageDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }

    private FindImageDialog(FindImageAction? initial)
    {
        Text = "Find image";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;

        // 仕様画像に近い横幅を確保（右側の Search area / Color tolerance が潰れないように）
        // ※ユーザー環境で 520px 幅だと ComboBox/NumericUpDown が切れるケースがあったため拡大
        ClientSize = new Size(640, 520);
        MinimumSize = new Size(640, 520);

        FormClosing += (_, __) => _testCts?.Cancel();

        // --- Group: Image to search ---
        var grpSpec = new GroupBox
        {
            Text = "Image to search",
            Dock = DockStyle.Top,
            Height = 175,
            Padding = new Padding(10)
        };

        var tblSpec = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // left: template preview
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        _picTemplate = new PictureBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill
        };

        var pnlImgBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        _btnCapture = new Button { Text = "Capture...", Width = 78, Height = 26 };
        _btnOpen = new Button { Text = "Open...", Width = 70, Height = 26 };
        _btnClear = new Button { Text = "Clear", Width = 60, Height = 26 };
        pnlImgBtns.Controls.AddRange(new Control[] { _btnCapture, _btnOpen, _btnClear });

        left.Controls.Add(_picTemplate, 0, 0);
        left.Controls.Add(pnlImgBtns, 0, 1);

        // right: search area + tolerance
        // 4列(AutoSize)だと右側のボタン幅が優先され、ComboBox/NumericUpDown が 0px になり得るため
        // (ユーザー環境で「Search area が選べない / Color tolerance が入力できない」状態になる)
        // 3列 + FlowLayoutPanel で確実に入力欄の幅を確保する。
        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // label
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // input (stretch)
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));  // button (Define/Confirm/Test)

        // Search area 行は Define/Confirm を縦に置くため高さを確保
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        right.Controls.Add(new Label { Text = "Search area:", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 0);

        _cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, Margin = new Padding(0, 6, 0, 0) };
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });

        // Define/Confirm は常に見せる（非Area選択時は無効化）
        _btnDefineArea = new Button { Text = "Define...", Width = 110, Height = 26 };
        _btnConfirmArea = new Button { Text = "Confirm Area", Width = 110, Height = 26 };

        var pnlAreaButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0)
        };
        pnlAreaButtons.Controls.Add(_btnDefineArea);
        pnlAreaButtons.Controls.Add(_btnConfirmArea);

        right.Controls.Add(_cmbArea, 1, 0);
        right.Controls.Add(pnlAreaButtons, 2, 0);

        right.Controls.Add(new Label { Text = "Color tolerance:", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 1);

        var pnlTol = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0)
        };
        _numTolerance = new NumericUpDown { Minimum = 0, Maximum = 100, Width = 70 };
        _lblPercent = new Label { Text = "%", AutoSize = true, Margin = new Padding(6, 9, 0, 0) };
        pnlTol.Controls.Add(_numTolerance);
        pnlTol.Controls.Add(_lblPercent);

        right.Controls.Add(pnlTol, 1, 1);

        _btnTest = new Button { Text = "Test", Width = 80, Height = 26 };
        right.Controls.Add(_btnTest, 2, 1);

        tblSpec.Controls.Add(left, 0, 0);
        tblSpec.Controls.Add(right, 1, 0);
        grpSpec.Controls.Add(tblSpec);

        // --- Group: If image is found ---
        var grpFound = new GroupBox
        {
            Text = "If image is found",
            Dock = DockStyle.Top,
            Height = 150,
            Padding = new Padding(10)
        };

        var tblFound = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3
        };
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _chkMouseAction = new CheckBox { Text = "Mouse action:", AutoSize = true };
        _cmbMouseAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _cmbMouseAction.Items.AddRange(new object[]
        {
            nameof(MouseActionBehavior.Positioning),
            nameof(MouseActionBehavior.LeftClick),
            nameof(MouseActionBehavior.RightClick),
            nameof(MouseActionBehavior.MiddleClick),
            nameof(MouseActionBehavior.DoubleClick),
        });

        _cmbMousePos = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _cmbMousePos.Items.AddRange(new object[] { "Centered", "Top-left", "Top-right", "Bottom-left", "Bottom-right" });

        _chkSaveCoord = new CheckBox { Text = "Save X to:", AutoSize = true };
        _txtSaveX = new TextBox { Width = 120 };
        var lblAndY = new Label { Text = "and Y to:", AutoSize = true, Margin = new Padding(0, 9, 0, 0) };
        _txtSaveY = new TextBox { Width = 120 };

        var lblGoTo = new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 9, 0, 0) };
        _cmbTrueGoTo = CreateGoToCombo();

        tblFound.Controls.Add(_chkMouseAction, 0, 0);
        tblFound.Controls.Add(_cmbMouseAction, 1, 0);
        tblFound.Controls.Add(_cmbMousePos, 2, 0);
        tblFound.SetColumnSpan(_cmbMousePos, 2);

        tblFound.Controls.Add(_chkSaveCoord, 0, 1);
        tblFound.Controls.Add(_txtSaveX, 1, 1);
        tblFound.Controls.Add(lblAndY, 2, 1);
        tblFound.Controls.Add(_txtSaveY, 3, 1);

        tblFound.Controls.Add(lblGoTo, 0, 2);
        tblFound.Controls.Add(_cmbTrueGoTo, 1, 2);
        tblFound.SetColumnSpan(_cmbTrueGoTo, 3);

        grpFound.Controls.Add(tblFound);

        // --- Group: If image is not found ---
        var grpNotFound = new GroupBox
        {
            Text = "If image is not found",
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(10)
        };

        var tblNotFound = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        tblNotFound.Controls.Add(new Label { Text = "Continue waiting", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 0);
        _numTimeoutSec = new NumericUpDown { Minimum = 0, Maximum = 86400, Width = 80 };
        tblNotFound.Controls.Add(_numTimeoutSec, 1, 0);
        tblNotFound.Controls.Add(new Label { Text = "seconds and then", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 2, 0);

        tblNotFound.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 1);
        _cmbFalseGoTo = CreateGoToCombo();
        tblNotFound.Controls.Add(_cmbFalseGoTo, 1, 1);
        tblNotFound.SetColumnSpan(_cmbFalseGoTo, 3);

        grpNotFound.Controls.Add(tblNotFound);

        // --- bottom buttons ---
        _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };
        pnlBottom.Controls.Add(_btnOk);
        pnlBottom.Controls.Add(_btnCancel);

        // root
        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        root.Controls.Add(grpNotFound);
        root.Controls.Add(grpFound);
        root.Controls.Add(grpSpec);

        Controls.Add(root);
        Controls.Add(pnlBottom);

        // ---- initial ----
        var init = initial ?? CreateDefault();
        _template = init.Template ?? new ImageTemplate();
        _area = init.SearchArea ?? init.Area ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };

        _cmbArea.SelectedItem = ToAreaText(_area);
        _numTolerance.Value = Math.Clamp(init.ColorTolerancePercent, 0, 100);

        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _cmbMousePos.SelectedItem = ToMousePosText(init.MousePosition);

        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _txtSaveX.Text = init.SaveXVariable ?? "X";
        _txtSaveY.Text = init.SaveYVariable ?? "Y";

        SetGoToSelection(_cmbTrueGoTo, init.TrueGoTo);
        SetGoToSelection(_cmbFalseGoTo, init.FalseGoTo);

        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        LoadPreviewFromTemplate();

        ApplyEnableState();
        UpdateAreaButtons();

        // ---- events ----
        _chkMouseAction.CheckedChanged += (_, __) => ApplyEnableState();
        _chkSaveCoord.CheckedChanged += (_, __) => ApplyEnableState();

        _cmbArea.SelectedIndexChanged += (_, __) =>
        {
            OnAreaSelectionChanged();
            UpdateAreaButtons();
        };

        _cmbTrueGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbTrueGoTo);
        _cmbFalseGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbFalseGoTo);

        _btnCapture.Click += (_, __) => CaptureTemplate();
        _btnOpen.Click += (_, __) => OpenTemplate();
        _btnClear.Click += (_, __) => ClearTemplate();

        _btnDefineArea.Click += (_, __) => DefineArea();
        _btnConfirmArea.Click += (_, __) => ConfirmArea();

        _btnTest.Click += async (_, __) => await TestAsync();

        _btnOk.Click += (_, __) => Result = BuildResult();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _preview?.Dispose();
            _testCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyEnableState()
    {
        _cmbMouseAction.Enabled = _chkMouseAction.Checked;
        _cmbMousePos.Enabled = _chkMouseAction.Checked;

        _txtSaveX.Enabled = _chkSaveCoord.Checked;
        _txtSaveY.Enabled = _chkSaveCoord.Checked;
    }

    private void UpdateAreaButtons()
    {
        bool isArea = IsAreaSelection();

        // Define/Confirm は常に表示し、Area 選択時だけ有効化する。
        // （非表示にするとユーザーが「どこで確認するのか」迷いやすい）
        _btnDefineArea.Enabled = isArea && !_testing;
        _btnConfirmArea.Enabled = isArea && !_testing && _definedScreenRect != Rectangle.Empty;
    }

    private bool IsAreaSelection()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        return sel is "Area of desktop" or "Area of focused window";
    }

    private void OnAreaSelectionChanged()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        _area = sel switch
        {
            "Focused window" => new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            "Area of desktop" => new SearchArea { Kind = SearchAreaKind.AreaOfDesktop },
            "Area of focused window" => new SearchArea { Kind = SearchAreaKind.AreaOfFocusedWindow },
            _ => new SearchArea { Kind = SearchAreaKind.EntireDesktop }
        };

        if (!IsAreaSelection())
            _definedScreenRect = Rectangle.Empty;
    }

    private void CaptureTemplate()
    {
        using var f = new ScreenRegionCaptureForm();
        if (f.ShowDialog(this) != DialogResult.OK)
            return;

        var bytes = f.CapturedPngBytes;
        if (bytes is null || bytes.Length == 0)
            return;

        _template = new ImageTemplate
        {
            Kind = ImageTemplateKind.EmbeddedPng,
            PngBytes = bytes,
            FilePath = string.Empty
        };

        LoadPreviewFromTemplate();
    }

    private void OpenTemplate()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
            Title = "Open image"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        _template = new ImageTemplate
        {
            Kind = ImageTemplateKind.FilePath,
            FilePath = ofd.FileName,
            PngBytes = Array.Empty<byte>()
        };

        LoadPreviewFromTemplate();
    }

    private void ClearTemplate()
    {
        _template = new ImageTemplate();
        LoadPreviewFromTemplate();
    }

    private void LoadPreviewFromTemplate()
    {
        _preview?.Dispose();
        _preview = null;

        try
        {
            if (_template.Kind == ImageTemplateKind.EmbeddedPng && _template.PngBytes is { Length: > 0 })
            {
                _preview = Image.FromStream(new MemoryStream(_template.PngBytes));
            }
            else if (_template.Kind == ImageTemplateKind.FilePath && !string.IsNullOrWhiteSpace(_template.FilePath) && File.Exists(_template.FilePath))
            {
                _preview = Image.FromFile(_template.FilePath);
            }
        }
        catch
        {
            _preview?.Dispose();
            _preview = null;
        }

        _picTemplate.Image = _preview;
    }

    private void DefineArea()
    {
        using var f = new ScreenRegionCaptureForm();
        if (f.ShowDialog(this) != DialogResult.OK)
            return;

        var r = f.CapturedScreenRectangle;
        if (r.Width <= 0 || r.Height <= 0)
            return;

        _definedScreenRect = r;

        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        if (sel == "Area of desktop")
        {
            _area = new SearchArea
            {
                Kind = SearchAreaKind.AreaOfDesktop,
                X1 = r.Left,
                Y1 = r.Top,
                X2 = r.Right,
                Y2 = r.Bottom
            };
        }
        else
        {
            // focused window 基準に相対化できれば相対化
            var center = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
            if (TryGetWindowRectFromPoint(center, out var win))
            {
                _area = new SearchArea
                {
                    Kind = SearchAreaKind.AreaOfFocusedWindow,
                    X1 = r.Left - win.Left,
                    Y1 = r.Top - win.Top,
                    X2 = r.Right - win.Left,
                    Y2 = r.Bottom - win.Top
                };
            }
            else
            {
                _area = new SearchArea
                {
                    Kind = SearchAreaKind.AreaOfFocusedWindow,
                    X1 = r.Left,
                    Y1 = r.Top,
                    X2 = r.Right,
                    Y2 = r.Bottom
                };
            }
        }

        UpdateAreaButtons();
    }

    private void ConfirmArea()
    {
        if (!IsAreaSelection())
            return;

        var rect = _definedScreenRect != Rectangle.Empty
            ? _definedScreenRect
            : DetectionTestUtil.ResolveSearchRectangle(_area);

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var wasVisible = Visible;
        try
        {
            Hide();
            AreaPreviewForm.ShowPreview(this, rect);
        }
        finally
        {
            SafeRestoreVisibility(wasVisible);
        }
    }

    private async Task TestAsync()
    {
        if (_testing) return;

        bool hasTemplate = _template.Kind switch
        {
            ImageTemplateKind.EmbeddedPng => _template.PngBytes is { Length: > 0 },
            ImageTemplateKind.FilePath => !string.IsNullOrWhiteSpace(_template.FilePath),
            _ => false
        };

        if (!hasTemplate)
        {
            SafeMessage("Template image is not set.", MessageBoxIcon.Warning);
            return;
        }

        _testing = true;
        _testCts?.Cancel();
        _testCts?.Dispose();
        _testCts = new CancellationTokenSource();

        SetTestingUi(true);

        var wasVisible = Visible;
        try
        {
            UseWaitCursor = true;

            // 自分が写り込むのを避ける
            Hide();
            await Task.Delay(150, _testCts.Token);

            if (IsDisposed || Disposing) return;

            var rect = IsAreaSelection()
                ? (_definedScreenRect != Rectangle.Empty ? _definedScreenRect : DetectionTestUtil.ResolveSearchRectangle(_area))
                : DetectionTestUtil.ResolveSearchRectangle(_area);

            var action = BuildResult();
            var testAction = action with
            {
                MouseActionEnabled = false,
                SaveCoordinateEnabled = false
            };

            var (success, pt, _) = await DetectionTestUtil.TestFindImageAsync(testAction, rect, _testCts.Token);

            if (IsDisposed || Disposing) return;

            SafeRestoreVisibility(wasVisible);

            SafeMessage(success && pt is not null
                    ? $"Found at ({pt.Value.X}, {pt.Value.Y})."
                    : "Not found.",
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            // closing / canceled
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing) return;
            SafeRestoreVisibility(wasVisible);
            SafeMessage(ex.Message, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                UseWaitCursor = false;
                SetTestingUi(false);
                _testing = false;
                UpdateAreaButtons();
            }
        }
    }

    private void SetTestingUi(bool testing)
    {
        if (!testing)
        {
            ControlBox = _savedControlBox;
        }
        else
        {
            _savedControlBox = ControlBox;
            ControlBox = false;
        }

        _btnOk.Enabled = !testing;
        _btnCancel.Enabled = !testing;

        _btnTest.Enabled = !testing;

        _btnCapture.Enabled = !testing;
        _btnOpen.Enabled = !testing;
        _btnClear.Enabled = !testing;

        _cmbArea.Enabled = !testing;
        _numTolerance.Enabled = !testing;

        _chkMouseAction.Enabled = !testing;
        _cmbMouseAction.Enabled = !testing && _chkMouseAction.Checked;
        _cmbMousePos.Enabled = !testing && _chkMouseAction.Checked;

        _chkSaveCoord.Enabled = !testing;
        _txtSaveX.Enabled = !testing && _chkSaveCoord.Checked;
        _txtSaveY.Enabled = !testing && _chkSaveCoord.Checked;

        _cmbTrueGoTo.Enabled = !testing;

        _numTimeoutSec.Enabled = !testing;
        _cmbFalseGoTo.Enabled = !testing;

        UpdateAreaButtons();
    }

    private void SafeRestoreVisibility(bool wasVisible)
    {
        if (!wasVisible) return;
        if (IsDisposed || Disposing) return;

        if (!Visible) Show();
        Activate();
    }

    private void SafeMessage(string msg, MessageBoxIcon icon)
    {
        if (IsDisposed || Disposing) return;

        if (IsHandleCreated)
            MessageBox.Show(this, msg, "Test", MessageBoxButtons.OK, icon);
        else
            MessageBox.Show(msg, "Test", MessageBoxButtons.OK, icon);
    }

    // --- result mapping ---
    private FindImageAction BuildResult()
    {
        return new FindImageAction
        {
            Template = _template,

            SearchArea = _area,
            Area = _area,

            ColorTolerancePercent = (int)_numTolerance.Value,

            MouseActionEnabled = _chkMouseAction.Checked,
            MouseAction = ParseMouseAction(_cmbMouseAction.SelectedItem?.ToString()),
            MousePosition = ParseMousePos(_cmbMousePos.SelectedItem?.ToString()),

            SaveCoordinateEnabled = _chkSaveCoord.Checked,
            SaveXVariable = _txtSaveX.Text,
            SaveYVariable = _txtSaveY.Text,

            TrueGoTo = ParseGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),

            TimeoutMs = (int)_numTimeoutSec.Value <= 0 ? 0 : (int)_numTimeoutSec.Value * 1000,
            FalseGoTo = ParseGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
        };
    }

    private static FindImageAction CreateDefault()
        => new()
        {
            Template = new ImageTemplate(),
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            ColorTolerancePercent = 0,

            MouseActionEnabled = true,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = DomainMousePosition.Center,

            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",

            TrueGoTo = GoToTarget.Next(),
            FalseGoTo = GoToTarget.End(),

            TimeoutMs = 120000
        };

    // --- combos ---
    private static ComboBox CreateGoToCombo()
    {
        var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        cmb.Items.AddRange(new object[] { "Next", "End", "Label..." });
        cmb.SelectedItem = "Next";
        return cmb;
    }

    private void OnGoToSelected(ComboBox cmb)
    {
        if (cmb.SelectedItem?.ToString() != "Label...")
            return;

        var label = SimpleTextPrompt.Show(this, title: "Go to label", message: "Enter label:");
        if (string.IsNullOrWhiteSpace(label))
        {
            cmb.SelectedItem = "Next";
            return;
        }

        var text = $"Label:{label}";
        if (!cmb.Items.Contains(text))
            cmb.Items.Insert(2, text);

        cmb.SelectedItem = text;
    }

    private static void SetGoToSelection(ComboBox cmb, GoToTarget target)
    {
        var text = ToGoToText(target);
        if (!cmb.Items.Contains(text) && text.StartsWith("Label:", StringComparison.Ordinal))
            cmb.Items.Insert(2, text);
        cmb.SelectedItem = text;
    }

    private static string ToGoToText(GoToTarget t)
        => t.Kind switch
        {
            GoToKind.End => "End",
            GoToKind.Label => $"Label:{t.Label}",
            _ => "Next"
        };

    private static GoToTarget ParseGoToText(string? text)
    {
        text ??= "Next";
        if (text == "End") return GoToTarget.End();
        if (text.StartsWith("Label:", StringComparison.Ordinal))
            return GoToTarget.ToLabel(text["Label:".Length..]);
        return GoToTarget.Next();
    }

    private static string ToAreaText(SearchArea a)
        => a.Kind switch
        {
            SearchAreaKind.FocusedWindow => "Focused window",
            SearchAreaKind.AreaOfDesktop => "Area of desktop",
            SearchAreaKind.AreaOfFocusedWindow => "Area of focused window",
            _ => "Entire desktop"
        };

    private static MouseActionBehavior ParseMouseAction(string? text)
        => text switch
        {
            nameof(MouseActionBehavior.LeftClick) => MouseActionBehavior.LeftClick,
            nameof(MouseActionBehavior.RightClick) => MouseActionBehavior.RightClick,
            nameof(MouseActionBehavior.MiddleClick) => MouseActionBehavior.MiddleClick,
            nameof(MouseActionBehavior.DoubleClick) => MouseActionBehavior.DoubleClick,
            _ => MouseActionBehavior.Positioning
        };

    private static string ToMousePosText(DomainMousePosition pos)
        => pos switch
        {
            DomainMousePosition.TopLeft => "Top-left",
            DomainMousePosition.TopRight => "Top-right",
            DomainMousePosition.BottomLeft => "Bottom-left",
            DomainMousePosition.BottomRight => "Bottom-right",
            _ => "Centered"
        };

    private static DomainMousePosition ParseMousePos(string? text)
        => text switch
        {
            "Top-left" => DomainMousePosition.TopLeft,
            "Top-right" => DomainMousePosition.TopRight,
            "Bottom-left" => DomainMousePosition.BottomLeft,
            "Bottom-right" => DomainMousePosition.BottomRight,
            _ => DomainMousePosition.Center
        };

    // --- P/Invoke: focused window rect ---
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point pt);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    private static bool TryGetWindowRectFromPoint(Point pt, out Rectangle rect)
    {
        rect = default;
        var h = WindowFromPoint(pt);
        if (h == IntPtr.Zero) return false;
        if (!GetWindowRect(h, out var r)) return false;
        rect = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return rect.Width > 0 && rect.Height > 0;
    }
}
