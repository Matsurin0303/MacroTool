using System.Drawing;

namespace MacroTool.WinForms;

internal static class PointExtensions
{
    // Point.Center() を呼ばれているため、とりあえず恒等で定義（必要なら後で意味のある実装に変更）
    public static Point Center(this Point p) => p;
}
