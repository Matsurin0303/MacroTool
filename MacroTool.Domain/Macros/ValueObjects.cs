using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroTool.Domain.Macros;

public readonly record struct ScreenPoint(int X, int Y);

public readonly record struct VirtualKey(ushort Code);

public enum MouseButton
{
    Left,
    Right
}
