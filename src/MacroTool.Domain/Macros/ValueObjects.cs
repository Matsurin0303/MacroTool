using System.ComponentModel;

namespace MacroTool.Domain.Macros;

// ===== basic value objects =====

public readonly record struct VirtualKey(ushort Code);

// ===== enums =====

public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

public enum MouseClickType
{
    Click,
    DoubleClick,
    Down,
    Up
}

public enum WheelOrientation
{
    Vertical,
    Horizontal
}

public enum KeyPressOption
{
    Press,
    Down,
    Up
}

public enum SearchAreaKind
{
    EntireDesktop,
    AreaOfDesktop,
    FocusedWindow,
    AreaOfFocusedWindow
}

public enum MouseActionBehavior
{
    Positioning,
    LeftClick,
    RightClick,
    MiddleClick,
    DoubleClick
}

public enum MousePosition
{
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum OcrLanguage
{
    English,
    Japanese
}

public enum GoToKind
{
    Start,
    End,
    Next,
    Label
}

public enum RepeatConditionKind
{
    Seconds,
    Repetitions,
    Until,
    Infinite
}

public enum IfConditionKind
{
    TextEquals,
    TextBeginsWith,
    TextEndsWith,
    TextIncludes,

    TextNotEquals,
    TextNotBeginsWith,
    TextNotEndsWith,
    TextNotIncludes,

    TextLongerThan,
    TextShorterThan,

    ValueHigherThan,
    ValueLowerThan,
    ValueHigherOrEqual,
    ValueLowerOrEqual,

    RegEx,
    ValueDefined
}

public enum ImageTemplateKind
{
    FilePath,
    EmbeddedPng
}

// ===== expandable objects (PropertyGrid friendly) =====

/// <summary>
/// GoTo の遷移先。
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record GoToTarget
{
    public GoToKind Kind { get; set; } = GoToKind.Next;

    /// <summary>
    /// Kind=Label のときに使用する。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    // ---- factories (Form1 が GoToTarget.Next() を呼ぶため) ----
    public static GoToTarget Start() => new() { Kind = GoToKind.Start };
    public static GoToTarget End() => new() { Kind = GoToKind.End };
    public static GoToTarget Next() => new() { Kind = GoToKind.Next };
    public static GoToTarget ToLabel(string label) => new()
    {
        Kind = GoToKind.Label,
        Label = label ?? string.Empty
    };

public override string ToString()
        => Kind == GoToKind.Label ? $"Label:{Label}" : Kind.ToString();
}

/// <summary>
/// 検索/監視対象領域。
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record SearchArea
{
    public SearchAreaKind Kind { get; set; } = SearchAreaKind.EntireDesktop;
    /// <summary>
    /// Kind=AreaOfDesktop では「スクリーン座標」、Kind=AreaOfFocusedWindow では「ウィンドウ左上からの相対座標」を想定。
    /// </summary>
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }

    public override string ToString()
        => Kind switch
        {
            SearchAreaKind.AreaOfDesktop or SearchAreaKind.AreaOfFocusedWindow => $"{Kind} ({X1},{Y1})-({X2},{Y2})",
            _ => Kind.ToString()
        };
}

/// <summary>
/// Repeat の条件設定。
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record RepeatCondition
{
    public RepeatConditionKind Kind { get; set; } = RepeatConditionKind.Repetitions;

    public int Seconds { get; set; } = 0;
    public int Repetitions { get; set; } = 1;

    /// <summary>
    /// HH:mm:ss（ローカル）
    /// </summary>
    public string UntilTime { get; set; } = "00:00:00";

    public override string ToString()
        => Kind switch
        {
            RepeatConditionKind.Seconds => $"{Seconds} sec",
            RepeatConditionKind.Repetitions => $"{Repetitions} times",
            RepeatConditionKind.Until => $"until {UntilTime}",
            RepeatConditionKind.Infinite => "infinite",
            _ => Kind.ToString()
        };
}

/// <summary>
/// Find image のテンプレート指定。
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record ImageTemplate
{
    public ImageTemplateKind Kind { get; set; } = ImageTemplateKind.FilePath;
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Kind=EmbeddedPng のときに使用。
    /// （JSONではBase64化される）
    /// </summary>
    public byte[]? PngBytes { get; set; }

    public override string ToString()
        => Kind switch
        {
            ImageTemplateKind.FilePath => string.IsNullOrWhiteSpace(FilePath) ? "(file not set)" : FilePath,
            ImageTemplateKind.EmbeddedPng => PngBytes is null ? "(embedded not set)" : $"Embedded ({PngBytes.Length} bytes)",
            _ => Kind.ToString()
        };
}
