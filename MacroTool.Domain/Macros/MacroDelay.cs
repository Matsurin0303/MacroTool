using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroTool.Domain.Macros;

public readonly record struct MacroDelay(int Milliseconds)
{
    public int TotalMilliseconds => Milliseconds < 0 ? 0 : Milliseconds;

    public static MacroDelay FromMilliseconds(int ms)
        => new(ms < 0 ? 0 : ms);

    public static MacroDelay Zero => new(0);
}
