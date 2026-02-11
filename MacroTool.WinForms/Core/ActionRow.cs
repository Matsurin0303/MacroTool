using MacroTool.Domain.Macros;
using DomainKeyDown = MacroTool.Domain.Macros.KeyDown;
using DomainKeyUp = MacroTool.Domain.Macros.KeyUp;
using DomainMacroAction = MacroTool.Domain.Macros.MacroAction;
using DomainMouseClick = MacroTool.Domain.Macros.MouseClick;

namespace MacroTool.WinForms.Core;

public sealed class ActionRow
{
    public int No { get; set; }
    public string IconKey { get; set; } = "";
    public string Action { get; set; } = "";
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Comment { get; set; } = "";

    public static ActionRow FromDomain(int no, MacroStep step)
    {
        return new ActionRow
        {
            No = no,
            IconKey = ToIconKey(step.Action),
            Action = step.Action.Kind,
            Value = step.Action.DisplayValue,
            Label = step.Label,
            Comment = step.Comment
        };
    }

    private static string ToIconKey(DomainMacroAction action)
    {
        return action switch
        {
            DomainMouseClick => "Mouse",
            DomainKeyDown or DomainKeyUp => "Keyboard",
            _ => "Misc"
        };
    }
}
