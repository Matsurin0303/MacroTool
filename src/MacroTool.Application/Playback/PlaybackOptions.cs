namespace MacroTool.Application.Playback;

public sealed class PlaybackOptions
{
    public bool EnableStabilizeWait { get; init; } = true;
    public int CursorSettleDelayMs { get; init; } = 10;
    public int ClickHoldDelayMs { get; init; } = 10;
}

