using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Mouse click（2-4-1 Click）設定ダイアログ。
/// UI仕様: docs/images/2-4-1_Click.png（Help ボタンは不要）
///
/// NOTE:
/// - v1.0 仕様書にある項目（Mouse button / Action / Relative / X / Y）を扱う。
/// - 画像にある "Random wiggle" は v1.0 仕様書に記載がないため、現状は UI のみ（結果に反映しない）。
/// </summary>
public sealed class MouseClickDialog : Form
{
    private readonly Label _lblHint;
    private readonly ComboBox _cmbButton;
    private readonly ComboBox _cmbAction;
    private readonly CheckBox _chkRelative;
    private readonly ComboBox _cmbX;
    private readonly ComboBox _cmbY;
    private readonly CheckBox _chkRandomWiggle;

    public MouseClickAction Result { get; private set; } = new();

    private MouseClickDialog(MouseClickAction? initial)
    {
        Text = "Mouse click action";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;

        // 画像の雰囲気に寄せる（厳密なピクセル合わせは後段で調整）
        Width = 520;
        Height = 300;

        _lblHint = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Click or press hotkey \"Space\" to capture the mouse position...",
            Padding = new Padding(0, 0, 0, 6)
        };

        var lblButton = new Label { Text = "Mouse button:", AutoSize = true };
        _cmbButton = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbButton.Items.AddRange(Enum.GetNames(typeof(MouseButton)));

        var lblAction = new Label { Text = "Action:", AutoSize = true };
        _cmbAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbAction.Items.AddRange(Enum.GetNames(typeof(MouseClickType)));

        _chkRelative = new CheckBox
        {
            Text = "Relative to current mouse position",
            AutoSize = true
        };

        var pnlXY = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };

        var lblX = new Label { Text = "X:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };
        _cmbX = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 160
        };
        _cmbX.Text = string.Empty;

        var lblY = new Label { Text = "Y:", AutoSize = true, Margin = new Padding(12, 6, 6, 0) };
        _cmbY = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 160
        };
        _cmbY.Text = string.Empty;

        pnlXY.Controls.Add(lblX);
        pnlXY.Controls.Add(_cmbX);
        pnlXY.Controls.Add(lblY);
        pnlXY.Controls.Add(_cmbY);

        _chkRandomWiggle = new CheckBox
        {
            Text = "Random wiggle",
            AutoSize = true
        };

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
            RowCount = 6,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // hint
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // button
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // action
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // relative
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // xy
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // random + buttons

        table.Controls.Add(_lblHint, 0, 0);
        table.SetColumnSpan(_lblHint, 2);

        table.Controls.Add(lblButton, 0, 1);
        table.Controls.Add(_cmbButton, 1, 1);

        table.Controls.Add(lblAction, 0, 2);
        table.Controls.Add(_cmbAction, 1, 2);

        table.Controls.Add(_chkRelative, 0, 3);
        table.SetColumnSpan(_chkRelative, 2);

        table.Controls.Add(new Label { Text = string.Empty, AutoSize = true }, 0, 4);
        table.Controls.Add(pnlXY, 1, 4);

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        bottom.Controls.Add(_chkRandomWiggle, 0, 0);
        bottom.Controls.Add(buttons, 0, 1);
        table.Controls.Add(bottom, 0, 5);
        table.SetColumnSpan(bottom, 2);

        Controls.Add(table);

        // initial
        var init = initial ?? new MouseClickAction();
        _cmbButton.SelectedItem = init.Button.ToString();
        _cmbAction.SelectedItem = init.Action.ToString();
        _chkRelative.Checked = init.Relative;
        _cmbX.Text = init.X.ToString();
        _cmbY.Text = init.Y.ToString();

        // Space で現在マウス位置を取り込み
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space)
            {
                var p = Cursor.Position;
                _cmbX.Text = p.X.ToString();
                _cmbY.Text = p.Y.ToString();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

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

    private bool TryBuildResult(out MouseClickAction result, out string error)
    {
        error = string.Empty;
        result = new MouseClickAction();

        if (!Enum.TryParse<MouseButton>(_cmbButton.SelectedItem?.ToString(), out var btn))
            btn = MouseButton.Left;

        if (!Enum.TryParse<MouseClickType>(_cmbAction.SelectedItem?.ToString(), out var act))
            act = MouseClickType.Click;

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

        result = new MouseClickAction
        {
            Button = btn,
            Action = act,
            // 互換/既存UI（ActionEditorForm）で ClickType が表示される場合に備えて同期
            ClickType = act,
            Relative = _chkRelative.Checked,
            X = x,
            Y = y
        };
        return true;
    }

    public static MouseClickAction? Show(IWin32Window owner, MouseClickAction? initial = null)
    {
        using var dlg = new MouseClickDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
