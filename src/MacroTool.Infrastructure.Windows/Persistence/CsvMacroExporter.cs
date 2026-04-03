using MacroTool.Domain.Macros;
using System.Globalization;
using System.Text;

namespace MacroTool.Infrastructure.Windows.Persistence;

/// <summary>
/// CSV_v1.0 仕様に準拠した CSV Export。
/// 48列固定ヘッダでマクロを出力する。
/// </summary>
public static class CsvMacroExporter
{
    // ---- 列インデックス（0-based、CSV_Schema_v1.0 準拠） ----
    private const int ColOrder = 0;
    private const int ColAction = 1;
    private const int ColLabel = 2;
    private const int ColComment = 3;
    private const int ColSearchAreaKind = 4;
    private const int ColX1 = 5;
    private const int ColY1 = 6;
    private const int ColX2 = 7;
    private const int ColY2 = 8;
    private const int ColWaitingMs = 9;
    private const int ColGoTo = 10;
    private const int ColTrueGoTo = 11;
    private const int ColFalseGoTo = 12;
    private const int ColFinishGoTo = 13;
    private const int ColMouseActionBehavior = 14;
    private const int ColMousePosition = 15;
    private const int ColSaveXVariable = 16;
    private const int ColSaveYVariable = 17;
    private const int ColTolerance = 18;
    private const int ColText = 19;
    private const int ColLanguage = 20;
    private const int ColBitmapKind = 21;
    private const int ColBitmapValue = 22;
    private const int ColMouseButton = 23;
    private const int ColClickType = 24;
    private const int ColRelative = 25;
    private const int ColX = 26;
    private const int ColY = 27;
    private const int ColColor = 28;
    private const int ColStartX = 29;
    private const int ColStartY = 30;
    private const int ColEndX = 31;
    private const int ColEndY = 32;
    private const int ColDurationMs = 33;
    private const int ColWheelOrientation = 34;
    private const int ColWheelValue = 35;
    private const int ColKeyOption = 36;
    private const int ColKey = 37;
    private const int ColCount = 38;
    private const int ColStartLabel = 39;
    private const int ColRepeatMode = 40;
    private const int ColSeconds = 41;
    private const int ColRepetitions = 42;
    private const int ColUntil = 43;
    private const int ColVariableName = 44;
    private const int ColConditionType = 45;
    private const int ColConditionValue = 46;
    private const int ColPath = 47;
    private const int ColumnCount = 48;

    private const string Header =
        "Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,"
      + "GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,"
      + "SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,"
      + "MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,"
      + "WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,"
      + "Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue,Path";

    /// <summary>
    /// CSV_v1.0 形式でマクロを指定パスに出力する。
    /// </summary>
    public static void Export(Macro macro, string path)
    {
        if (macro is null) throw new ArgumentNullException(nameof(macro));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var lines = new List<string>(macro.Count + 1) { Header };

        for (int i = 0; i < macro.Steps.Count; i++)
        {
            lines.Add(BuildRow(i + 1, macro.Steps[i]));
        }

        File.WriteAllLines(path, lines, Encoding.UTF8);
    }

    // ---- 行生成 ----

    private static string BuildRow(int order, MacroStep step)
    {
        var cols = new string[ColumnCount];
        Array.Fill(cols, "");

        cols[ColOrder] = order.ToString(CultureInfo.InvariantCulture);
        cols[ColAction] = step.Action.Kind;
        cols[ColLabel] = step.Label;
        cols[ColComment] = step.Comment;

        MapAction(cols, step.Action);

        return string.Join(",", cols.Select(CsvEscape));
    }

    // ---- Action 別列マッピング ----

    private static void MapAction(string[] cols, MacroAction action)
    {
        switch (action)
        {
            case MouseClickAction a:
                cols[ColMouseButton] = a.Button.ToString();
                cols[ColClickType] = a.ClickType.ToString();
                cols[ColRelative] = a.Relative.ToString();
                cols[ColX] = a.X.ToString(CultureInfo.InvariantCulture);
                cols[ColY] = a.Y.ToString(CultureInfo.InvariantCulture);
                break;

            case MouseMoveAction a:
                cols[ColRelative] = a.Relative.ToString();
                cols[ColStartX] = a.StartX.ToString(CultureInfo.InvariantCulture);
                cols[ColStartY] = a.StartY.ToString(CultureInfo.InvariantCulture);
                cols[ColEndX] = a.EndX.ToString(CultureInfo.InvariantCulture);
                cols[ColEndY] = a.EndY.ToString(CultureInfo.InvariantCulture);
                cols[ColDurationMs] = a.DurationMs.ToString(CultureInfo.InvariantCulture);
                break;

            case MouseWheelAction a:
                cols[ColWheelOrientation] = a.Orientation.ToString();
                cols[ColWheelValue] = a.Value.ToString(CultureInfo.InvariantCulture);
                break;

            case KeyPressAction a:
                cols[ColKeyOption] = a.Option.ToString();
                cols[ColKey] = a.Key.Code.ToString(CultureInfo.InvariantCulture);
                cols[ColCount] = a.Count.ToString(CultureInfo.InvariantCulture);
                break;

            case WaitTimeAction a:
                cols[ColWaitingMs] = a.Milliseconds.ToString(CultureInfo.InvariantCulture);
                break;

            case WaitForPixelColorAction a:
                cols[ColWaitingMs] = a.TimeoutMs.ToString(CultureInfo.InvariantCulture);
                cols[ColTrueGoTo] = ToGoToString(a.TrueGoTo);
                cols[ColFalseGoTo] = ToGoToString(a.FalseGoTo);
                cols[ColTolerance] = a.TolerancePercent.ToString(CultureInfo.InvariantCulture);
                cols[ColX] = a.X.ToString(CultureInfo.InvariantCulture);
                cols[ColY] = a.Y.ToString(CultureInfo.InvariantCulture);
                cols[ColColor] = a.ColorHex;
                break;

            case WaitForTextInputAction a:
                cols[ColWaitingMs] = a.TimeoutMs.ToString(CultureInfo.InvariantCulture);
                cols[ColTrueGoTo] = ToGoToString(a.TrueGoTo);
                cols[ColFalseGoTo] = ToGoToString(a.FalseGoTo);
                cols[ColText] = a.TextToWaitFor;
                break;

            case FindImageAction a:
                cols[ColSearchAreaKind] = a.SearchArea.Kind.ToString();
                cols[ColX1] = a.SearchArea.X1.ToString(CultureInfo.InvariantCulture);
                cols[ColY1] = a.SearchArea.Y1.ToString(CultureInfo.InvariantCulture);
                cols[ColX2] = a.SearchArea.X2.ToString(CultureInfo.InvariantCulture);
                cols[ColY2] = a.SearchArea.Y2.ToString(CultureInfo.InvariantCulture);
                cols[ColWaitingMs] = a.TimeoutMs.ToString(CultureInfo.InvariantCulture);
                cols[ColTrueGoTo] = ToGoToString(a.TrueGoTo);
                cols[ColFalseGoTo] = ToGoToString(a.FalseGoTo);
                cols[ColMouseActionBehavior] = a.MouseAction.ToString();
                cols[ColMousePosition] = a.MousePosition.ToString();
                if (a.SaveCoordinateEnabled)
                {
                    cols[ColSaveXVariable] = a.SaveXVariable;
                    cols[ColSaveYVariable] = a.SaveYVariable;
                }
                cols[ColTolerance] = a.ColorTolerancePercent.ToString(CultureInfo.InvariantCulture);
                cols[ColBitmapKind] = a.Template.Kind.ToString();
                // CapturedBitmap の CSV 表現は仕様上未確定（CSV_Schema_v1.0 §10）
                cols[ColBitmapValue] = a.Template.Kind == ImageTemplateKind.FilePath
                    ? a.Template.FilePath
                    : "";
                break;

            case FindTextOcrAction a:
                cols[ColSearchAreaKind] = a.SearchArea.Kind.ToString();
                cols[ColX1] = a.SearchArea.X1.ToString(CultureInfo.InvariantCulture);
                cols[ColY1] = a.SearchArea.Y1.ToString(CultureInfo.InvariantCulture);
                cols[ColX2] = a.SearchArea.X2.ToString(CultureInfo.InvariantCulture);
                cols[ColY2] = a.SearchArea.Y2.ToString(CultureInfo.InvariantCulture);
                cols[ColWaitingMs] = a.TimeoutMs.ToString(CultureInfo.InvariantCulture);
                cols[ColTrueGoTo] = ToGoToString(a.TrueGoTo);
                cols[ColFalseGoTo] = ToGoToString(a.FalseGoTo);
                cols[ColMouseActionBehavior] = a.MouseAction.ToString();
                cols[ColMousePosition] = a.MousePosition.ToString();
                if (a.SaveCoordinateEnabled)
                {
                    cols[ColSaveXVariable] = a.SaveXVariable;
                    cols[ColSaveYVariable] = a.SaveYVariable;
                }
                cols[ColText] = a.TextToSearchFor;
                cols[ColLanguage] = a.Language.ToString();
                break;

            case GoToAction a:
                cols[ColGoTo] = ToGoToString(a.Target);
                break;

            case IfAction a:
                cols[ColTrueGoTo] = ToGoToString(a.TrueGoTo);
                cols[ColFalseGoTo] = ToGoToString(a.FalseGoTo);
                cols[ColVariableName] = a.VariableName;
                cols[ColConditionType] = a.Condition.ToString();
                cols[ColConditionValue] = a.Value;
                break;

            case RepeatAction a:
                cols[ColFinishGoTo] = ToGoToString(a.AfterRepeatGoTo);
                cols[ColStartLabel] = a.StartLabel;
                cols[ColRepeatMode] = a.Condition.Kind.ToString();
                switch (a.Condition.Kind)
                {
                    case RepeatConditionKind.Seconds:
                        cols[ColSeconds] = a.Condition.Seconds.ToString(CultureInfo.InvariantCulture);
                        break;
                    case RepeatConditionKind.Repetitions:
                        cols[ColRepetitions] = a.Condition.Repetitions.ToString(CultureInfo.InvariantCulture);
                        break;
                    case RepeatConditionKind.Until:
                        cols[ColUntil] = a.Condition.UntilTime;
                        break;
                    // Infinite: 追加列不要
                }
                break;

            case EmbedMacroFileAction a:
                cols[ColPath] = a.MacroFilePath;
                break;

            case ExecuteProgramAction a:
                cols[ColPath] = a.ProgramPath;
                break;
        }
    }

    // ---- ヘルパー ----

    private static string ToGoToString(GoToTarget target)
    {
        return target.Kind switch
        {
            GoToKind.Start => "Start",
            GoToKind.Next => "Next",
            GoToKind.End => "End",
            GoToKind.Label => $"Label:{target.Label}",
            _ => "Next"
        };
    }

    private static string CsvEscape(string? s)
    {
        s ??= "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
        {
            return '"' + s.Replace("\"", "\"\"") + '"';
        }
        return s;
    }
}
