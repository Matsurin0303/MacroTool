using MacroTool.Application.Playback;

namespace MacroTool.Application.Abstractions;

public sealed class PlaybackOptionsAccessor : IPlaybackOptionsAccessor
{
    private readonly object _lock = new();
    private PlaybackOptions _current;

    public PlaybackOptionsAccessor(PlaybackOptions initial)
    {
        _current = initial;
    }

    public PlaybackOptions Current
    {
        get { lock (_lock) return _current; }
    }

    public void Update(PlaybackOptions options)
    {
        lock (_lock) _current = options;
    }
}
