using System.Drawing;
using System.Globalization;
using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Wait for pixel color（2-6-2）設定ダイアログ。
/// UI仕様: docs/images/2-6-2_WaitForPixelColor.png（Help ボタンは不要）
/// </summary>
public sealed class WaitForPixelColorDialog : Form
{
    private readonly ComboBox _cmbX;
    private readonly ComboBox _cmbY;
    private readonly TextBox _txtHex;
    private readonly Panel _pnlColor;
    private readonly NumericUpDown _numTolerance;
    private readonly NumericUpDown _numTimeoutSec;
    private readonly ComboBox _cmbTrueGoTo;
    private readonly ComboBox _cmbFalseGoTo;

    public WaitForPixelColorAction Result { get; private set; } = new();

    private WaitForPixelColorDialog(WaitForPixelColorAction? initial)
    {
        Text = "Wait for pixel color";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;

        Width = 520;
        Height = 520;

        var lblIntro = new Label
        {
            Text = "This action waits until a specific pixel color appears on the screen.",
            AutoSize = true,
            Dock = DockStyle.Top
        };

        // === Group: Pixel Detection ===
        var grpPixel = new GroupBox { Text = "Pixel Detection", Dock = DockStyle.Top, Height = 210 };
        var lblHint = new Label
        {
            Text = "Click the pixel or press hotkey \"Space\" to capture the position to monitor for color changes...",
            AutoSize = true,
            Dock = DockStyle.Top
        };

        var lblX = new Label { Text = "X:", AutoSize = true };
        _cmbX = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };
        var lblY = new Label { Text = "Y:", AutoSize = true };
        _cmbY = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };

        var lblColor = new Label { Text = "Color:", AutoSize = true };
        _txtHex = new TextBox { Width = 110, MaxLength = 6 };
        _pnlColor = new Panel { Width = 60, Height = 22, BorderStyle = BorderStyle.FixedSingle };

        var lblTol = new Label { Text = "Color tolerance:", AutoSize = true };
        _numTolerance = new NumericUpDown { Minimum = 0, Maximum = 100, Width = 80 };
        var lblPercent = new Label { Text = "%", AutoSize = true, Margin = new Padding(6, 6, 0, 0) };

        var tblPixel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(10, 10, 10, 10)
        };
        tblPixel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblPixel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblPixel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tblPixel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblPixel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tblPixel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tblPixel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tblPixel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        tblPixel.Controls.Add(lblHint, 0, 0);
        tblPixel.SetColumnSpan(lblHint, 4);

        tblPixel.Controls.Add(lblX, 0, 1);
        tblPixel.Controls.Add(_cmbX, 1, 1);
        tblPixel.Controls.Add(lblY, 2, 1);
        tblPixel.Controls.Add(_cmbY, 3, 1);

        tblPixel.Controls.Add(lblColor, 0, 2);
        tblPixel.Controls.Add(_txtHex, 1, 2);
        tblPixel.Controls.Add(_pnlColor, 2, 2);

        tblPixel.Controls.Add(lblTol, 0, 3);
        tblPixel.Controls.Add(_numTolerance, 1, 3);
        tblPixel.Controls.Add(lblPercent, 2, 3);

        grpPixel.Controls.Add(tblPixel);

        // === Group: If the color shows up ===
        var grpTrue = new GroupBox { Text = "If the color shows up", Dock = DockStyle.Top, Height = 70 };
        var tblTrue = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10, 10, 10, 10) };
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblTrue.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _cmbTrueGoTo = CreateGoToCombo();
        tblTrue.Controls.Add(_cmbTrueGoTo, 1, 0);
        grpTrue.Controls.Add(tblTrue);

        // === Group: If the color does not show up ===
        var grpFalse = new GroupBox { Text = "If the color does not show up", Dock = DockStyle.Top, Height = 100 };
        var tblFalse = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(10, 10, 10, 10) };
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        tblFalse.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
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

        // Buttons
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
        root.Controls.Add(grpPixel);
        root.Controls.Add(lblIntro);
        Controls.Add(root);
        Controls.Add(pnlButtons);

        // initial
        var init = initial ?? CreateDefault();
        _cmbX.Text = init.X.ToString();
        _cmbY.Text = init.Y.ToString();
        SetHex(init.ColorHex);
        _numTolerance.Value = Math.Clamp(init.TolerancePercent, 0, 100);
        _cmbTrueGoTo.SelectedItem = ToGoToText(init.TrueGoTo);
        _cmbFalseGoTo.SelectedItem = ToGoToText(init.FalseGoTo);
        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        _txtHex.TextChanged += (_, __) => UpdateColorPreview();

        // Space: capture current cursor position + pixel color
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;
            var p = Cursor.Position;
            _cmbX.Text = p.X.ToString();
            _cmbY.Text = p.Y.ToString();
            var c = GetPixelColorAt(p);
            _txtHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
            UpdateColorPreview();
            e.Handled = true;
            e.SuppressKeyPress = true;
        };

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;
            if (!TryBuildResult(out var res, out var err))
            {
                MessageBox.Show(this, err, "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
            Result = res;
        };
    }

    private static WaitForPixelColorAction CreateDefault()
    {
        var p = Cursor.Position;
        var c = GetPixelColorAt(p);
        return new WaitForPixelColorAction
        {
            X = p.X,
            Y = p.Y,
            ColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}",
            TolerancePercent = 10,
            TrueGoTo = GoToTarget.Next(),
            FalseGoTo = GoToTarget.End(),
            TimeoutMs = 120_000
        };
    }

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

    private void SetHex(string hex)
    {
        hex = (hex ?? string.Empty).Trim();
        if (hex.StartsWith("#")) hex = hex[1..];
        _txtHex.Text = hex;
        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        if (TryParseHex(_txtHex.Text.Trim(), out var color))
            _pnlColor.BackColor = color;
    }

    private bool TryBuildResult(out WaitForPixelColorAction result, out string error)
    {
        error = string.Empty;
        result = new WaitForPixelColorAction();

        if (!int.TryParse(_cmbX.Text.Trim(), out var x))
        {
            error = "X must be an integer.";
            return false;
        }
        if (!int.TryParse(_cmbY.Text.Trim(), out var y))
        {
            error = "Y must be an integer.";
            return false;
        }

        var hex = _txtHex.Text.Trim();
        if (!TryParseHex(hex, out var c))
        {
            error = "Color must be a 6-digit hex value (RRGGBB).";
            return false;
        }

        result = new WaitForPixelColorAction
        {
            X = x,
            Y = y,
            ColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}",
            TolerancePercent = (int)_numTolerance.Value,
            TrueGoTo = FromGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),
            FalseGoTo = FromGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
            TimeoutMs = (int)_numTimeoutSec.Value * 1000
        };
        return true;
    }

    private static bool TryParseHex(string hex, out Color color)
    {
        color = Color.Black;
        hex = (hex ?? string.Empty).Trim();
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length != 6) return false;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return false;
        int r = (rgb >> 16) & 0xFF;
        int g = (rgb >> 8) & 0xFF;
        int b = rgb & 0xFF;
        color = Color.FromArgb(r, g, b);
        return true;
    }

    private static Color GetPixelColorAt(Point p)
    {
        using var bmp = new Bitmap(1, 1);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(p, new Point(0, 0), new Size(1, 1));
        }
        return bmp.GetPixel(0, 0);
    }

    public static WaitForPixelColorAction? Show(IWin32Window owner, WaitForPixelColorAction? initial)
    {
        using var dlg = new WaitForPixelColorDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
