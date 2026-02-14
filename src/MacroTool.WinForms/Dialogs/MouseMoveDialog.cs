using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Mouse move（2-4-2 Move）設定ダイアログ。
/// UI仕様: docs/images/2-4-2_Move.png（Help ボタンは不要）
///
/// NOTE:
/// - v1.0 Domain は Start/End を int で保持するため、現状は数値のみ受け付ける。
/// - UI の "Value or variable..." は将来拡張用の見た目（TDD的にUI優先で合わせる）。
/// </summary>
public sealed class MouseMoveDialog : Form
{
    private const string Placeholder = "Value or variable...";

    private readonly Label _lblHint;
    private readonly CheckBox _chkRelative;
    private readonly ComboBox _cmbStartX;
    private readonly ComboBox _cmbStartY;
    private readonly ComboBox _cmbEndX;
    private readonly ComboBox _cmbEndY;
    private readonly ComboBox _cmbDuration;

    private int _captureIndex = 0; // 0: start, 1: end

    public MouseMoveAction Result { get; private set; } = new();

    private MouseMoveDialog(MouseMoveAction? initial)
    {
        Text = "Mouse move action";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;

        Width = 520;
        Height = 320;

        _lblHint = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Click the start and end point of the desired mouse move or press hotkey \"Space\" to capture the mouse position...",
            Padding = new Padding(0, 0, 0, 6)
        };

        _chkRelative = new CheckBox
        {
            Text = "Relative to current mouse position",
            AutoSize = true
        };

        _cmbStartX = CreateValueBox();
        _cmbStartY = CreateValueBox();
        _cmbEndX = CreateValueBox();
        _cmbEndY = CreateValueBox();

        _cmbDuration = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 120
        };
        _cmbDuration.Items.AddRange(new object[] { "0", "10", "20", "50", "100", "200", "500", "1000" });

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

        // Layout
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // hint
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // relative
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // start labels
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // start inputs
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 18)); // end labels
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // end inputs
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // duration
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // buttons

        table.Controls.Add(_lblHint, 0, 0);
        table.SetColumnSpan(_lblHint, 2);

        table.Controls.Add(_chkRelative, 0, 1);
        table.SetColumnSpan(_chkRelative, 2);

        table.Controls.Add(new Label { Text = "Start X:", AutoSize = true }, 0, 2);
        table.Controls.Add(new Label { Text = "Start Y:", AutoSize = true }, 1, 2);
        table.Controls.Add(_cmbStartX, 0, 3);
        table.Controls.Add(_cmbStartY, 1, 3);

        table.Controls.Add(new Label { Text = "End X:", AutoSize = true }, 0, 4);
        table.Controls.Add(new Label { Text = "End Y:", AutoSize = true }, 1, 4);
        table.Controls.Add(_cmbEndX, 0, 5);
        table.Controls.Add(_cmbEndY, 1, 5);

        var durationPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };
        durationPanel.Controls.Add(new Label { Text = "Duration:", AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
        durationPanel.Controls.Add(_cmbDuration);
        durationPanel.Controls.Add(new Label { Text = "ms", AutoSize = true, Margin = new Padding(6, 6, 0, 0) });
        table.Controls.Add(durationPanel, 0, 6);
        table.SetColumnSpan(durationPanel, 2);

        table.Controls.Add(buttons, 0, 7);
        table.SetColumnSpan(buttons, 2);

        Controls.Add(table);

        // initial
        var init = initial ?? new MouseMoveAction();
        _chkRelative.Checked = init.Relative;
        SetValue(_cmbStartX, init.StartX.ToString());
        SetValue(_cmbStartY, init.StartY.ToString());
        SetValue(_cmbEndX, init.EndX.ToString());
        SetValue(_cmbEndY, init.EndY.ToString());
        _cmbDuration.Text = init.DurationMs.ToString();

        // Space: start -> end の順でカーソル位置を取り込む
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;
            var p = Cursor.Position;
            if (_captureIndex == 0)
            {
                SetValue(_cmbStartX, p.X.ToString());
                SetValue(_cmbStartY, p.Y.ToString());
                _captureIndex = 1;
            }
            else
            {
                SetValue(_cmbEndX, p.X.ToString());
                SetValue(_cmbEndY, p.Y.ToString());
                _captureIndex = 0;
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
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

    private static ComboBox CreateValueBox()
    {
        var cmb = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 220
        };

        // 擬似プレースホルダー（見た目合わせ）
        cmb.Text = Placeholder;
        cmb.ForeColor = SystemColors.GrayText;
        cmb.Enter += (_, __) =>
        {
            if (!string.Equals(cmb.Text, Placeholder, StringComparison.Ordinal)) return;
            cmb.Text = string.Empty;
            cmb.ForeColor = SystemColors.WindowText;
        };
        cmb.Leave += (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(cmb.Text)) return;
            cmb.Text = Placeholder;
            cmb.ForeColor = SystemColors.GrayText;
        };

        return cmb;
    }

    private static void SetValue(ComboBox cmb, string value)
    {
        cmb.ForeColor = SystemColors.WindowText;
        cmb.Text = value;
    }

    private static string GetValue(ComboBox cmb)
    {
        var t = cmb.Text.Trim();
        return string.Equals(t, Placeholder, StringComparison.Ordinal) ? string.Empty : t;
    }

    private bool TryBuildResult(out MouseMoveAction result, out string error)
    {
        error = string.Empty;
        result = new MouseMoveAction();

        if (!int.TryParse(GetValue(_cmbStartX), out var sx)) { error = "Start X must be an integer."; return false; }
        if (!int.TryParse(GetValue(_cmbStartY), out var sy)) { error = "Start Y must be an integer."; return false; }
        if (!int.TryParse(GetValue(_cmbEndX), out var ex)) { error = "End X must be an integer."; return false; }
        if (!int.TryParse(GetValue(_cmbEndY), out var ey)) { error = "End Y must be an integer."; return false; }
        if (!int.TryParse(_cmbDuration.Text.Trim(), out var dur) || dur < 0) { error = "Duration must be a non-negative integer."; return false; }

        result = new MouseMoveAction
        {
            Relative = _chkRelative.Checked,
            StartX = sx,
            StartY = sy,
            EndX = ex,
            EndY = ey,
            DurationMs = dur
        };
        return true;
    }

    public static MouseMoveAction? Show(IWin32Window owner, MouseMoveAction? initial = null)
    {
        using var dlg = new MouseMoveDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
