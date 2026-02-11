namespace MacroTool.WinForms.Settings;

public sealed class PlaybackSettingsPage : UserControl
{
    private readonly CheckBox _chkEnable = new() { Text = "Enable stabilize waits (recommended)" };
    private readonly NumericUpDown _numCursor = new() { Minimum = 0, Maximum = 2000, Increment = 1 };
    private readonly NumericUpDown _numHold = new() { Minimum = 0, Maximum = 2000, Increment = 1 };

    public PlaybackSettingsPage(AppSettings.PlaybackSettings s)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(16),
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        var lblCursor = new Label { Text = "Cursor settle delay (ms)", AutoSize = true };
        var lblHold = new Label { Text = "Click hold delay (ms)", AutoSize = true };

        _chkEnable.Checked = s.EnableStabilizeWait;
        _numCursor.Value = s.CursorSettleDelayMs;
        _numHold.Value = s.ClickHoldDelayMs;

        layout.Controls.Add(_chkEnable, 0, 0);
        layout.SetColumnSpan(_chkEnable, 2);

        layout.Controls.Add(lblCursor, 0, 1);
        layout.Controls.Add(_numCursor, 1, 1);

        layout.Controls.Add(lblHold, 0, 2);
        layout.Controls.Add(_numHold, 1, 2);

        Controls.Add(layout);

        _chkEnable.CheckedChanged += (_, __) =>
        {
            _numCursor.Enabled = _chkEnable.Checked;
            _numHold.Enabled = _chkEnable.Checked;
        };
        _numCursor.Enabled = _chkEnable.Checked;
        _numHold.Enabled = _chkEnable.Checked;
    }

    public void ApplyTo(AppSettings.PlaybackSettings s)
    {
        s.EnableStabilizeWait = _chkEnable.Checked;
        s.CursorSettleDelayMs = (int)_numCursor.Value;
        s.ClickHoldDelayMs = (int)_numHold.Value;
    }
}
