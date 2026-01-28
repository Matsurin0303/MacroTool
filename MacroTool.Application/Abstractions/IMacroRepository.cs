using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroTool.Domain;
using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public interface IMacroRepository
{
    void Save(string path, Macro macro);
    Macro Load(string path);
}
