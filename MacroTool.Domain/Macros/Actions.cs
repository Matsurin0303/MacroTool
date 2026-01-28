using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroTool.Domain.Macros;

public abstract record MacroAction
{
    public abstract string Kind { get; }
    public abstract string DisplayValue { get; }
}

public sealed record MouseClick(ScreenPoint Point, MouseButton Button) : MacroAction
{
    public override string Kind => "MouseClick";
    public override string DisplayValue => $"({Point.X},{Point.Y}) {Button}";
}

public sealed record KeyDown(VirtualKey Key) : MacroAction
{
    public override string Kind => "KeyDown";
    public override string DisplayValue => $"VK={Key.Code}";
}

public sealed record KeyUp(VirtualKey Key) : MacroAction
{
    public override string Kind => "KeyUp";
    public override string DisplayValue => $"VK={Key.Code}";
}
