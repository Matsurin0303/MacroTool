using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

/// <summary>
/// CSV_v1.0 形式でのマクロ出力を担うインターフェース。
/// Infrastructure 層で実装する。
/// </summary>
public interface ICsvMacroExporter
{
    /// <summary>
    /// マクロを指定パスに CSV_v1.0 形式で出力する。
    /// </summary>
    void Export(Macro macro, string path);
}
