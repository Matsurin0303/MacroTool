using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Wait time（2-6-1 Wait）設定ダイアログ。
/// UI仕様: docs/images/2-6-1_Wait.png（Help ボタンは不要）
///
/// NOTE:
/// - 仕様画像では「Value or variable」だが、現状の Domain は int(Milliseconds) のみ。
///   そのため入力は整数(ms)のみ受け付ける（変数表現の実装は別タスク）。
/// </summary>
public sealed class WaitTimeDialog : Form
{
    private readonly ComboBox _cmbValue;

    public WaitTimeAction Result { get; private set; } = new();

    private WaitTimeDialog(WaitTimeAction? initial)
    {
        Text = "Wait time";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        // 画像サイズ感に寄せる
        Width = 320;
        Height = 190;

        var lblHint = new Label
        {
            Text = "Enter the time to wait",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        var lblValue = new Label
        {
            Text = "Value or variable:",
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 4)
        };

        _cmbValue = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 180
        };
        _cmbValue.Items.AddRange(new object[] { "10", "100", "500", "1000", "2000", "5000" });

        var lblMs = new Label
        {
            Text = "ms",
            AutoSize = true,
            Margin = new Padding(8, 6, 0, 0)
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };

        AcceptButton = ok;
        CancelButton = cancel;

        var pnlValue = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };
        pnlValue.Controls.Add(_cmbValue);
        pnlValue.Controls.Add(lblMs);

        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0, 10, 0, 0)
        };
        pnlButtons.Controls.Add(ok);
        pnlButtons.Controls.Add(cancel);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // hint
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // label
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // value
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // buttons (bottom)

        table.Controls.Add(lblHint, 0, 0);
        table.Controls.Add(lblValue, 0, 2);
        table.Controls.Add(pnlValue, 0, 3);
        table.Controls.Add(pnlButtons, 0, 4);

        Controls.Add(table);

        // initial
        var init = initial ?? new WaitTimeAction { Milliseconds = 1000 };
        _cmbValue.Text = init.Milliseconds.ToString();

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;

            if (!int.TryParse(_cmbValue.Text.Trim(), out var ms) || ms < 0)
            {
                MessageBox.Show(this, "Milliseconds must be a non-negative integer.", "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            Result = new WaitTimeAction { Milliseconds = ms };
        };
    }

    public static WaitTimeAction? Show(IWin32Window owner, WaitTimeAction? initial)
    {
        using var dlg = new WaitTimeDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
