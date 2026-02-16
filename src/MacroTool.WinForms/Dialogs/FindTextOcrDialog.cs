using System.Runtime.InteropServices;
using MacroTool.Domain.Macros;

// WinForms の Control.MousePosition（Point）と、ドメイン enum の MousePosition が衝突するため別名を付ける
using DomainMousePosition = MacroTool.Domain.Macros.MousePosition;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Find text (OCR)（2-7-2）設定ダイアログ。
/// UI仕様: docs/images/2-7-2_FindText.png（Help ボタンは不要）
/// </summary>
public sealed class FindTextOcrDialog : Form
{
    private readonly TextBox _txtText;
    private readonly CheckBox _chkRegex;
    private readonly ComboBox _cmbLang;
    private readonly ComboBox _cmbArea;
    private readonly Button _btnDefineArea;
    private readonly Button _btnConfirmArea;
    private readonly Button _btnTest;

    private readonly CheckBox _chkMouseAction;
    private readonly ComboBox _cmbMouseAction;
    private readonly ComboBox _cmbMousePos;
    private readonly CheckBox _chkSaveCoord;
    private readonly ComboBox _cmbSaveX;
    private readonly ComboBox _cmbSaveY;
    private readonly ComboBox _cmbTrueGoTo;
    private readonly NumericUpDown _numTimeoutSec;
    private readonly ComboBox _cmbFalseGoTo;

    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };
    private Rectangle _definedScreenRect = Rectangle.Empty;

    public FindTextOcrAction Result { get; private set; } = new();

    private FindTextOcrDialog(FindTextOcrAction? initial)
    {
        Text = "Find text (OCR)";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        Width = 520;
        Height = 620;

        // === Group: Text to search for ===
        var grpText = new GroupBox { Text = "Text to search for", Dock = DockStyle.Top, Height = 210 };
        var tblText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10, 10, 10, 10)
        };
        tblText.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        tblText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblText.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblText.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblText.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblText.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblText.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        tblText.Controls.Add(new Label { Text = "Text:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _txtText = new TextBox { Dock = DockStyle.Fill };
        tblText.Controls.Add(_txtText, 1, 0);

        _chkRegex = new CheckBox { Text = "Treat as regular expression", AutoSize = true, Enabled = false };
        tblText.Controls.Add(_chkRegex, 1, 1);

        tblText.Controls.Add(new Label { Text = "Language:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 2);
        _cmbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        _cmbLang.Items.AddRange(new object[] { "English", "Japanese" });
        tblText.Controls.Add(_cmbLang, 1, 2);

        tblText.Controls.Add(new Label { Text = "Search area:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 3);
        _cmbArea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbArea.Items.AddRange(new object[] { "Entire desktop", "Focused window", "Area of desktop", "Area of focused window" });
        _btnDefineArea = new Button { Text = "Define", Width = 70, Height = 24 };
        _btnConfirmArea = new Button { Text = "Confirm Area", Width = 100, Height = 24 };
        var pnlArea = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        pnlArea.Controls.Add(_cmbArea);
        pnlArea.Controls.Add(_btnDefineArea);
        pnlArea.Controls.Add(_btnConfirmArea);
        tblText.Controls.Add(pnlArea, 1, 3);

        // Test
        _btnTest = new Button { Text = "Test", Width = 90, Height = 24 };
        tblText.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 4);
        tblText.Controls.Add(_btnTest, 1, 4);

        grpText.Controls.Add(tblText);

        // === Group: Optimize recognition for (UI only / future options) ===
        var grpOpt = new GroupBox { Text = "Optimize recognition for", Dock = DockStyle.Top, Height = 125 };
        var flpOpt = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10), AutoScroll = true };
        flpOpt.Controls.Add(new CheckBox { Text = "a single line of text", AutoSize = true, Enabled = false });
        flpOpt.Controls.Add(new CheckBox { Text = "a single word", AutoSize = true, Enabled = false });
        flpOpt.Controls.Add(new CheckBox { Text = "a single character", AutoSize = true, Enabled = false });
        flpOpt.Controls.Add(new CheckBox { Text = "a number", AutoSize = true, Enabled = false });
        flpOpt.Controls.Add(new CheckBox { Text = "some other purpose", AutoSize = true, Enabled = false });
        grpOpt.Controls.Add(flpOpt);

        // === Group: If text is found ===
        var grpFound = new GroupBox { Text = "If text is found", Dock = DockStyle.Top, Height = 145 };
        var tblFound = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(10, 10, 10, 10)
        };
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tblFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _chkMouseAction = new CheckBox { Text = "Mouse action:", AutoSize = true };
        _cmbMouseAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _cmbMouseAction.Items.AddRange(new object[] { "Positioning", "LeftClick", "RightClick", "MiddleClick", "DoubleClick" });
        _cmbMousePos = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _cmbMousePos.Items.AddRange(new object[] { "Centered", "Top-left", "Top-right", "Bottom-left", "Bottom-right" });

        _chkSaveCoord = new CheckBox { Text = "Save X to:", AutoSize = true };
        _cmbSaveX = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };
        var lblAndY = new Label { Text = "and Y to:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        _cmbSaveY = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };

        var lblGoTo = new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        _cmbTrueGoTo = CreateGoToCombo();

        tblFound.Controls.Add(_chkMouseAction, 0, 0);
        tblFound.Controls.Add(_cmbMouseAction, 1, 0);
        tblFound.Controls.Add(_cmbMousePos, 2, 0);
        tblFound.SetColumnSpan(_cmbMousePos, 2);

        tblFound.Controls.Add(_chkSaveCoord, 0, 1);
        tblFound.Controls.Add(_cmbSaveX, 1, 1);
        tblFound.Controls.Add(lblAndY, 2, 1);
        tblFound.Controls.Add(_cmbSaveY, 3, 1);

        tblFound.Controls.Add(lblGoTo, 0, 2);
        tblFound.Controls.Add(_cmbTrueGoTo, 1, 2);
        tblFound.SetColumnSpan(_cmbTrueGoTo, 3);

        grpFound.Controls.Add(tblFound);

        // === Group: If text is not found ===
        var grpNotFound = new GroupBox { Text = "If text is not found", Dock = DockStyle.Top, Height = 110 };
        var tblNotFound = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(10, 10, 10, 10)
        };
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblNotFound.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblNotFound.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        tblNotFound.Controls.Add(new Label { Text = "Continue waiting", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _numTimeoutSec = new NumericUpDown { Minimum = 0, Maximum = 86400, Width = 80 };
        tblNotFound.Controls.Add(_numTimeoutSec, 1, 0);
        tblNotFound.Controls.Add(new Label { Text = "seconds and then", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 2, 0);

        tblNotFound.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 1);
        _cmbFalseGoTo = CreateGoToCombo();
        tblNotFound.Controls.Add(_cmbFalseGoTo, 1, 1);
        tblNotFound.SetColumnSpan(_cmbFalseGoTo, 3);

        grpNotFound.Controls.Add(tblNotFound);

        // buttons
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };
        pnlBottom.Controls.Add(btnOk);
        pnlBottom.Controls.Add(btnCancel);

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        root.Controls.Add(grpNotFound);
        root.Controls.Add(grpFound);
        root.Controls.Add(grpOpt);
        root.Controls.Add(grpText);

        Controls.Add(root);
        Controls.Add(pnlBottom);

        // initial
        var init = initial ?? CreateDefault();
        _area = init.SearchArea ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };

        _txtText.Text = init.TextToSearchFor ?? string.Empty;
        _cmbLang.SelectedItem = init.Language == OcrLanguage.Japanese ? "Japanese" : "English";
        _cmbArea.SelectedItem = ToAreaText(_area);

        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _cmbMousePos.SelectedItem = ToMousePosText(init.MousePosition);
        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _cmbSaveX.Text = init.SaveXVariable ?? "X";
        _cmbSaveY.Text = init.SaveYVariable ?? "Y";
        SetGoToSelection(_cmbTrueGoTo, init.TrueGoTo);
        SetGoToSelection(_cmbFalseGoTo, init.FalseGoTo);
        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        ApplyEnableState();
        UpdateAreaButtons();

        // events
        _chkMouseAction.CheckedChanged += (_, __) => ApplyEnableState();
        _chkSaveCoord.CheckedChanged += (_, __) => ApplyEnableState();
        _cmbArea.SelectedIndexChanged += (_, __) => { OnAreaSelectionChanged(); UpdateAreaButtons(); };
        _cmbTrueGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbTrueGoTo);
        _cmbFalseGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbFalseGoTo);

        _btnDefineArea.Click += (_, __) => DefineArea();
        _btnConfirmArea.Click += (_, __) => ConfirmArea();
        _btnTest.Click += async (_, __) => await TestAsync();

        btnOk.Click += (_, __) =>
        {
            Result = BuildResult();
        };
    }

    private void ApplyEnableState()
    {
        _cmbMouseAction.Enabled = _chkMouseAction.Checked;
        _cmbMousePos.Enabled = _chkMouseAction.Checked;
        _cmbSaveX.Enabled = _chkSaveCoord.Checked;
        _cmbSaveY.Enabled = _chkSaveCoord.Checked;
    }

    private void OnAreaSelectionChanged()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        _area = sel switch
        {
            "Focused window" => new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            "Area of desktop" => _area.Kind == SearchAreaKind.AreaOfDesktop ? _area : new SearchArea { Kind = SearchAreaKind.AreaOfDesktop },
            "Area of focused window" => _area.Kind == SearchAreaKind.AreaOfFocusedWindow ? _area : new SearchArea { Kind = SearchAreaKind.AreaOfFocusedWindow },
            _ => new SearchArea { Kind = SearchAreaKind.EntireDesktop }
        };
    }

    private void UpdateAreaButtons()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        bool isArea = sel is "Area of desktop" or "Area of focused window";
        _btnDefineArea.Enabled = isArea;

        bool hasRect = _definedScreenRect != Rectangle.Empty || _area.Kind switch
        {
            SearchAreaKind.AreaOfDesktop => (Math.Abs(_area.X2 - _area.X1) > 0) && (Math.Abs(_area.Y2 - _area.Y1) > 0),
            SearchAreaKind.AreaOfFocusedWindow => (Math.Abs(_area.X2 - _area.X1) > 0) && (Math.Abs(_area.Y2 - _area.Y1) > 0),
            _ => false
        };
        _btnConfirmArea.Enabled = isArea && hasRect;
    }

    private void DefineArea()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        if (sel is not ("Area of desktop" or "Area of focused window"))
            return;

        using var cap = new ScreenRegionCaptureForm();
        if (cap.ShowDialog(this) != DialogResult.OK || cap.CapturedScreenRectangle == Rectangle.Empty)
            return;

        var r = cap.CapturedScreenRectangle;
        _definedScreenRect = r;
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
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        if (sel is not ("Area of desktop" or "Area of focused window"))
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
            if (wasVisible) { Show(); Activate(); }
        }
    }

    private async Task TestAsync()
    {
        var action = BuildResult();
        var testAction = action with
        {
            MouseActionEnabled = false,
            SaveCoordinateEnabled = false
        };

        var wasVisible = Visible;

        try
        {
            UseWaitCursor = true;
            _btnTest.Enabled = false;

            Hide();
            await Task.Delay(150);

            var rect = (_cmbArea.SelectedItem?.ToString() ?? "") is "Area of desktop" or "Area of focused window"
                ? (_definedScreenRect != Rectangle.Empty ? _definedScreenRect : DetectionTestUtil.ResolveSearchRectangle(_area))
                : DetectionTestUtil.ResolveSearchRectangle(_area);

            var (success, pt, _) = await DetectionTestUtil.TestFindTextOcrAsync(testAction, rect, CancellationToken.None);

            if (wasVisible) { Show(); Activate(); }
            if (success && pt is not null)
            {
                MessageBox.Show(this, $"Found at ({pt.Value.X}, {pt.Value.Y}).", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Not found.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            if (wasVisible && !Visible) { Show(); Activate(); }
            MessageBox.Show(this, ex.Message, "Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (wasVisible && !Visible) { Show(); Activate(); }
            _btnTest.Enabled = true;
            UseWaitCursor = false;
        }
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
            cmb.Items.Insert(3, text);
        cmb.SelectedItem = text;
    }

    private FindTextOcrAction BuildResult()
    {
        var lang = _cmbLang.SelectedItem?.ToString() == "Japanese" ? OcrLanguage.Japanese : OcrLanguage.English;
        return new FindTextOcrAction
        {
            TextToSearchFor = _txtText.Text ?? string.Empty,
            Language = lang,
            SearchArea = _area,
            Area = _area,
            MouseActionEnabled = _chkMouseAction.Checked,
            MouseAction = ParseMouseAction(_cmbMouseAction.SelectedItem?.ToString()),
            MousePosition = ParseMousePos(_cmbMousePos.SelectedItem?.ToString()),
            SaveCoordinateEnabled = _chkSaveCoord.Checked,
            SaveXVariable = string.IsNullOrWhiteSpace(_cmbSaveX.Text) ? "X" : _cmbSaveX.Text.Trim(),
            SaveYVariable = string.IsNullOrWhiteSpace(_cmbSaveY.Text) ? "Y" : _cmbSaveY.Text.Trim(),
            TrueGoTo = FromGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),
            FalseGoTo = FromGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
            TimeoutMs = (int)_numTimeoutSec.Value * 1000
        };
    }

    private static FindTextOcrAction CreateDefault()
        => new()
        {
            TextToSearchFor = "",
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
            TimeoutMs = 120_000
        };

    private static ComboBox CreateGoToCombo()
    {
        var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        cmb.Items.AddRange(new object[] { "Next", "End", "Start", "Label..." });
        cmb.SelectedIndex = 0;
        return cmb;
    }

    private static void SetGoToSelection(ComboBox cmb, GoToTarget t)
    {
        var text = ToGoToText(t);
        if (text.StartsWith("Label:", StringComparison.Ordinal) && !cmb.Items.Contains(text))
            cmb.Items.Insert(3, text);
        cmb.SelectedItem = text;
    }

    private static string ToGoToText(GoToTarget t)
        => t.Kind switch
        {
            GoToKind.End => "End",
            GoToKind.Start => "Start",
            GoToKind.Label => $"Label:{t.Label}",
            _ => "Next"
        };

    private static GoToTarget FromGoToText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return GoToTarget.Next();
        if (text == "End") return GoToTarget.End();
        if (text == "Start") return GoToTarget.Start();
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

    public static FindTextOcrAction? Show(IWin32Window owner, FindTextOcrAction? initial)
    {
        using var dlg = new FindTextOcrDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
