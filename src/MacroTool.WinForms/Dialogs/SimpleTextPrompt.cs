namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// 1つの文字列を入力させる簡易プロンプト。
/// </summary>
public static class SimpleTextPrompt
{
    public static string? Show(IWin32Window owner, string title, string message, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Width = 520,
            Height = 160
        };

        var lbl = new Label
        {
            Text = message,
            AutoSize = false,
            Left = 12,
            Top = 12,
            Width = 480,
            Height = 22
        };

        var txt = new TextBox
        {
            Left = 12,
            Top = 40,
            Width = 480,
            Text = defaultValue
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 312, Top = 72, Width = 86 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 406, Top = 72, Width = 86 };

        form.Controls.Add(lbl);
        form.Controls.Add(txt);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog(owner) == DialogResult.OK ? txt.Text : null;
    }
}
