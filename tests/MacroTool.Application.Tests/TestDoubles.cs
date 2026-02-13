using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;

namespace MacroTool.Application.Tests;

internal sealed class FakeRecorder : IRecorder
{
    public event EventHandler<RecordedAction>? ActionRecorded;
    public bool IsRecording { get; private set; }
    public bool StartResult { get; set; } = true;


    public bool Start()
    {
        IsRecording = StartResult;
        return StartResult;
    }

    public void Stop() => IsRecording = false;

    public void Raise(TimeSpan elapsed, MacroAction action)
    => ActionRecorded?.Invoke(this, new RecordedAction(elapsed, action));
}

internal sealed class FakePlayer : IPlayer
{
    public bool ThrowOnPlay { get; set; }
    public Macro? PlayedMacro { get; private set; }

    public Task PlayAsync(Macro macro, CancellationToken token)
    {
        PlayedMacro = macro;
        if (ThrowOnPlay) throw new InvalidOperationException("play error");
        return Task.CompletedTask;
    }
}

internal sealed class FakeRepo : IMacroRepository
{
    public Macro Saved { get; private set; } = new();
    public Macro ToLoad { get; set; } = new();

    public void Save(string path, Macro macro) => Saved = macro;
    public Macro Load(string path) => ToLoad;
}
