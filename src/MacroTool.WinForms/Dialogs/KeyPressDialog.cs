using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// キー入力（Key press）設定ダイアログ。
/// ユーザーがキーを押して選択できる簡易UI。
/// </summary>
public sealed class KeyPressDialog : Form
{
    private readonly ComboBox _cmbOption;
    private readonly TextBox _txtKey;
    private readonly NumericUpDown _numCount;

    private Keys _selectedKey = Keys.None;

    public KeyPressAction Result { get; private set; } = new();

    private KeyPressDialog(KeyPressAction? initial)
    {
        Text = "Key press";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;
        Width = 420;
        Height = 220;

        var lblOption = new Label { Text = "Option", AutoSize = true };
        _cmbOption = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        _cmbOption.Items.AddRange(Enum.GetNames(typeof(KeyPressOption)));

        var lblKey = new Label { Text = "Key (press any key)", AutoSize = true };
        _txtKey = new TextBox { ReadOnly = true, Width = 240 };

        var lblCount = new Label { Text = "Count", AutoSize = true };
        _numCount = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1, Width = 80 };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100 };

        AcceptButton = ok;
        CancelButton = cancel;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        table.Controls.Add(lblOption, 0, 0);
        table.Controls.Add(_cmbOption, 1, 0);
        table.Controls.Add(lblKey, 0, 1);
        table.Controls.Add(_txtKey, 1, 1);
        table.Controls.Add(lblCount, 0, 2);
        table.Controls.Add(_numCount, 1, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        table.Controls.Add(buttons, 0, 3);
        table.SetColumnSpan(buttons, 2);

        Controls.Add(table);

        // initial
        if (initial != null)
        {
            _cmbOption.SelectedItem = initial.Option.ToString();
            _numCount.Value = Math.Clamp(initial.Count, 1, 100);
            try
            {
                _selectedKey = (Keys)initial.Key.Code;
            }
            catch
            {
                _selectedKey = Keys.None;
            }
        }
        else
        {
            _cmbOption.SelectedItem = KeyPressOption.Press.ToString();
        }

        UpdateKeyText();

        KeyDown += OnKeyDown;
        FormClosing += (_, __) => BuildResult();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // 画面上の編集操作でESCが効くようにする
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        _selectedKey = e.KeyCode;
        UpdateKeyText();
        e.SuppressKeyPress = true;
    }

    private void UpdateKeyText()
    {
        _txtKey.Text = _selectedKey == Keys.None ? "(none)" : _selectedKey.ToString();
    }

    private void BuildResult()
    {
        if (DialogResult != DialogResult.OK) return;

        var option = Enum.TryParse<KeyPressOption>(_cmbOption.SelectedItem?.ToString(), out var o)
            ? o
            : KeyPressOption.Press;

        Result = new KeyPressAction
        {
            Option = option,
            Key = new VirtualKey((ushort)_selectedKey),
            Count = (int)_numCount.Value
        };
    }

    public static KeyPressAction? Show(IWin32Window owner, KeyPressAction? initial = null)
    {
        using var dlg = new KeyPressDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }
}
