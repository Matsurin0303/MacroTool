namespace MacroTool.WinForms.Settings;

public sealed class AppSettings
{
    public PlaybackSettings Playback { get; set; } = new();
    public UiSettings Ui { get; set; } = new();

    public sealed class PlaybackSettings
    {
        public bool EnableStabilizeWait { get; set; } = true;
        public int CursorSettleDelayMs { get; set; } = 10;
        public int ClickHoldDelayMs { get; set; } = 10;
    }

    public sealed class UiSettings
    {
        public bool ConfirmDelete { get; set; } = true;
    }
}
