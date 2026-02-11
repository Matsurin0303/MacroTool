namespace MacroTool.Domain.Macros;

public sealed class MacroStep
{
    public MacroDelay Delay { get; }
    public MacroAction Action { get; }

    public string Label { get; }
    public string Comment { get; }

    public MacroStep(MacroDelay delay, MacroAction action, string label = "", string comment = "")
    {
        Delay = delay;
        Action = action;
        Label = label ?? "";
        Comment = comment ?? "";
    }
}
