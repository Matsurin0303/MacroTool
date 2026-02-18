using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using MacroTool.Domain.Macros;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// FindImage / FindText(OCR) の「テスト」用の簡易実行。
/// - UI ダイアログから呼び出す用途
/// - マウス移動/クリックなどの副作用は行わない（座標計算のみ）
/// </summary>
internal static class DetectionTestUtil
{
    public static async Task<(bool success, Point? screenPoint, Rectangle searchRect)> TestFindImageAsync(FindImageAction action, CancellationToken token)
    {
        var rect = ResolveSearchRectangle(action.SearchArea);
        return await TestFindImageAsync(action, rect, token);
    }

    public static async Task<(bool success, Point? screenPoint, Rectangle searchRect)> TestFindImageAsync(FindImageAction action, Rectangle searchRect, CancellationToken token)
    {
        var rect = searchRect;
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("FindImage: 検索領域が不正です。");

        using var template = LoadTemplate(action.Template);
        if (template is null)
            throw new InvalidOperationException("FindImage: テンプレートが設定されていません。");

        var start = Stopwatch.StartNew();
        var timeout = Math.Max(0, action.TimeoutMs);

        while (true)
        {
            if (token.IsCancellationRequested)
                return (false, null, rect);

            using var screen = Capture(rect);
            if (TryFindTemplate(screen, template, action.ColorTolerancePercent, out var foundTopLeft))
            {
                var anchor = ResolveAnchorPoint(foundTopLeft, template.Size, action.MousePosition);
                var screenPt = new Point(rect.Left + anchor.X, rect.Top + anchor.Y);
                return (true, screenPt, rect);
            }

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return (false, null, rect);

            // テストは UI なので少し短め
            await Task.Delay(150);
            if (token.IsCancellationRequested)
                return (false, null, rect);
        }
    }

    public static async Task<(bool success, Point? screenPoint, Rectangle searchRect)> TestFindTextOcrAsync(FindTextOcrAction action, CancellationToken token)
    {
        var rect = ResolveSearchRectangle(action.SearchArea);
        return await TestFindTextOcrAsync(action, rect, token);
    }

    public static async Task<(bool success, Point? screenPoint, Rectangle searchRect)> TestFindTextOcrAsync(FindTextOcrAction action, Rectangle searchRect, CancellationToken token)
    {
        var rect = searchRect;
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("FindText(OCR): 検索領域が不正です。");

        var start = Stopwatch.StartNew();
        var timeout = Math.Max(0, action.TimeoutMs);

        while (true)
        {
            if (token.IsCancellationRequested)
                return (false, null, rect);

            using var screen = Capture(rect);
            var found = await TryFindTextByOcrAsync(screen, action.TextToSearchFor ?? string.Empty, action.Language, token);
            if (found is not null)
            {
                var anchor = ResolveAnchorPoint(new Point(found.Value.Bounds.Left, found.Value.Bounds.Top), found.Value.Bounds.Size, action.MousePosition);
                var screenPt = new Point(rect.Left + anchor.X, rect.Top + anchor.Y);
                return (true, screenPt, rect);
            }

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return (false, null, rect);

            await Task.Delay(200);
            if (token.IsCancellationRequested)
                return (false, null, rect);
        }
    }

    public static Rectangle ResolveSearchRectangle(SearchArea area)
    {
        area ??= new SearchArea { Kind = SearchAreaKind.EntireDesktop };
        return area.Kind switch
        {
            SearchAreaKind.EntireDesktop => SystemInformation.VirtualScreen,
            SearchAreaKind.AreaOfDesktop => NormalizeRect(area.X1, area.Y1, area.X2, area.Y2),
            SearchAreaKind.FocusedWindow => GetForegroundWindowRectOrVirtual(),
            SearchAreaKind.AreaOfFocusedWindow => ResolveAreaOfFocusedWindow(area),
            _ => SystemInformation.VirtualScreen
        };
    }

    private static Rectangle ResolveAreaOfFocusedWindow(SearchArea area)
    {
        var win = GetForegroundWindowRectOrVirtual();
        var rel = NormalizeRect(area.X1, area.Y1, area.X2, area.Y2);
        return new Rectangle(win.Left + rel.Left, win.Top + rel.Top, rel.Width, rel.Height);
    }

    private static Rectangle NormalizeRect(int x1, int y1, int x2, int y2)
    {
        int left = Math.Min(x1, x2);
        int top = Math.Min(y1, y2);
        int right = Math.Max(x1, x2);
        int bottom = Math.Max(y1, y2);
        return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static Bitmap Capture(Rectangle rect)
    {
        var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }

    private static byte[] ReadBytes(Bitmap bmp, out int stride)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        stride = data.Stride;
        var bytes = new byte[stride * bmp.Height];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        bmp.UnlockBits(data);
        return bytes;
    }

    private static Bitmap? LoadTemplate(ImageTemplate template)
    {
        template ??= new ImageTemplate();

        return template.Kind switch
        {
            ImageTemplateKind.EmbeddedPng when template.PngBytes is { Length: > 0 } => new Bitmap(new MemoryStream(template.PngBytes)),
            ImageTemplateKind.FilePath when !string.IsNullOrWhiteSpace(template.FilePath) && File.Exists(template.FilePath) => new Bitmap(template.FilePath),
            _ => null
        };
    }

    private static bool TryFindTemplate(Bitmap haystack, Bitmap needle, int tolerancePercent, out Point found)
    {
        if (needle.Width <= 0 || needle.Height <= 0 || haystack.Width < needle.Width || haystack.Height < needle.Height)
        {
            found = default;
            return false;
        }

        var hayBytes = ReadBytes(haystack, out int hayStride);
        var neeBytes = ReadBytes(needle, out int neeStride);

        double maxDist = 441.67295593 * Math.Clamp(tolerancePercent, 0, 100) / 100.0;
        double thrSq = maxDist * maxDist;

        int maxX = haystack.Width - needle.Width;
        int maxY = haystack.Height - needle.Height;

        // 大きいテンプレは軽くサンプリング
        int sample = (needle.Width * needle.Height) > 4000 ? 2 : 1;

        for (int y = 0; y <= maxY; y++)
        {
            for (int x = 0; x <= maxX; x++)
            {
                bool match = true;

                for (int ny = 0; ny < needle.Height; ny += sample)
                {
                    int rowHay = (y + ny) * hayStride;
                    int rowNee = ny * neeStride;

                    for (int nx = 0; nx < needle.Width; nx += sample)
                    {
                        int iHay = rowHay + (x + nx) * 4;
                        int iNee = rowNee + nx * 4;

                        int db = hayBytes[iHay + 0] - neeBytes[iNee + 0];
                        int dg = hayBytes[iHay + 1] - neeBytes[iNee + 1];
                        int dr = hayBytes[iHay + 2] - neeBytes[iNee + 2];

                        int distSq = db * db + dg * dg + dr * dr;
                        if (distSq > thrSq)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (!match) break;
                }

                if (match)
                {
                    found = new Point(x, y);
                    return true;
                }
            }
        }

        found = default;
        return false;
    }

    private static Point ResolveAnchorPoint(Point topLeft, Size size, MousePosition pos)
    {
        int w = Math.Max(1, size.Width);
        int h = Math.Max(1, size.Height);

        return pos switch
        {
            MousePosition.TopLeft => new Point(topLeft.X, topLeft.Y),
            MousePosition.TopRight => new Point(topLeft.X + w - 1, topLeft.Y),
            MousePosition.BottomLeft => new Point(topLeft.X, topLeft.Y + h - 1),
            MousePosition.BottomRight => new Point(topLeft.X + w - 1, topLeft.Y + h - 1),
            _ => new Point(topLeft.X + w / 2, topLeft.Y + h / 2)
        };
    }

    private readonly record struct OcrFound(Rectangle Bounds);

    private static async Task<OcrFound?> TryFindTextByOcrAsync(Bitmap bmp, string needle, OcrLanguage lang, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(needle)) return null;

        // Bitmap -> SoftwareBitmap
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Bmp);
        var bytes = ms.ToArray();

        var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(bytes.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        // フォーマット変換（OcrEngineが扱える形式に）
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }

        if (token.IsCancellationRequested)
            return null;

        var language = lang == OcrLanguage.Japanese ? new Language("ja-JP") : new Language("en-US");
        var engine = OcrEngine.TryCreateFromLanguage(language) ?? OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
            return null;

        var result = await engine.RecognizeAsync(softwareBitmap);
        var comp = StringComparison.OrdinalIgnoreCase;

        foreach (var line in result.Lines)
        {
            var lineText = string.Join(" ", line.Words.Select(w => w.Text));
            if (!lineText.Contains(needle, comp))
                continue;

            // 行のBoundingを推定（ワード矩形のunion）
            double left = double.MaxValue, top = double.MaxValue, right = 0, bottom = 0;
            foreach (var w in line.Words)
            {
                var r = w.BoundingRect;
                left = Math.Min(left, r.X);
                top = Math.Min(top, r.Y);
                right = Math.Max(right, r.X + r.Width);
                bottom = Math.Max(bottom, r.Y + r.Height);
            }

            if (left == double.MaxValue) continue;
            var rect = Rectangle.FromLTRB((int)left, (int)top, (int)right, (int)bottom);
            return new OcrFound(rect);
        }

        return null;
    }

    // ===== Win32: foreground window rect =====
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    private static Rectangle GetForegroundWindowRectOrVirtual()
    {
        var h = GetForegroundWindow();
        if (h == IntPtr.Zero) return SystemInformation.VirtualScreen;
        if (!GetWindowRect(h, out var r)) return SystemInformation.VirtualScreen;
        return new Rectangle(r.Left, r.Top, Math.Max(0, r.Right - r.Left), Math.Max(0, r.Bottom - r.Top));
    }
}
