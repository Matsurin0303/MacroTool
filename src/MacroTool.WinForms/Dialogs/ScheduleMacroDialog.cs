using System.Text;

namespace MacroTool.WinForms.Dialogs;
public sealed record ScheduleMacroDialogResult(bool Clear, DateTime? RunAt);

public sealed class ScheduleMacroDialog : Form
{
    private readonly DateTimePicker _picker;
    private readonly CheckBox _chkClear;

    public DateTime ScheduledAt => _picker.Value;
    public bool ClearSchedule => _chkClear.Checked;



    private ScheduleMacroDialog(DateTime? current)
    {
        Text = "Schedule Macro";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        Width = 520;
        Height = 210;

        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Run at (local time):"
        };

        _picker = new DateTimePicker
        {
            Dock = DockStyle.Top,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy/MM/dd HH:mm:ss",
            Value = current ?? DateTime.Now.AddMinutes(1)
        };

        _chkClear = new CheckBox
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Clear existing schedule"
        };
        _chkClear.CheckedChanged += (_, __) => _picker.Enabled = !_chkClear.Checked;

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

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        body.Controls.Add(_chkClear);
        body.Controls.Add(_picker);
        body.Controls.Add(lbl);

        Controls.Add(body);
        Controls.Add(buttons);
    }

    public static ScheduleMacroDialogResult? Show(IWin32Window owner, DateTime? current)
    {
        using var dlg = new ScheduleMacroDialog(current);
        if (dlg.ShowDialog(owner) != DialogResult.OK)
            return null;

        // 「Clear existing schedule」にチェックが付いていたら Clear=true で返す
        if (dlg._chkClear.Checked)
            return new ScheduleMacroDialogResult(Clear: true, RunAt: null);

        return new ScheduleMacroDialogResult(Clear: false, RunAt: dlg._picker.Value);
    }
}
