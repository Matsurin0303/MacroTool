namespace MacroTool.WinForms.Settings;

public sealed class UiSettingsPage : UserControl
{
    private readonly CheckBox _chkConfirmDelete = new() { Text = "Confirm before delete" };

    public UiSettingsPage(AppSettings.UiSettings s)
    {
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(16),
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        _chkConfirmDelete.Checked = s.ConfirmDelete;

        layout.Controls.Add(_chkConfirmDelete);
        Controls.Add(layout);
    }

    public void ApplyTo(AppSettings.UiSettings s)
    {
        s.ConfirmDelete = _chkConfirmDelete.Checked;
    }
}
