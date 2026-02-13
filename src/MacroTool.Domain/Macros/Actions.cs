using System.Text.Json.Serialization;

namespace MacroTool.Domain.Macros;

/// <summary>
/// マクロの1アクション。
/// v1.0 の機能一覧（将来実装予定を除く）をDomainに表現する。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MouseClickAction), "MouseClick")]
[JsonDerivedType(typeof(MouseMoveAction), "MouseMove")]
[JsonDerivedType(typeof(MouseWheelAction), "MouseWheel")]
[JsonDerivedType(typeof(KeyPressAction), "KeyPress")]
[JsonDerivedType(typeof(WaitTimeAction), "Wait")]
[JsonDerivedType(typeof(WaitForPixelColorAction), "WaitForPixelColor")]
[JsonDerivedType(typeof(WaitForScreenChangeAction), "WaitForScreenChange")]
[JsonDerivedType(typeof(WaitForTextInputAction), "WaitForTextInput")]
[JsonDerivedType(typeof(FindImageAction), "FindImage")]
[JsonDerivedType(typeof(FindTextOcrAction), "FindTextOcr")]
[JsonDerivedType(typeof(RepeatAction), "Repeat")]
[JsonDerivedType(typeof(GoToAction), "GoTo")]
[JsonDerivedType(typeof(IfAction), "If")]
[JsonDerivedType(typeof(EmbedMacroFileAction), "EmbedMacroFile")]
[JsonDerivedType(typeof(ExecuteProgramAction), "ExecuteProgram")]
public abstract record MacroAction
{
    [JsonIgnore]
    public abstract string Kind { get; }

    [JsonIgnore]
    public abstract string DisplayValue { get; }
}

// ===== Record / Edit =====

public sealed record MouseClickAction : MacroAction
{
    public MouseButton Button { get; set; } = MouseButton.Left;
    public MouseClickType Action { get; set; } = MouseClickType.Click;

    /// <summary>ON: 相対座標 / OFF: 絶対座標</summary>
    public bool Relative { get; set; } = false;

    public int X { get; set; }
    public int Y { get; set; }

    public override string Kind => "MouseClick";
    public override string DisplayValue
        => $"{Action} {Button} {(Relative ? "rel" : "abs")} ({X},{Y})";
}

public sealed record MouseMoveAction : MacroAction
{
    public bool Relative { get; set; } = false;
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }
    public int DurationMs { get; set; } = 0;

    public override string Kind => "MouseMove";
    public override string DisplayValue
        => $"{(Relative ? "rel" : "abs")} ({StartX},{StartY})->({EndX},{EndY}) {DurationMs}ms";
}

public sealed record MouseWheelAction : MacroAction
{
    public WheelOrientation Orientation { get; set; } = WheelOrientation.Vertical;
    public int Value { get; set; } = 120;

    public override string Kind => "MouseWheel";
    public override string DisplayValue => $"{Orientation} {Value}";
}

public sealed record KeyPressAction : MacroAction
{
    public KeyPressOption Option { get; set; } = KeyPressOption.Press;
    public VirtualKey Key { get; set; } = new(0);
    public int Count { get; set; } = 1;

    public override string Kind => "KeyPress";
    public override string DisplayValue => $"{Option} VK={Key.Code} x{Count}";
}

// ===== Wait =====

public sealed record WaitTimeAction : MacroAction
{
    public int Milliseconds { get; set; } = 0;

    public override string Kind => "Wait";
    public override string DisplayValue => $"{Milliseconds} ms";
}

public sealed record WaitForPixelColorAction : MacroAction
{
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>#RRGGBB</summary>
    public string ColorHex { get; set; } = "#FFFFFF";

    /// <summary>0-100</summary>
    public int TolerancePercent { get; set; } = 0;

    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };

    /// <summary>0以下なら無期限</summary>
    public int TimeoutMs { get; set; } = 0;

    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "WaitForPixelColor";
    public override string DisplayValue
        => $"({X},{Y}) {ColorHex} tol={TolerancePercent}% timeout={TimeoutMs}ms";
}

public sealed record WaitForScreenChangeAction : MacroAction
{
    public SearchArea SearchArea { get; set; } = new();

    public bool MouseAction { get; set; } = false;
    public MouseActionBehavior MouseActionBehavior { get; set; } = MouseActionBehavior.Positioning;

    public bool SaveCoordinate { get; set; } = false;
    public string SaveXVariable { get; set; } = "";
    public string SaveYVariable { get; set; } = "";

    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };
    public int TimeoutMs { get; set; } = 0;
    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "WaitForScreenChange";
    public override string DisplayValue
        => $"{SearchArea} timeout={TimeoutMs}ms";
}

public sealed record WaitForTextInputAction : MacroAction
{
    public string TextToWaitFor { get; set; } = "";
    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };
    public int TimeoutMs { get; set; } = 0;
    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "WaitForTextInput";
    public override string DisplayValue
        => $"\"{TextToWaitFor}\" timeout={TimeoutMs}ms";
}

// ===== Detection =====

public sealed record FindImageAction : MacroAction
{
    public SearchArea SearchArea { get; set; } = new();
    public int ColorTolerancePercent { get; set; } = 0;

    public ImageTemplate Template { get; set; } = new();

    public bool MouseAction { get; set; } = false;
    public MouseActionBehavior MouseActionBehavior { get; set; } = MouseActionBehavior.Positioning;
    public MousePosition MousePosition { get; set; } = MousePosition.Center;

    public bool SaveCoordinate { get; set; } = false;
    public string SaveXVariable { get; set; } = "";
    public string SaveYVariable { get; set; } = "";

    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };
    public int TimeoutMs { get; set; } = 0;
    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "FindImage";
    public override string DisplayValue
        => $"{SearchArea} tol={ColorTolerancePercent}% timeout={TimeoutMs}ms";
}

public sealed record FindTextOcrAction : MacroAction
{
    public string TextToSearchFor { get; set; } = "";
    public OcrLanguage Language { get; set; } = OcrLanguage.English;
    public SearchArea SearchArea { get; set; } = new();

    public bool MouseAction { get; set; } = false;
    public MouseActionBehavior MouseActionBehavior { get; set; } = MouseActionBehavior.Positioning;
    public MousePosition MousePosition { get; set; } = MousePosition.Center;

    public bool SaveCoordinate { get; set; } = false;
    public string SaveXVariable { get; set; } = "";
    public string SaveYVariable { get; set; } = "";

    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };
    public int TimeoutMs { get; set; } = 0;
    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "FindTextOcr";
    public override string DisplayValue
        => $"\"{TextToSearchFor}\" {Language} {SearchArea} timeout={TimeoutMs}ms";
}

// ===== Control Flow =====

public sealed record RepeatAction : MacroAction
{
    /// <summary>繰り返し範囲の開始ラベル</summary>
    public string StartLabel { get; set; } = "";

    public RepeatCondition Condition { get; set; } = new();

    /// <summary>Repeat終了後の遷移</summary>
    public GoToTarget AfterRepeatGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "Repeat";
    public override string DisplayValue
        => $"label={StartLabel} {Condition} -> {AfterRepeatGoTo}";
}

public sealed record GoToAction : MacroAction
{
    public GoToTarget Target { get; set; } = new() { Kind = GoToKind.Next };
    public override string Kind => "GoTo";
    public override string DisplayValue => Target.ToString();
}

public sealed record IfAction : MacroAction
{
    public string VariableName { get; set; } = "";
    public IfConditionKind Condition { get; set; } = IfConditionKind.ValueDefined;
    public string Value { get; set; } = "";

    public GoToTarget IfTrueGoTo { get; set; } = new() { Kind = GoToKind.Next };
    public GoToTarget IfFalseGoTo { get; set; } = new() { Kind = GoToKind.Next };

    public override string Kind => "If";
    public override string DisplayValue
        => $"{VariableName} {Condition} \"{Value}\"";
}

public sealed record EmbedMacroFileAction : MacroAction
{
    public string MacroFilePath { get; set; } = "";
    public override string Kind => "EmbedMacroFile";
    public override string DisplayValue => MacroFilePath;
}

public sealed record ExecuteProgramAction : MacroAction
{
    public string ProgramPath { get; set; } = "";
    public override string Kind => "ExecuteProgram";
    public override string DisplayValue => ProgramPath;
}
