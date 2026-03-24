using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacroTool.Infrastructure.Windows.Persistence;

/// <summary>
/// Macro_v1.0.0 仕様の JSON 永続化。
/// 旧 version ベース形式は本版では読み込まない。
/// </summary>
public sealed class JsonMacroRepository : IMacroRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public void Save(string path, Macro macro)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        if (macro is null)
            throw new ArgumentNullException(nameof(macro));

        var dto = new MacroFileDto
        {
            Format = "MacroTool.Macro",
            FormatVersion = "1.0.0",
            SpecVersion = "Macro_v1.0.0",
            Macro = new MacroBodyDto
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Steps = macro.Steps
                    .Select((s, i) => new StepDto
                    {
                        Order = i,
                        Label = s.Label ?? string.Empty,
                        Comment = s.Comment ?? string.Empty,
                        Action = ToActionEnvelope(s.Action)
                    })
                    .ToList()
            }
        };

        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public Macro Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var json = File.ReadAllText(path);

        // 旧 version 形式は本版未対応
        if (json.Contains("\"version\"", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("旧版マクロ形式は本版では読み込めません。Macro_v1.0.0 形式へ変換してください。");

        var dto = JsonSerializer.Deserialize<MacroFileDto>(json, JsonOptions)
                  ?? throw new InvalidDataException("Invalid macro file.");

        ValidateRoot(dto);

        var macro = new Macro();
        foreach (var s in dto.Macro.Steps.OrderBy(x => x.Order))
        {
            if (s.Action is null)
                throw new InvalidDataException("Step.Action is null.");

            var action = FromActionEnvelope(s.Action);
            macro.AddStep(new MacroStep(action, s.Label ?? string.Empty, s.Comment ?? string.Empty));
        }

        return macro;
    }

    private static void ValidateRoot(MacroFileDto dto)
    {
        if (!string.Equals(dto.Format, "MacroTool.Macro", StringComparison.Ordinal))
            throw new InvalidDataException("format が不正です。");

        if (!string.Equals(dto.FormatVersion, "1.0.0", StringComparison.Ordinal))
            throw new InvalidDataException("formatVersion が不正です。");

        if (!string.Equals(dto.SpecVersion, "Macro_v1.0.0", StringComparison.Ordinal))
            throw new InvalidDataException("specVersion が不正です。");

        if (dto.Macro is null)
            throw new InvalidDataException("macro が存在しません。");

        if (dto.Macro.Steps is null)
            throw new InvalidDataException("macro.steps が存在しません。");
    }

    private static ActionEnvelopeDto ToActionEnvelope(MacroAction action)
    {
        return action switch
        {
            MouseClickAction a => Envelope("MouseClick", ToMouseClickData(a)),
            MouseMoveAction a => Envelope("MouseMove", ToMouseMoveData(a)),
            MouseWheelAction a => Envelope("MouseWheel", ToMouseWheelData(a)),
            KeyPressAction a => Envelope("KeyPress", ToKeyPressData(a)),
            WaitTimeAction a => Envelope("Wait", ToWaitData(a)),
            WaitForPixelColorAction a => Envelope("WaitForPixelColor", ToWaitForPixelColorData(a)),
            WaitForTextInputAction a => Envelope("WaitForTextInput", ToWaitForTextInputData(a)),
            FindImageAction a => Envelope("FindImage", ToFindImageData(a)),
            FindTextOcrAction a => Envelope("FindTextOcr", ToFindTextOcrData(a)),
            RepeatAction a => Envelope("Repeat", ToRepeatData(a)),
            GoToAction a => Envelope("GoTo", ToGoToData(a)),
            IfAction a => Envelope("If", ToIfData(a)),
            EmbedMacroFileAction a => Envelope("EmbedMacroFile", new PathData { Path = a.MacroFilePath }),
            ExecuteProgramAction a => Envelope("ExecuteProgram", new PathData { Path = a.ProgramPath }),
            _ => throw new InvalidDataException($"Unsupported action type: {action.GetType().Name}")
        };
    }

    private static ActionEnvelopeDto Envelope<T>(string type, T data)
    {
        return new ActionEnvelopeDto
        {
            Type = type,
            Data = JsonSerializer.SerializeToElement(data, JsonOptions)
        };
    }

    private static MacroAction FromActionEnvelope(ActionEnvelopeDto dto)
    {
        return dto.Type switch
        {
            "MouseClick" => ToMouseClickAction(ReadData<MouseClickData>(dto.Data, "MouseClick")),
            "MouseMove" => ToMouseMoveAction(ReadData<MouseMoveData>(dto.Data, "MouseMove")),
            "MouseWheel" => ToMouseWheelAction(ReadData<MouseWheelData>(dto.Data, "MouseWheel")),
            "KeyPress" => ToKeyPressAction(ReadData<KeyPressData>(dto.Data, "KeyPress")),
            "Wait" => ToWaitAction(ReadData<WaitData>(dto.Data, "Wait")),
            "WaitForPixelColor" => ToWaitForPixelColorAction(ReadData<WaitForPixelColorData>(dto.Data, "WaitForPixelColor")),
            "WaitForTextInput" => ToWaitForTextInputAction(ReadData<WaitForTextInputData>(dto.Data, "WaitForTextInput")),
            "FindImage" => ToFindImageAction(ReadData<FindImageData>(dto.Data, "FindImage")),
            "FindTextOcr" => ToFindTextOcrAction(ReadData<FindTextOcrData>(dto.Data, "FindTextOcr")),
            "Repeat" => ToRepeatAction(ReadData<RepeatData>(dto.Data, "Repeat")),
            "GoTo" => ToGoToAction(ReadData<GoToData>(dto.Data, "GoTo")),
            "If" => ToIfAction(ReadData<IfData>(dto.Data, "If")),
            "EmbedMacroFile" => new EmbedMacroFileAction
            {
                MacroFilePath = ReadData<PathData>(dto.Data, "EmbedMacroFile").Path
            },
            "ExecuteProgram" => new ExecuteProgramAction
            {
                ProgramPath = ReadData<PathData>(dto.Data, "ExecuteProgram").Path
            },

            "WaitForScreenChange" => throw new InvalidDataException("このマクロファイルには本版対象外の WaitForScreenChange が含まれているため読み込めません。"),
            _ => throw new InvalidDataException($"Unsupported action.type: {dto.Type}")
        };
    }

    private static T ReadData<T>(JsonElement data, string typeName)
    {
        return data.Deserialize<T>(JsonOptions)
               ?? throw new InvalidDataException($"{typeName} data is invalid.");
    }

    // ===== Save: Domain -> DTO =====

    private static MouseClickData ToMouseClickData(MouseClickAction action)
        => new()
        {
            MouseButton = ToMouseButtonToken(action.Button),
            ClickType = ToMouseClickTypeToken(action.Action),
            Relative = action.Relative,
            X = action.X,
            Y = action.Y
        };

    private static MouseMoveData ToMouseMoveData(MouseMoveAction action)
        => new()
        {
            Relative = action.Relative,
            StartX = action.StartX,
            StartY = action.StartY,
            EndX = action.EndX,
            EndY = action.EndY,
            DurationMs = action.DurationMs
        };

    private static MouseWheelData ToMouseWheelData(MouseWheelAction action)
        => new()
        {
            Orientation = ToWheelOrientationToken(action.Orientation),
            Value = action.Value
        };

    private static KeyPressData ToKeyPressData(KeyPressAction action)
        => new()
        {
            KeyPressOption = ToKeyPressOptionToken(action.Option),
            KeyCode = action.Key.Code,
            Count = action.Count
        };

    private static WaitData ToWaitData(WaitTimeAction action)
        => new()
        {
            WaitingMs = action.Milliseconds
        };

    private static WaitForPixelColorData ToWaitForPixelColorData(WaitForPixelColorAction action)
        => new()
        {
            X = action.X,
            Y = action.Y,
            Color = action.ColorHex,
            ColorTolerance = action.TolerancePercent,
            TrueGoTo = ToGoToTargetDto(action.TrueGoTo),
            WaitingMs = action.TimeoutMs,
            FalseGoTo = ToGoToTargetDto(action.FalseGoTo)
        };

    private static WaitForTextInputData ToWaitForTextInputData(WaitForTextInputAction action)
        => new()
        {
            TextToWaitFor = action.TextToWaitFor,
            TrueGoTo = ToGoToTargetDto(action.TrueGoTo),
            WaitingMs = action.TimeoutMs,
            FalseGoTo = ToGoToTargetDto(action.FalseGoTo)
        };

    private static FindImageData ToFindImageData(FindImageAction action)
        => new()
        {
            SearchArea = ToSearchAreaDto(action.SearchArea),
            BitmapSource = ToBitmapSourceDto(action.Template),
            Tolerance = action.ColorTolerancePercent,
            MouseActionEnabled = action.MouseActionEnabled,
            MouseAction = ToMouseActionBehaviorToken(action.MouseAction),
            MousePosition = ToMousePositionToken(action.MousePosition),
            SaveCoordinateEnabled = action.SaveCoordinateEnabled,
            SaveXVariable = action.SaveXVariable,
            SaveYVariable = action.SaveYVariable,
            TrueGoTo = ToGoToTargetDto(action.TrueGoTo),
            WaitingMs = action.TimeoutMs,
            FalseGoTo = ToGoToTargetDto(action.FalseGoTo)
        };

    private static FindTextOcrData ToFindTextOcrData(FindTextOcrAction action)
        => new()
        {
            TextToSearchFor = action.TextToSearchFor,
            Language = ToOcrLanguageToken(action.Language),
            SearchArea = ToSearchAreaDto(action.SearchArea),
            MouseActionEnabled = action.MouseActionEnabled,
            MouseAction = ToMouseActionBehaviorToken(action.MouseAction),
            MousePosition = ToMousePositionToken(action.MousePosition),
            SaveCoordinateEnabled = action.SaveCoordinateEnabled,
            SaveXVariable = action.SaveXVariable,
            SaveYVariable = action.SaveYVariable,
            TrueGoTo = ToGoToTargetDto(action.TrueGoTo),
            WaitingMs = action.TimeoutMs,
            FalseGoTo = ToGoToTargetDto(action.FalseGoTo)
        };

    private static RepeatData ToRepeatData(RepeatAction action)
        => new()
        {
            StartLabel = action.StartLabel,
            ConditionKind = ToRepeatConditionKindToken(action.Condition.Kind),
            Seconds = action.Condition.Seconds,
            Repetitions = action.Condition.Repetitions,
            UntilTime = action.Condition.UntilTime,
            FinishGoTo = ToGoToTargetDto(action.AfterRepeatGoTo)
        };

    private static GoToData ToGoToData(GoToAction action)
        => new()
        {
            GoTo = ToGoToTargetDto(action.Target)
        };

    private static IfData ToIfData(IfAction action)
        => new()
        {
            VariableName = action.VariableName,
            Condition = ToIfConditionToken(action.Condition),
            Value = action.Value,
            TrueGoTo = ToGoToTargetDto(action.TrueGoTo),
            FalseGoTo = ToGoToTargetDto(action.FalseGoTo)
        };

    private static GoToTargetDto ToGoToTargetDto(GoToTarget target)
        => new()
        {
            Kind = ToGoToKindToken(target.Kind),
            Label = target.Kind == GoToKind.Label ? target.Label : null
        };

    private static SearchAreaDto ToSearchAreaDto(SearchArea area)
        => new()
        {
            Kind = ToSearchAreaKindToken(area.Kind),
            Rect = area.Kind is SearchAreaKind.AreaOfDesktop or SearchAreaKind.AreaOfFocusedWindow
                ? new RectDto
                {
                    X1 = area.X1,
                    Y1 = area.Y1,
                    X2 = area.X2,
                    Y2 = area.Y2
                }
                : null
        };

    private static BitmapSourceDto ToBitmapSourceDto(ImageTemplate template)
    {
        return template.Kind switch
        {
            ImageTemplateKind.FilePath => new BitmapSourceDto
            {
                Kind = "FilePath",
                Value = template.FilePath
            },
            ImageTemplateKind.CapturedBitmap => new BitmapSourceDto
            {
                Kind = "CapturedBitmap",
                Value = template.PngBytes is null ? string.Empty : Convert.ToBase64String(template.PngBytes)
            },
            _ => throw new InvalidDataException($"Unsupported ImageTemplateKind: {template.Kind}")
        };
    }

    // ===== Load: DTO -> Domain =====

    private static MouseClickAction ToMouseClickAction(MouseClickData dto)
        => new()
        {
            Button = ParseMouseButtonToken(dto.MouseButton),
            Action = ParseMouseClickTypeToken(dto.ClickType),
            ClickType = ParseMouseClickTypeToken(dto.ClickType),
            Relative = dto.Relative,
            X = dto.X,
            Y = dto.Y
        };

    private static MouseMoveAction ToMouseMoveAction(MouseMoveData dto)
        => new()
        {
            Relative = dto.Relative,
            StartX = dto.StartX,
            StartY = dto.StartY,
            EndX = dto.EndX,
            EndY = dto.EndY,
            DurationMs = dto.DurationMs
        };

    private static MouseWheelAction ToMouseWheelAction(MouseWheelData dto)
        => new()
        {
            Orientation = ParseWheelOrientationToken(dto.Orientation),
            Value = dto.Value
        };

    private static KeyPressAction ToKeyPressAction(KeyPressData dto)
        => new()
        {
            Option = ParseKeyPressOptionToken(dto.KeyPressOption),
            Key = new VirtualKey(dto.KeyCode),
            Count = dto.Count
        };

    private static WaitTimeAction ToWaitAction(WaitData dto)
        => new()
        {
            Milliseconds = dto.WaitingMs
        };

    private static WaitForPixelColorAction ToWaitForPixelColorAction(WaitForPixelColorData dto)
        => new()
        {
            X = dto.X,
            Y = dto.Y,
            ColorHex = dto.Color,
            TolerancePercent = dto.ColorTolerance,
            TrueGoTo = ToGoToTarget(dto.TrueGoTo),
            TimeoutMs = dto.WaitingMs,
            FalseGoTo = ToGoToTarget(dto.FalseGoTo)
        };

    private static WaitForTextInputAction ToWaitForTextInputAction(WaitForTextInputData dto)
        => new()
        {
            TextToWaitFor = dto.TextToWaitFor,
            TrueGoTo = ToGoToTarget(dto.TrueGoTo),
            TimeoutMs = dto.WaitingMs,
            FalseGoTo = ToGoToTarget(dto.FalseGoTo)
        };

    private static FindImageAction ToFindImageAction(FindImageData dto)
        => new()
        {
            SearchArea = ToSearchArea(dto.SearchArea),
            Template = ToImageTemplate(dto.BitmapSource),
            ColorTolerancePercent = dto.Tolerance,
            MouseActionEnabled = dto.MouseActionEnabled,
            MouseAction = ParseMouseActionBehaviorToken(dto.MouseAction),
            MousePosition = ParseMousePositionToken(dto.MousePosition),
            SaveCoordinateEnabled = dto.SaveCoordinateEnabled,
            SaveXVariable = dto.SaveXVariable ?? "X",
            SaveYVariable = dto.SaveYVariable ?? "Y",
            TrueGoTo = ToGoToTarget(dto.TrueGoTo),
            TimeoutMs = dto.WaitingMs,
            FalseGoTo = ToGoToTarget(dto.FalseGoTo)
        };

    private static FindTextOcrAction ToFindTextOcrAction(FindTextOcrData dto)
        => new()
        {
            TextToSearchFor = dto.TextToSearchFor,
            Language = ParseOcrLanguageToken(dto.Language),
            SearchArea = ToSearchArea(dto.SearchArea),
            MouseActionEnabled = dto.MouseActionEnabled,
            MouseAction = ParseMouseActionBehaviorToken(dto.MouseAction),
            MousePosition = ParseMousePositionToken(dto.MousePosition),
            SaveCoordinateEnabled = dto.SaveCoordinateEnabled,
            SaveXVariable = dto.SaveXVariable ?? "X",
            SaveYVariable = dto.SaveYVariable ?? "Y",
            TrueGoTo = ToGoToTarget(dto.TrueGoTo),
            TimeoutMs = dto.WaitingMs,
            FalseGoTo = ToGoToTarget(dto.FalseGoTo)
        };

    private static RepeatAction ToRepeatAction(RepeatData dto)
        => new()
        {
            StartLabel = dto.StartLabel,
            Condition = new RepeatCondition
            {
                Kind = ParseRepeatConditionKindToken(dto.ConditionKind),
                Seconds = dto.Seconds,
                Repetitions = dto.Repetitions,
                UntilTime = dto.UntilTime
            },
            AfterRepeatGoTo = ToGoToTarget(dto.FinishGoTo)
        };

    private static GoToAction ToGoToAction(GoToData dto)
        => new()
        {
            Target = ToGoToTarget(dto.GoTo)
        };

    private static IfAction ToIfAction(IfData dto)
        => new()
        {
            VariableName = dto.VariableName,
            Condition = ParseIfConditionToken(dto.Condition),
            Value = dto.Value,
            TrueGoTo = ToGoToTarget(dto.TrueGoTo),
            FalseGoTo = ToGoToTarget(dto.FalseGoTo)
        };

    private static GoToTarget ToGoToTarget(GoToTargetDto dto)
    {
        var kind = ParseGoToKindToken(dto.Kind);
        return kind switch
        {
            GoToKind.Start => GoToTarget.Start(),
            GoToKind.End => GoToTarget.End(),
            GoToKind.Next => GoToTarget.Next(),
            GoToKind.Label => GoToTarget.ToLabel(dto.Label ?? string.Empty),
            _ => throw new InvalidDataException($"Invalid GoToTarget kind: {dto.Kind}")
        };
    }

    private static SearchArea ToSearchArea(SearchAreaDto dto)
    {
        var kind = ParseSearchAreaKindToken(dto.Kind);
        return new SearchArea
        {
            Kind = kind,
            X1 = dto.Rect?.X1 ?? 0,
            Y1 = dto.Rect?.Y1 ?? 0,
            X2 = dto.Rect?.X2 ?? 0,
            Y2 = dto.Rect?.Y2 ?? 0
        };
    }

    private static ImageTemplate ToImageTemplate(BitmapSourceDto dto)
    {
        return dto.Kind switch
        {
            "FilePath" => new ImageTemplate
            {
                Kind = ImageTemplateKind.FilePath,
                FilePath = dto.Value ?? string.Empty
            },
            "CapturedBitmap" => new ImageTemplate
            {
                Kind = ImageTemplateKind.CapturedBitmap,
                PngBytes = string.IsNullOrWhiteSpace(dto.Value) ? null : Convert.FromBase64String(dto.Value)
            },
            _ => throw new InvalidDataException($"Unsupported bitmapSource.kind: {dto.Kind}")
        };
    }

    // ===== Token mapping =====

    private static string ToMouseButtonToken(MouseButton value) => value switch
    {
        MouseButton.Left => "Left",
        MouseButton.Right => "Right",
        MouseButton.Middle => "Middle",
        MouseButton.SideButton1 => "SideButton1",
        MouseButton.SideButton2 => "SideButton2",
        _ => throw new InvalidDataException($"Unsupported MouseButton: {value}")
    };

    private static MouseButton ParseMouseButtonToken(string token) => token switch
    {
        "Left" => MouseButton.Left,
        "Right" => MouseButton.Right,
        "Middle" => MouseButton.Middle,
        "SideButton1" => MouseButton.SideButton1,
        "SideButton2" => MouseButton.SideButton2,
        _ => throw new InvalidDataException($"Invalid mouseButton: {token}")
    };

    private static string ToMouseClickTypeToken(MouseClickType value) => value switch
    {
        MouseClickType.Click => "Click",
        MouseClickType.DoubleClick => "DoubleClick",
        MouseClickType.Down => "Down",
        MouseClickType.Up => "Up",
        _ => throw new InvalidDataException($"Unsupported MouseClickType: {value}")
    };

    private static MouseClickType ParseMouseClickTypeToken(string token) => token switch
    {
        "Click" => MouseClickType.Click,
        "DoubleClick" => MouseClickType.DoubleClick,
        "Down" => MouseClickType.Down,
        "Up" => MouseClickType.Up,
        _ => throw new InvalidDataException($"Invalid clickType: {token}")
    };

    private static string ToWheelOrientationToken(WheelOrientation value) => value switch
    {
        WheelOrientation.Vertical => "Vertical",
        WheelOrientation.Horizontal => "Horizontal",
        _ => throw new InvalidDataException($"Unsupported WheelOrientation: {value}")
    };

    private static WheelOrientation ParseWheelOrientationToken(string token) => token switch
    {
        "Vertical" => WheelOrientation.Vertical,
        "Horizontal" => WheelOrientation.Horizontal,
        _ => throw new InvalidDataException($"Invalid orientation: {token}")
    };

    private static string ToKeyPressOptionToken(KeyPressOption value) => value switch
    {
        KeyPressOption.Press => "Press",
        KeyPressOption.Down => "Down",
        KeyPressOption.Up => "Up",
        _ => throw new InvalidDataException($"Unsupported KeyPressOption: {value}")
    };

    private static KeyPressOption ParseKeyPressOptionToken(string token) => token switch
    {
        "Press" => KeyPressOption.Press,
        "Down" => KeyPressOption.Down,
        "Up" => KeyPressOption.Up,
        _ => throw new InvalidDataException($"Invalid keyPressOption: {token}")
    };

    private static string ToSearchAreaKindToken(SearchAreaKind value) => value switch
    {
        SearchAreaKind.EntireDesktop => "EntireDesktop",
        SearchAreaKind.AreaOfDesktop => "AreaOfDesktop",
        SearchAreaKind.FocusedWindow => "FocusedWindow",
        SearchAreaKind.AreaOfFocusedWindow => "AreaOfFocusedWindow",
        _ => throw new InvalidDataException($"Unsupported SearchAreaKind: {value}")
    };

    private static SearchAreaKind ParseSearchAreaKindToken(string token) => token switch
    {
        "EntireDesktop" => SearchAreaKind.EntireDesktop,
        "AreaOfDesktop" => SearchAreaKind.AreaOfDesktop,
        "FocusedWindow" => SearchAreaKind.FocusedWindow,
        "AreaOfFocusedWindow" => SearchAreaKind.AreaOfFocusedWindow,
        _ => throw new InvalidDataException($"Invalid searchArea.kind: {token}")
    };

    private static string ToMouseActionBehaviorToken(MouseActionBehavior value) => value switch
    {
        MouseActionBehavior.Positioning => "Positioning",
        MouseActionBehavior.LeftClick => "LeftClick",
        MouseActionBehavior.RightClick => "RightClick",
        MouseActionBehavior.MiddleClick => "MiddleClick",
        MouseActionBehavior.DoubleClick => "DoubleClick",
        _ => throw new InvalidDataException($"Unsupported MouseActionBehavior: {value}")
    };

    private static MouseActionBehavior ParseMouseActionBehaviorToken(string token) => token switch
    {
        "Positioning" => MouseActionBehavior.Positioning,
        "LeftClick" => MouseActionBehavior.LeftClick,
        "RightClick" => MouseActionBehavior.RightClick,
        "MiddleClick" => MouseActionBehavior.MiddleClick,
        "DoubleClick" => MouseActionBehavior.DoubleClick,
        _ => throw new InvalidDataException($"Invalid mouseAction: {token}")
    };

    private static string ToMousePositionToken(MousePosition value) => value switch
    {
        MousePosition.Center => "Center",
        MousePosition.TopLeft => "TopLeft",
        MousePosition.TopRight => "TopRight",
        MousePosition.BottomLeft => "BottomLeft",
        MousePosition.BottomRight => "BottomRight",
        _ => throw new InvalidDataException($"Unsupported MousePosition: {value}")
    };

    private static MousePosition ParseMousePositionToken(string token) => token switch
    {
        "Center" => MousePosition.Center,
        "TopLeft" => MousePosition.TopLeft,
        "TopRight" => MousePosition.TopRight,
        "BottomLeft" => MousePosition.BottomLeft,
        "BottomRight" => MousePosition.BottomRight,
        _ => throw new InvalidDataException($"Invalid mousePosition: {token}")
    };

    private static string ToOcrLanguageToken(OcrLanguage value) => value switch
    {
        OcrLanguage.English => "English",
        OcrLanguage.Japanese => "Japanese",
        _ => throw new InvalidDataException($"Unsupported OcrLanguage: {value}")
    };

    private static OcrLanguage ParseOcrLanguageToken(string token) => token switch
    {
        "English" => OcrLanguage.English,
        "Japanese" => OcrLanguage.Japanese,
        _ => throw new InvalidDataException($"Invalid language: {token}")
    };

    private static string ToGoToKindToken(GoToKind value) => value switch
    {
        GoToKind.Start => "Start",
        GoToKind.End => "End",
        GoToKind.Next => "Next",
        GoToKind.Label => "Label",
        _ => throw new InvalidDataException($"Unsupported GoToKind: {value}")
    };

    private static GoToKind ParseGoToKindToken(string token) => token switch
    {
        "Start" => GoToKind.Start,
        "End" => GoToKind.End,
        "Next" => GoToKind.Next,
        "Label" => GoToKind.Label,
        _ => throw new InvalidDataException($"Invalid goTo.kind: {token}")
    };

    private static string ToRepeatConditionKindToken(RepeatConditionKind value) => value switch
    {
        RepeatConditionKind.Seconds => "Seconds",
        RepeatConditionKind.Repetitions => "Repetitions",
        RepeatConditionKind.Until => "Until",
        RepeatConditionKind.Infinite => "Infinite",
        _ => throw new InvalidDataException($"Unsupported RepeatConditionKind: {value}")
    };

    private static RepeatConditionKind ParseRepeatConditionKindToken(string token) => token switch
    {
        "Seconds" => RepeatConditionKind.Seconds,
        "Repetitions" => RepeatConditionKind.Repetitions,
        "Until" => RepeatConditionKind.Until,
        "Infinite" => RepeatConditionKind.Infinite,
        _ => throw new InvalidDataException($"Invalid repeat.conditionKind: {token}")
    };

    private static string ToIfConditionToken(IfConditionKind value) => value switch
    {
        IfConditionKind.TextEquals => "TextEquals",
        IfConditionKind.TextBeginsWith => "TextBeginsWith",
        IfConditionKind.TextEndsWith => "TextEndsWith",
        IfConditionKind.TextIncludes => "TextIncludes",
        IfConditionKind.TextNotEquals => "TextNotEquals",
        IfConditionKind.TextNotBeginsWith => "TextNotBeginsWith",
        IfConditionKind.TextNotEndsWith => "TextNotEndsWith",
        IfConditionKind.TextNotIncludes => "TextNotIncludes",
        IfConditionKind.TextLongerThan => "TextLongerThan",
        IfConditionKind.TextShorterThan => "TextShorterThan",
        IfConditionKind.ValueHigherThan => "ValueHigherThan",
        IfConditionKind.ValueLowerThan => "ValueLowerThan",
        IfConditionKind.ValueHigherOrEqual => "ValueHigherOrEqual",
        IfConditionKind.ValueLowerOrEqual => "ValueLowerOrEqual",
        IfConditionKind.ValueDefined => "ValueDefined",
        _ => throw new InvalidDataException($"Unsupported IfConditionKind: {value}")
    };

    private static IfConditionKind ParseIfConditionToken(string token) => token switch
    {
        "TextEquals" => IfConditionKind.TextEquals,
        "TextBeginsWith" => IfConditionKind.TextBeginsWith,
        "TextEndsWith" => IfConditionKind.TextEndsWith,
        "TextIncludes" => IfConditionKind.TextIncludes,
        "TextNotEquals" => IfConditionKind.TextNotEquals,
        "TextNotBeginsWith" => IfConditionKind.TextNotBeginsWith,
        "TextNotEndsWith" => IfConditionKind.TextNotEndsWith,
        "TextNotIncludes" => IfConditionKind.TextNotIncludes,
        "TextLongerThan" => IfConditionKind.TextLongerThan,
        "TextShorterThan" => IfConditionKind.TextShorterThan,
        "ValueHigherThan" => IfConditionKind.ValueHigherThan,
        "ValueLowerThan" => IfConditionKind.ValueLowerThan,
        "ValueHigherOrEqual" => IfConditionKind.ValueHigherOrEqual,
        "ValueLowerOrEqual" => IfConditionKind.ValueLowerOrEqual,
        "ValueDefined" => IfConditionKind.ValueDefined,
        "RegEx" => throw new InvalidDataException("このマクロファイルには本版対象外の If.RegEx が含まれているため読み込めません。"),
        _ => throw new InvalidDataException($"Invalid if.condition: {token}")
    };
}