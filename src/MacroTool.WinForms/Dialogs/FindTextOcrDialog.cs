using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacroTool.Domain.Macros;

// WinForms の Control.MousePosition（Point）と、ドメイン enum の MousePosition が衝突するため別名を付ける
using DomainMousePosition = MacroTool.Domain.Macros.MousePosition;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Find text (OCR)（2-7-2）設定ダイアログ。
/// UI仕様の見た目に寄せつつ、
/// - マクロ追加前の Test
/// - Area of desktop / Area of focused window の Define / Confirm Area
/// を追加。
/// </summary>
public sealed class FindTextOcrDialog : Form
{
    private readonly TextBox _txtText;
    private readonly ComboBox _cmbLang;

    private readonly ComboBox _cmbArea;
    private readonly Button _btnDefineArea;
    private readonly Button _btnConfirmArea;
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

    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };
    private Rectangle _definedScreenRect = Rectangle.Empty;

    // Test 実行中ガード
    private CancellationTokenSource? _testCts;
    private bool _testing;
    private bool _savedControlBox;

    public FindTextOcrAction Result { get; private set; } = new();

    public static FindTextOcrAction? Show(IWin32Window owner, FindTextOcrAction? initial)
    {
        using var dlg = new FindTextOcrDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }

    private FindTextOcrDialog(FindTextOcrAction? initial)
    {
        Text = "Find text (OCR)";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;

        // 右側入力欄が潰れないように横幅を確保
        ClientSize = new Size(660, 560);
        MinimumSize = new Size(660, 560);

        FormClosing += (_, __) => _testCts?.Cancel();

        // --- Group: Text to search ---
        var grpSpec = new GroupBox
        {
            Text = "Text to search",
            Dock = DockStyle.Top,
            Height = 185,
            Padding = new Padding(10)
        };

        var tblSpec = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        tblSpec.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // left: multiline text
        var left = new Panel { Dock = DockStyle.Fill };
        _txtText = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill
        };
        left.Controls.Add(_txtText);

        // right: language + area + test
        // 4列(AutoSize)だとボタン幅が優先され、ComboBox が 0px になり得るため
        // 3列 + FlowLayoutPanel で確実に入力欄の幅を確保する。
        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        // Search area 行は Define/Confirm を縦に置くため高さを確保
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        right.Controls.Add(new Label { Text = "Language:", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 0);
        _cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
        _cmbLang.Items.AddRange(new object[] { nameof(OcrLanguage.English), nameof(OcrLanguage.Japanese) });
        right.Controls.Add(_cmbLang, 1, 0);
        right.SetColumnSpan(_cmbLang, 2);

        right.Controls.Add(new Label { Text = "Search area:", AutoSize = true, Margin = new Padding(0, 9, 0, 0) }, 0, 1);

        _cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, Margin = new Padding(0, 6, 0, 0) };
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });

        // Define/Confirm は常に見せる（非Area選択時は無効化）
        _btnDefineArea = new Button { Text = "Define...", Width = 78, Height = 26 };
        _btnConfirmArea = new Button { Text = "Confirm Area", Width = 100, Height = 26 };

        var pnlAreaButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0)
        };
        _btnDefineArea.Width = 110;
        _btnConfirmArea.Width = 110;
        pnlAreaButtons.Controls.Add(_btnDefineArea);
        pnlAreaButtons.Controls.Add(_btnConfirmArea);

        right.Controls.Add(_cmbArea, 1, 1);
        right.Controls.Add(pnlAreaButtons, 2, 1);

        _btnTest = new Button { Text = "Test", Width = 80, Height = 26 };
        right.Controls.Add(_btnTest, 2, 2);

        tblSpec.Controls.Add(left, 0, 0);
        tblSpec.Controls.Add(right, 1, 0);
        grpSpec.Controls.Add(tblSpec);

        // --- Group: If text is found ---
        var grpFound = new GroupBox
        {
            Text = "If text is found",
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

        // --- Group: If text is not found ---
        var grpNotFound = new GroupBox
        {
            Text = "If text is not found",
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

        _txtText.Text = init.TextToSearchFor ?? string.Empty;
        _cmbLang.SelectedItem = init.Language.ToString();

        _area = init.SearchArea ?? init.Area ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };
        _cmbArea.SelectedItem = ToAreaText(_area);

        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _cmbMousePos.SelectedItem = ToMousePosText(init.MousePosition);

        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _txtSaveX.Text = init.SaveXVariable ?? "X";
        _txtSaveY.Text = init.SaveYVariable ?? "Y";

        SetGoToSelection(_cmbTrueGoTo, init.TrueGoTo);
        SetGoToSelection(_cmbFalseGoTo, init.FalseGoTo);

        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

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

        _btnDefineArea.Click += (_, __) => DefineArea();
        _btnConfirmArea.Click += (_, __) => ConfirmArea();
        _btnTest.Click += async (_, __) => await TestAsync();

        _btnOk.Click += (_, __) => Result = BuildResult();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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

        if (string.IsNullOrWhiteSpace(_txtText.Text))
        {
            SafeMessage("Text is empty.", MessageBoxIcon.Warning);
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

            var (success, pt, _) = await DetectionTestUtil.TestFindTextOcrAsync(testAction, rect, _testCts.Token);

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
        _txtText.Enabled = !testing;
        _cmbLang.Enabled = !testing;
        _cmbArea.Enabled = !testing;

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
    private FindTextOcrAction BuildResult()
    {
        return new FindTextOcrAction
        {
            TextToSearchFor = _txtText.Text,
            Language = ParseLanguage(_cmbLang.SelectedItem?.ToString()),

            SearchArea = _area,
            Area = _area,

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

    private static FindTextOcrAction CreateDefault()
        => new()
        {
            TextToSearchFor = string.Empty,
            Language = OcrLanguage.English,
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },

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

    private static OcrLanguage ParseLanguage(string? text)
        => text switch
        {
            nameof(OcrLanguage.Japanese) => OcrLanguage.Japanese,
            _ => OcrLanguage.English
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
