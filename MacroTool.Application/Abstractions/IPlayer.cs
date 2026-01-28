using System.Threading;
using System.Threading.Tasks;
using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public interface IPlayer
{
    Task PlayAsync(Macro macro, CancellationToken token);
}
