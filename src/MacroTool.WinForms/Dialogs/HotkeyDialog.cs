using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// ホットキー入力（Ctrl+Z など）を取得し、Key press アクション列に展開する。
/// </summary>
public sealed class HotkeyDialog : Form
{
    private readonly Label _lbl;
    private readonly TextBox _txt;
    private Keys _mainKey = Keys.None;
    private bool _ctrl;
    private bool _shift;
    private bool _alt;
    private bool _win;

    public IReadOnlyList<MacroAction> ResultActions { get; private set; } = Array.Empty<MacroAction>();

    private HotkeyDialog()
    {
        Text = "Hotkey";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        KeyPreview = true;
        Width = 520;
        Height = 180;

        _lbl = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Press a hotkey combination (e.g., Ctrl+Z)" 
        };

        _txt = new TextBox
        {
            Dock = DockStyle.Top,
            ReadOnly = true,
            Height = 28
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100 };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(10)
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(buttons);
        Controls.Add(_txt);
        Controls.Add(_lbl);

        KeyDown += OnKeyDown;
        FormClosing += (_, __) => BuildActions();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        _ctrl = e.Control;
        _shift = e.Shift;
        _alt = e.Alt;

        // WinキーはOS側で奪われることがあるため、入力できた場合のみ扱う
        if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
        {
            _win = true;
            _mainKey = Keys.None;
        }
        else
        {
            _mainKey = e.KeyCode;
        }

        _txt.Text = FormatHotkey();
        e.SuppressKeyPress = true;
    }

    private string FormatHotkey()
    {
        var parts = new List<string>();
        if (_ctrl) parts.Add("Ctrl");
        if (_shift) parts.Add("Shift");
        if (_alt) parts.Add("Alt");
        if (_win) parts.Add("Win");
        if (_mainKey != Keys.None) parts.Add(_mainKey.ToString());
        return parts.Count == 0 ? "(none)" : string.Join("+", parts);
    }

    private void BuildActions()
    {
        if (DialogResult != DialogResult.OK) return;
        if (_mainKey == Keys.None)
        {
            // メインキーが無い場合は無効
            ResultActions = Array.Empty<MacroAction>();
            return;
        }

        var mods = new List<Keys>();
        if (_ctrl) mods.Add(Keys.ControlKey);
        if (_shift) mods.Add(Keys.ShiftKey);
        if (_alt) mods.Add(Keys.Menu);
        if (_win) mods.Add(Keys.LWin);

        var actions = new List<MacroAction>();

        // Down (modifier)
        foreach (var k in mods)
        {
            actions.Add(new KeyPressAction { Option = KeyPressOption.Down, Key = new VirtualKey((ushort)k), Count = 1 });
        }

        // Press (main)
        actions.Add(new KeyPressAction { Option = KeyPressOption.Press, Key = new VirtualKey((ushort)_mainKey), Count = 1 });

        // Up (modifier) - reverse
        for (int i = mods.Count - 1; i >= 0; i--)
        {
            var k = mods[i];
            actions.Add(new KeyPressAction { Option = KeyPressOption.Up, Key = new VirtualKey((ushort)k), Count = 1 });
        }

        ResultActions = actions;
    }

    public new static IReadOnlyList<MacroAction>? Show(IWin32Window owner)
    {
        using var dlg = new HotkeyDialog();
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.ResultActions : null;
    }
}
