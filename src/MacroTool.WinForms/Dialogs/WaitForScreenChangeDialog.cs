using System.Drawing;
using System.Runtime.InteropServices;
using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Wait for changes on screen（2-6-3）設定ダイアログ。
/// UI仕様: docs/images/2-6-3_WaitForScreenChange.png（Help ボタンは不要）
/// </summary>
public sealed class WaitForScreenChangeDialog : Form
{
    private readonly ComboBox _cmbArea;
    private readonly CheckBox _chkMouseAction;
    private readonly ComboBox _cmbMouseAction;
    private readonly CheckBox _chkSaveCoord;
    private readonly ComboBox _cmbSaveX;
    private readonly ComboBox _cmbSaveY;
    private readonly ComboBox _cmbTrueGoTo;
    private readonly NumericUpDown _numTimeoutSec;
    private readonly ComboBox _cmbFalseGoTo;

    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };

    public WaitForScreenChangeAction Result { get; private set; } = new();

    private WaitForScreenChangeDialog(WaitForScreenChangeAction? initial)
    {
        Text = "Wait for changes on screen";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        Width = 560;
        Height = 560;

        var lblIntro = new Label
        {
            Text = "This action waits for changes in the specified screen region.",
            AutoSize = true,
            Dock = DockStyle.Top
        };

        // === Define search area ===
        var grpArea = new GroupBox { Text = "Define search area", Dock = DockStyle.Top, Height = 80 };
        var tblArea = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10, 10, 10, 10) };
        tblArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblArea.Controls.Add(new Label { Text = "Search area", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
        _cmbArea.Items.AddRange(new object[]
        {
            "Entire desktop",
            "Area of desktop...",
            "Focused window",
            "Area of focused window..."
        });
        _cmbArea.SelectedIndex = 0;
        tblArea.Controls.Add(_cmbArea, 1, 0);
        grpArea.Controls.Add(tblArea);

        // === If change found ===
        var grpTrue = new GroupBox { Text = "If change is found", Dock = DockStyle.Top, Height = 170 };
        var tblTrue = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(10, 10, 10, 10) };
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        _chkMouseAction = new CheckBox { Text = "Mouse action", AutoSize = true };
        _cmbMouseAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        _cmbMouseAction.Items.AddRange(new object[] { "Positioning", "LeftClick", "RightClick", "MiddleClick", "DoubleClick" });
        _cmbMouseAction.SelectedIndex = 0;
        tblTrue.Controls.Add(_chkMouseAction, 0, 0);
        tblTrue.Controls.Add(_cmbMouseAction, 1, 0);

        _chkSaveCoord = new CheckBox { Text = "Save X coordinate to", AutoSize = true };
        _cmbSaveX = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120, Text = "X" };
        tblTrue.Controls.Add(_chkSaveCoord, 0, 1);
        tblTrue.Controls.Add(_cmbSaveX, 1, 1);

        tblTrue.Controls.Add(new Label { Text = "Y to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 2, 1);
        _cmbSaveY = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120, Text = "Y" };
        tblTrue.Controls.Add(_cmbSaveY, 3, 1);

        tblTrue.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 3);
        _cmbTrueGoTo = CreateGoToCombo();
        tblTrue.Controls.Add(_cmbTrueGoTo, 1, 3);
        tblTrue.SetColumnSpan(_cmbTrueGoTo, 3);

        grpTrue.Controls.Add(tblTrue);

        // === If no change found ===
        var grpFalse = new GroupBox { Text = "If no change is found", Dock = DockStyle.Top, Height = 110 };
        var tblFalse = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(10, 10, 10, 10) };
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblFalse.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblFalse.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        tblFalse.Controls.Add(new Label { Text = "Continue waiting", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _numTimeoutSec = new NumericUpDown { Minimum = 0, Maximum = 86400, Width = 80, Value = 120 };
        tblFalse.Controls.Add(_numTimeoutSec, 1, 0);
        tblFalse.Controls.Add(new Label { Text = "seconds and then", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 2, 0);

        tblFalse.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 1);
        _cmbFalseGoTo = CreateGoToCombo();
        tblFalse.Controls.Add(_cmbFalseGoTo, 1, 1);
        tblFalse.SetColumnSpan(_cmbFalseGoTo, 3);

        grpFalse.Controls.Add(tblFalse);

        // buttons
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100 };
        AcceptButton = ok;
        CancelButton = cancel;
        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 10, 10),
            Height = 54
        };
        pnlButtons.Controls.Add(ok);
        pnlButtons.Controls.Add(cancel);

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        root.Controls.Add(grpFalse);
        root.Controls.Add(grpTrue);
        root.Controls.Add(grpArea);
        root.Controls.Add(lblIntro);
        Controls.Add(root);
        Controls.Add(pnlButtons);

        // initial
        var init = initial ?? CreateDefault();
        _area = init.SearchArea ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };
        _cmbArea.SelectedItem = ToAreaText(_area);
        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _cmbSaveX.Text = init.SaveXVariable ?? "X";
        _cmbSaveY.Text = init.SaveYVariable ?? "Y";
        _cmbTrueGoTo.SelectedItem = ToGoToText(init.TrueGoTo);
        _cmbFalseGoTo.SelectedItem = ToGoToText(init.FalseGoTo);
        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        ApplyEnableState();

        _chkMouseAction.CheckedChanged += (_, __) => ApplyEnableState();
        _chkSaveCoord.CheckedChanged += (_, __) => ApplyEnableState();

        _cmbArea.SelectedIndexChanged += (_, __) =>
        {
            var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
            if (sel is "Area of desktop..." or "Area of focused window...")
            {
                using var cap = new ScreenRegionCaptureForm();
                if (cap.ShowDialog(this) != DialogResult.OK || cap.CapturedScreenRectangle == Rectangle.Empty)
                {
                    // revert
                    _cmbArea.SelectedItem = ToAreaText(_area);
                    return;
                }

                var r = cap.CapturedScreenRectangle;
                if (sel == "Area of desktop...")
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
                    // できるだけ UI 直感に合わせて「選択した領域が属するウィンドウ」から相対化する
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
                        // fallback: 絶対座標として保持（再生時にウィンドウ基準に換算されるため、使用には注意）
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
            }
            else
            {
                _area = sel == "Focused window" ? new SearchArea { Kind = SearchAreaKind.FocusedWindow } : new SearchArea { Kind = SearchAreaKind.EntireDesktop };
            }
        };

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;
            Result = BuildResult();
        };
    }

    private void ApplyEnableState()
    {
        _cmbMouseAction.Enabled = _chkMouseAction.Checked;
        _cmbSaveX.Enabled = _chkSaveCoord.Checked;
        _cmbSaveY.Enabled = _chkSaveCoord.Checked;
    }

    private WaitForScreenChangeAction BuildResult()
    {
        var action = new WaitForScreenChangeAction
        {
            SearchArea = _area,
            Area = _area,
            MouseActionEnabled = _chkMouseAction.Checked,
            MouseAction = Enum.TryParse<MouseActionBehavior>(_cmbMouseAction.SelectedItem?.ToString(), out var m) ? m : MouseActionBehavior.Positioning,
            SaveCoordinateEnabled = _chkSaveCoord.Checked,
            SaveXVariable = _cmbSaveX.Text.Trim(),
            SaveYVariable = _cmbSaveY.Text.Trim(),
            TrueGoTo = FromGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),
            FalseGoTo = FromGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
            TimeoutMs = (int)_numTimeoutSec.Value * 1000
        };
        return action;
    }

    private static WaitForScreenChangeAction CreateDefault()
        => new()
        {
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            MouseActionEnabled = false,
            MouseAction = MouseActionBehavior.Positioning,
            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",
            TrueGoTo = GoToTarget.Next(),
            FalseGoTo = GoToTarget.End(),
            TimeoutMs = 120_000
        };

    private static ComboBox CreateGoToCombo()
    {
        var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        cmb.Items.AddRange(new object[] { "Next", "End", "Start" });
        cmb.SelectedIndex = 0;
        return cmb;
    }

    private static string ToGoToText(GoToTarget t)
        => t.Kind switch
        {
            GoToKind.End => "End",
            GoToKind.Start => "Start",
            _ => "Next"
        };

    private static GoToTarget FromGoToText(string? text)
        => text switch
        {
            "End" => GoToTarget.End(),
            "Start" => GoToTarget.Start(),
            _ => GoToTarget.Next()
        };

    private static string ToAreaText(SearchArea a)
        => a.Kind switch
        {
            SearchAreaKind.FocusedWindow => "Focused window",
            SearchAreaKind.AreaOfDesktop => "Area of desktop...",
            SearchAreaKind.AreaOfFocusedWindow => "Area of focused window...",
            _ => "Entire desktop"
        };

    // ===== Win32: window rect from point =====
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    private static bool TryGetWindowRectFromPoint(Point screenPoint, out Rectangle rect)
    {
        rect = Rectangle.Empty;
        var hwnd = WindowFromPoint(new POINT(screenPoint.X, screenPoint.Y));
        if (hwnd == IntPtr.Zero) return false;
        if (!GetWindowRect(hwnd, out var r)) return false;
        rect = new Rectangle(r.Left, r.Top, Math.Max(0, r.Right - r.Left), Math.Max(0, r.Bottom - r.Top));
        return rect.Width > 0 && rect.Height > 0;
    }

    public static WaitForScreenChangeAction? Show(IWin32Window owner, WaitForScreenChangeAction? initial)
    {
        using var dlg = new WaitForScreenChangeDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
