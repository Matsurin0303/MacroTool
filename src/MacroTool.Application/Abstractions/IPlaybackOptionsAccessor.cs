using MacroTool.Application.Playback;

namespace MacroTool.Application.Abstractions;

public interface IPlaybackOptionsAccessor
{
    PlaybackOptions Current { get; }
    void Update(PlaybackOptions options);
}
