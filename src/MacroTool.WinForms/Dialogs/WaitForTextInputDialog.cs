using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Wait for text input（2-6-5）設定ダイアログ。
/// UI仕様: docs/images/2-6-5_WaitForTextInput.png（Help ボタンは不要）
/// </summary>
public sealed class WaitForTextInputDialog : Form
{
    private readonly TextBox _txtWaitFor;
    private readonly ComboBox _cmbTrueGoTo;
    private readonly NumericUpDown _numTimeoutSec;
    private readonly ComboBox _cmbFalseGoTo;

    public WaitForTextInputAction Result { get; private set; } = new();

    private WaitForTextInputDialog(WaitForTextInputAction? initial)
    {
        Text = "Wait for text input";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        Width = 560;
        Height = 430;

        var lbl = new Label
        {
            Text = "Text to wait for (right-click to add variables):",
            AutoSize = true,
            Dock = DockStyle.Top
        };

        _txtWaitFor = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 120,
            Dock = DockStyle.Top
        };

        var grpTrue = new GroupBox { Text = "If the text has been input", Dock = DockStyle.Top, Height = 70 };
        var tblTrue = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10, 10, 10, 10) };
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblTrue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblTrue.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        tblTrue.Controls.Add(new Label { Text = "Go to", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
        _cmbTrueGoTo = CreateGoToCombo();
        tblTrue.Controls.Add(_cmbTrueGoTo, 1, 0);
        grpTrue.Controls.Add(tblTrue);

        var grpFalse = new GroupBox { Text = "If the text has not been input", Dock = DockStyle.Top, Height = 110 };
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
        root.Controls.Add(_txtWaitFor);
        root.Controls.Add(lbl);
        Controls.Add(root);
        Controls.Add(pnlButtons);

        // initial
        var init = initial ?? CreateDefault();
        _txtWaitFor.Text = init.TextToWaitFor ?? string.Empty;
        _cmbTrueGoTo.SelectedItem = ToGoToText(init.TrueGoTo);
        _cmbFalseGoTo.SelectedItem = ToGoToText(init.FalseGoTo);
        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;
            Result = new WaitForTextInputAction
            {
                TextToWaitFor = _txtWaitFor.Text,
                TrueGoTo = FromGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),
                FalseGoTo = FromGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
                TimeoutMs = (int)_numTimeoutSec.Value * 1000
            };
        };
    }

    private static WaitForTextInputAction CreateDefault()
        => new()
        {
            TextToWaitFor = "OK",
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

    public static WaitForTextInputAction? Show(IWin32Window owner, WaitForTextInputAction? initial)
    {
        using var dlg = new WaitForTextInputDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
