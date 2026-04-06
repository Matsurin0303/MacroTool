using System.Text.RegularExpressions;

namespace MacroTool.Domain.Macros;

/// <summary>
/// Macro の整合性検証を行うドメインサービス。
/// CSV Import / Export 時の共通バリデーションロジックを提供する。
/// </summary>
public sealed class MacroValidator
{
    private static readonly Regex VariableNameRegex = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Macro 全体の整合性を検証する。
    /// </summary>
    public IReadOnlyList<MacroValidationError> Validate(Macro macro)
    {
        if (macro is null) throw new ArgumentNullException(nameof(macro));

        var errors = new List<MacroValidationError>();
        var definedLabels = new HashSet<string>(
            macro.GetDefinedLabels(),
            StringComparer.Ordinal);

        for (int i = 0; i < macro.Steps.Count; i++)
        {
            var step = macro.Steps[i];
            ValidateGoToReferences(step.Action, i, definedLabels, errors);
            ValidateRepeat(step.Action, i, definedLabels, macro.Steps, errors);
            ValidateVariableNames(step.Action, i, errors);
            ValidateRequiredFields(step.Action, i, errors);
        }

        return errors;
    }

    /// <summary>
    /// GoTo 参照先ラベルが存在するか検証する。
    /// </summary>
    private static void ValidateGoToReferences(
        MacroAction action, int index,
        HashSet<string> definedLabels,
        List<MacroValidationError> errors)
    {
        switch (action)
        {
            case GoToAction a:
                ValidateGoToTarget(a.Target, index, "GoTo", definedLabels, errors);
                break;
            case IfAction a:
                ValidateGoToTarget(a.TrueGoTo, index, "TrueGoTo", definedLabels, errors);
                ValidateGoToTarget(a.FalseGoTo, index, "FalseGoTo", definedLabels, errors);
                break;
            case WaitForPixelColorAction a:
                ValidateGoToTarget(a.TrueGoTo, index, "TrueGoTo", definedLabels, errors);
                ValidateGoToTarget(a.FalseGoTo, index, "FalseGoTo", definedLabels, errors);
                break;
            case WaitForTextInputAction a:
                ValidateGoToTarget(a.TrueGoTo, index, "TrueGoTo", definedLabels, errors);
                ValidateGoToTarget(a.FalseGoTo, index, "FalseGoTo", definedLabels, errors);
                break;
            case FindImageAction a:
                ValidateGoToTarget(a.TrueGoTo, index, "TrueGoTo", definedLabels, errors);
                ValidateGoToTarget(a.FalseGoTo, index, "FalseGoTo", definedLabels, errors);
                break;
            case FindTextOcrAction a:
                ValidateGoToTarget(a.TrueGoTo, index, "TrueGoTo", definedLabels, errors);
                ValidateGoToTarget(a.FalseGoTo, index, "FalseGoTo", definedLabels, errors);
                break;
            case RepeatAction a:
                ValidateGoToTarget(a.AfterRepeatGoTo, index, "FinishGoTo", definedLabels, errors);
                break;
        }
    }

    private static void ValidateGoToTarget(
        GoToTarget target, int index, string field,
        HashSet<string> definedLabels,
        List<MacroValidationError> errors)
    {
        if (target is null) return;
        if (target.Kind != GoToKind.Label) return;

        if (string.IsNullOrWhiteSpace(target.Label))
        {
            errors.Add(new MacroValidationError(
                index, field,
                $"行 {index + 1}: {field} の Label が空です。"));
            return;
        }

        if (!definedLabels.Contains(target.Label))
        {
            errors.Add(new MacroValidationError(
                index, field,
                $"行 {index + 1}: {field} の参照先ラベル「{target.Label}」が存在しません。"));
        }
    }

    /// <summary>
    /// Repeat の StartLabel 参照と、ネスト禁止を検証する。
    /// </summary>
    private static void ValidateRepeat(
        MacroAction action, int repeatIndex,
        HashSet<string> definedLabels,
        IReadOnlyList<MacroStep> steps,
        List<MacroValidationError> errors)
    {
        if (action is not RepeatAction repeat) return;

        // StartLabel 必須
        if (string.IsNullOrWhiteSpace(repeat.StartLabel))
        {
            errors.Add(new MacroValidationError(
                repeatIndex, "StartLabel",
                $"行 {repeatIndex + 1}: Repeat の StartLabel が空です。"));
            return;
        }

        // StartLabel が既存ラベルを参照しているか
        if (!definedLabels.Contains(repeat.StartLabel))
        {
            errors.Add(new MacroValidationError(
                repeatIndex, "StartLabel",
                $"行 {repeatIndex + 1}: Repeat の StartLabel「{repeat.StartLabel}」が存在しません。"));
            return;
        }

        // StartLabel のインデックスを取得
        int startIndex = -1;
        for (int i = 0; i < steps.Count; i++)
        {
            var stepLabel = (steps[i].Label ?? "").Trim();
            if (stepLabel == repeat.StartLabel)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex < 0) return; // ラベル検証は上でエラー済み
        if (startIndex >= repeatIndex) return; // StartLabel が Repeat 行より後方にある場合はネスト検証不要

        // ネスト禁止: StartLabel 行と Repeat 行の間に別の RepeatAction がないか
        for (int i = startIndex + 1; i < repeatIndex; i++)
        {
            if (steps[i].Action is RepeatAction)
            {
                errors.Add(new MacroValidationError(
                    repeatIndex, "Repeat",
                    $"行 {repeatIndex + 1}: Repeat のネストが検出されました（行 {i + 1} に別の Repeat があります）。"));
                break;
            }
        }
    }

    /// <summary>
    /// 変数名が正規表現 ^[A-Za-z_][A-Za-z0-9_]*$ に一致するか検証する。
    /// </summary>
    private static void ValidateVariableNames(
        MacroAction action, int index,
        List<MacroValidationError> errors)
    {
        switch (action)
        {
            case IfAction a:
                ValidateVariableName(a.VariableName, index, "VariableName", required: true, errors);
                break;
            case FindImageAction a:
                if (a.SaveCoordinateEnabled)
                {
                    ValidateVariableName(a.SaveXVariable, index, "SaveXVariable", required: false, errors);
                    ValidateVariableName(a.SaveYVariable, index, "SaveYVariable", required: false, errors);
                }
                break;
            case FindTextOcrAction a:
                if (a.SaveCoordinateEnabled)
                {
                    ValidateVariableName(a.SaveXVariable, index, "SaveXVariable", required: false, errors);
                    ValidateVariableName(a.SaveYVariable, index, "SaveYVariable", required: false, errors);
                }
                break;
            case WaitForScreenChangeAction a:
                if (a.SaveCoordinateEnabled)
                {
                    ValidateVariableName(a.SaveXVariable, index, "SaveXVariable", required: false, errors);
                    ValidateVariableName(a.SaveYVariable, index, "SaveYVariable", required: false, errors);
                }
                break;
        }
    }

    private static void ValidateVariableName(
        string? name, int index, string field,
        bool required, List<MacroValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            if (required)
            {
                errors.Add(new MacroValidationError(
                    index, field,
                    $"行 {index + 1}: {field} は必須です。"));
            }
            return;
        }

        if (!VariableNameRegex.IsMatch(name))
        {
            errors.Add(new MacroValidationError(
                index, field,
                $"行 {index + 1}: {field}「{name}」が変数名規則に違反しています（使用可能: 英字・数字・アンダースコア、先頭は英字またはアンダースコア）。"));
        }
    }

    /// <summary>
    /// Action 別の必須フィールドを検証する。
    /// </summary>
    private static void ValidateRequiredFields(
        MacroAction action, int index,
        List<MacroValidationError> errors)
    {
        switch (action)
        {
            case EmbedMacroFileAction a:
                if (string.IsNullOrWhiteSpace(a.MacroFilePath))
                {
                    errors.Add(new MacroValidationError(
                        index, "Path",
                        $"行 {index + 1}: EmbedMacroFile の Path は必須です。"));
                }
                break;
            case ExecuteProgramAction a:
                if (string.IsNullOrWhiteSpace(a.ProgramPath))
                {
                    errors.Add(new MacroValidationError(
                        index, "Path",
                        $"行 {index + 1}: ExecuteProgram の Path は必須です。"));
                }
                break;
            case WaitForTextInputAction a:
                if (string.IsNullOrWhiteSpace(a.TextToWaitFor))
                {
                    errors.Add(new MacroValidationError(
                        index, "Text",
                        $"行 {index + 1}: WaitForTextInput の Text は必須です。"));
                }
                break;
            case FindTextOcrAction a:
                if (string.IsNullOrWhiteSpace(a.TextToSearchFor))
                {
                    errors.Add(new MacroValidationError(
                        index, "Text",
                        $"行 {index + 1}: FindTextOcr の Text は必須です。"));
                }
                break;
        }
    }
}
