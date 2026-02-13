using MacroTool.Domain.Macros;

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

    private static string ToIconKey(MacroAction action)
    {
        return action switch
        {
            MouseClickAction or MouseMoveAction or MouseWheelAction => "Mouse",
            KeyPressAction => "Keyboard",

            WaitTimeAction or WaitForPixelColorAction or WaitForScreenChangeAction or WaitForTextInputAction => "Wait",

            FindImageAction or FindTextOcrAction => "Image",

            _ => "Misc"
        };
    }
}
