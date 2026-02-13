namespace MacroTool.Domain.Macros;

/// <summary>
/// マクロの1行（ステップ）。
/// v1.0 では「待機」もアクションとして表現するため、Delayは持たない。
/// </summary>
public sealed record MacroStep
{
    public MacroAction Action { get; }
    public string Label { get; }
    public string Comment { get; }

    public MacroStep(MacroAction action, string? label = "", string? comment = "")
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Label = label ?? string.Empty;
        Comment = comment ?? string.Empty;
    }
}
