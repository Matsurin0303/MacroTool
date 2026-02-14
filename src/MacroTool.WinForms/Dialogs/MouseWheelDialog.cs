using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Mouse wheel（2-4-3 Wheel）設定ダイアログ。
/// UI仕様: docs/images/2-4-3_Wheel.png（Help ボタンは不要）
/// </summary>
public sealed class MouseWheelDialog : Form
{
    private readonly Label _lblHint;
    private readonly ComboBox _cmbOrientation;
    private readonly TextBox _txtValue;

    public MouseWheelAction Result { get; private set; } = new();

    private MouseWheelDialog(MouseWheelAction? initial)
    {
        Text = "Mouse wheel action";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        Width = 480;
        Height = 220;

        _lblHint = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Perform the mouse scroll-wheel operation and edit it here...",
            Padding = new Padding(0, 0, 0, 6)
        };

        var lblOrientation = new Label { Text = "Orientation:", AutoSize = true };
        _cmbOrientation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbOrientation.Items.AddRange(Enum.GetNames(typeof(WheelOrientation)));

        var lblValue = new Label { Text = "Value:", AutoSize = true };
        _txtValue = new TextBox { Width = 220 };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100 };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        table.Controls.Add(_lblHint, 0, 0);
        table.SetColumnSpan(_lblHint, 2);

        table.Controls.Add(lblOrientation, 0, 1);
        table.Controls.Add(_cmbOrientation, 1, 1);

        table.Controls.Add(lblValue, 0, 2);
        table.Controls.Add(_txtValue, 1, 2);

        table.Controls.Add(buttons, 0, 3);
        table.SetColumnSpan(buttons, 2);

        Controls.Add(table);

        // initial
        var init = initial ?? new MouseWheelAction();
        _cmbOrientation.SelectedItem = init.Orientation.ToString();
        _txtValue.Text = init.Value.ToString();

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;
            if (!TryBuildResult(out var res, out var error))
            {
                MessageBox.Show(this, error, "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
            Result = res;
        };
    }

    private bool TryBuildResult(out MouseWheelAction result, out string error)
    {
        error = string.Empty;
        result = new MouseWheelAction();

        if (!Enum.TryParse<WheelOrientation>(_cmbOrientation.SelectedItem?.ToString(), out var ori))
            ori = WheelOrientation.Vertical;

        if (!int.TryParse(_txtValue.Text.Trim(), out var v))
        {
            error = "Value must be an integer.";
            return false;
        }

        result = new MouseWheelAction
        {
            Orientation = ori,
            Value = v
        };
        return true;
    }

    public static MouseWheelAction? Show(IWin32Window owner, MouseWheelAction? initial = null)
    {
        using var dlg = new MouseWheelDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
