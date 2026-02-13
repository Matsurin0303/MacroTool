namespace MacroTool.Application.Abstractions;

public interface IRecorder
{
    event EventHandler<RecordedAction>? ActionRecorded;

    bool Start();
    void Stop();

    bool IsRecording { get; }
}
