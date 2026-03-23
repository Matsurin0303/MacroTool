using System.Text.Json;

namespace MacroTool.Infrastructure.Windows.Persistence;

internal sealed class MacroFileDto
{
    public string Format { get; set; } = "MacroTool.Macro";
    public string FormatVersion { get; set; } = "1.0.0";
    public string SpecVersion { get; set; } = "Macro_v1.0.0";
    public MacroBodyDto Macro { get; set; } = new();
}

internal sealed class MacroBodyDto
{
    public string Name { get; set; } = "";
    public List<StepDto> Steps { get; set; } = new();
}

internal sealed class StepDto
{
    public int Order { get; set; }
    public string Label { get; set; } = "";
    public string Comment { get; set; } = "";
    public ActionEnvelopeDto Action { get; set; } = new();
}

internal sealed class ActionEnvelopeDto
{
    public string Type { get; set; } = "";
    public JsonElement Data { get; set; }
}

internal sealed class PathData
{
    public string Path { get; set; } = "";
}

internal sealed class WaitData
{
    public int WaitingMs { get; set; }
}

internal sealed class GoToTargetDto
{
    public string Kind { get; set; } = "";
    public string? Label { get; set; }
}

internal sealed class RectDto
{
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}

internal sealed class SearchAreaDto
{
    public string Kind { get; set; } = "";
    public RectDto? Rect { get; set; }
}

internal sealed class BitmapSourceDto
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
}

internal sealed class MouseClickData
{
    public string MouseButton { get; set; } = "";
    public string ClickType { get; set; } = "";
    public bool Relative { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

internal sealed class MouseMoveData
{
    public bool Relative { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }
    public int DurationMs { get; set; }
}

internal sealed class MouseWheelData
{
    public string Orientation { get; set; } = "";
    public int Value { get; set; }
}

internal sealed class KeyPressData
{
    public string KeyPressOption { get; set; } = "";
    public ushort KeyCode { get; set; }
    public int Count { get; set; }
}

internal sealed class WaitForPixelColorData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; } = "#FFFFFF";
    public int ColorTolerance { get; set; }
    public GoToTargetDto TrueGoTo { get; set; } = new();
    public int WaitingMs { get; set; }
    public GoToTargetDto FalseGoTo { get; set; } = new();
}

internal sealed class WaitForTextInputData
{
    public string TextToWaitFor { get; set; } = "";
    public GoToTargetDto TrueGoTo { get; set; } = new();
    public int WaitingMs { get; set; }
    public GoToTargetDto FalseGoTo { get; set; } = new();
}

internal sealed class FindImageData
{
    public SearchAreaDto SearchArea { get; set; } = new();
    public BitmapSourceDto BitmapSource { get; set; } = new();
    public int Tolerance { get; set; }
    public bool MouseActionEnabled { get; set; }
    public string MouseAction { get; set; } = "";
    public string MousePosition { get; set; } = "";
    public bool SaveCoordinateEnabled { get; set; }
    public string? SaveXVariable { get; set; }
    public string? SaveYVariable { get; set; }
    public GoToTargetDto TrueGoTo { get; set; } = new();
    public int WaitingMs { get; set; }
    public GoToTargetDto FalseGoTo { get; set; } = new();
}

internal sealed class FindTextOcrData
{
    public string TextToSearchFor { get; set; } = "";
    public string Language { get; set; } = "";
    public SearchAreaDto SearchArea { get; set; } = new();
    public bool MouseActionEnabled { get; set; }
    public string MouseAction { get; set; } = "";
    public string MousePosition { get; set; } = "";
    public bool SaveCoordinateEnabled { get; set; }
    public string? SaveXVariable { get; set; }
    public string? SaveYVariable { get; set; }
    public GoToTargetDto TrueGoTo { get; set; } = new();
    public int WaitingMs { get; set; }
    public GoToTargetDto FalseGoTo { get; set; } = new();
}

internal sealed class RepeatData
{
    public string StartLabel { get; set; } = "";
    public string ConditionKind { get; set; } = "";
    public int Seconds { get; set; }
    public int Repetitions { get; set; }
    public string UntilTime { get; set; } = "00:00:00";
    public GoToTargetDto FinishGoTo { get; set; } = new();
}

internal sealed class GoToData
{
    public GoToTargetDto GoTo { get; set; } = new();
}

internal sealed class IfData
{
    public string VariableName { get; set; } = "";
    public string Condition { get; set; } = "";
    public string Value { get; set; } = "";
    public GoToTargetDto TrueGoTo { get; set; } = new();
    public GoToTargetDto FalseGoTo { get; set; } = new();
}