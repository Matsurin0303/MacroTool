namespace MacroTool.Domain.Macros;

/// <summary>
/// マクロの整合性検証で検出されたエラー。
/// </summary>
/// <param name="StepIndex">エラーが発生したステップのインデックス（0始まり）</param>
/// <param name="Field">エラーが発生したフィールド名</param>
/// <param name="Message">エラーメッセージ</param>
public sealed record MacroValidationError(int StepIndex, string Field, string Message);
