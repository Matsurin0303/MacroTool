using System;

namespace MacroTool.Application.Abstractions;

public sealed class StepExecutingEventArgs : EventArgs
{
    public int StepIndex { get; }

    public StepExecutingEventArgs(int stepIndex)
        => StepIndex = stepIndex;
}
