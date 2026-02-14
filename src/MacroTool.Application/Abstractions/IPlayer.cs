using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public interface IPlayer
{
    event EventHandler<StepExecutingEventArgs>? StepExecuting;
    Task PlayAsync(Macro macro, CancellationToken token);
}
