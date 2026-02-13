using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public interface IMacroRepository
{
    void Save(string path, Macro macro);
    Macro Load(string path);
}
