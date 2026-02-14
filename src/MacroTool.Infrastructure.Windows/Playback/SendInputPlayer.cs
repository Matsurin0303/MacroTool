using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Domain.Macros;
using MacroTool.Infrastructure.Windows.Interop;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MacroTool.Infrastructure.Windows.Playback;

/// <summary>
/// SendInput + 画面キャプチャ/OCR を用いたプレイヤー。
/// v1.0 機能のうち「将来実装予定」を除く範囲を実装。
/// </summary>
public sealed class SendInputPlayer : IPlayer
{
    private readonly IPlaybackOptionsAccessor _optAccessor;
    private readonly IMacroRepository _repo;

    public SendInputPlayer(IPlaybackOptionsAccessor optAccessor, IMacroRepository repo)
    {
        _optAccessor = optAccessor;
        _repo = repo;
    }

    public async Task PlayAsync(Macro macro, CancellationToken token)
    {
        // ★再生中は固定：開始時点の設定をスナップショット
        var opt = _optAccessor.Current;

        var ctx = new PlaybackContext();
        await PlayMacroAsync(macro, ctx, opt, depth: 0, token);
    }

    private async Task PlayMacroAsync(Macro macro, PlaybackContext ctx, PlaybackOptions opt, int depth, CancellationToken token)
    {
        if (depth > 5)
            throw new InvalidOperationException("EmbedMacroFile の入れ子が深すぎます（最大5）。");

        var steps = macro.Steps;
        var labelMap = BuildLabelMap(steps);

        int i = 0;
        while (i >= 0 && i < steps.Count)
        {
            token.ThrowIfCancellationRequested();

            var step = steps[i];
            var next = await ExecuteStepAsync(step, i, steps, labelMap, ctx, opt, depth, token);
            i = next;
        }
    }

    private async Task<int> ExecuteStepAsync(
        MacroStep step,
        int index,
        IReadOnlyList<MacroStep> steps,
        Dictionary<string, int> labelMap,
        PlaybackContext ctx,
        PlaybackOptions opt,
        int depth,
        CancellationToken token)
    {
        var action = step.Action;

        switch (action)
        {
            // ===== Mouse =====
            case MouseClickAction mc:
                await DoMouseClickAsync(mc, opt, token);
                return index + 1;

            case MouseMoveAction mm:
                await DoMouseMoveAsync(mm, opt, token);
                return index + 1;

            case MouseWheelAction mw:
                DoMouseWheel(mw);
                return index + 1;

            // ===== Key =====
            case KeyPressAction kp:
                await DoKeyPressAsync(kp, opt, token);
                return index + 1;

            // ===== Wait =====
            case WaitTimeAction wt:
                if (wt.Milliseconds > 0)
                    await Task.Delay(wt.Milliseconds, token);
                return index + 1;

            case WaitForPixelColorAction wpc:
                {
                    bool ok = await WaitForPixelColorAsync(wpc, token);
                    return ResolveGoTo(ok ? wpc.IfTrueGoTo : wpc.IfFalseGoTo, index, steps, labelMap);
                }

            case WaitForScreenChangeAction wsc:
                {
                    var result = await WaitForScreenChangeAsync(wsc, ctx, opt, token);
                    return ResolveGoTo(result.Success ? wsc.IfTrueGoTo : wsc.IfFalseGoTo, index, steps, labelMap);
                }

            case WaitForTextInputAction wti:
                {
                    bool ok = await WaitForTextInputAsync(wti, token);
                    return ResolveGoTo(ok ? wti.IfTrueGoTo : wti.IfFalseGoTo, index, steps, labelMap);
                }

            // ===== Detection =====
            case FindImageAction fia:
                {
                    var result = await FindImageAsync(fia, ctx, opt, token);
                    return ResolveGoTo(result.Success ? fia.IfTrueGoTo : fia.IfFalseGoTo, index, steps, labelMap);
                }

            case FindTextOcrAction fto:
                {
                    var result = await FindTextOcrAsync(fto, ctx, opt, token);
                    return ResolveGoTo(result.Success ? fto.IfTrueGoTo : fto.IfFalseGoTo, index, steps, labelMap);
                }

            // ===== Control flow =====
            case GoToAction gt:
                return ResolveGoTo(gt.Target, index, steps, labelMap);

            case IfAction ia:
                {
                    bool ok = EvaluateIf(ia, ctx);
                    return ResolveGoTo(ok ? ia.IfTrueGoTo : ia.IfFalseGoTo, index, steps, labelMap);
                }

            case RepeatAction ra:
                {
                    bool cont = EvaluateRepeat(ra, index, labelMap, ctx);
                    if (cont)
                        return ResolveLabel(ra.StartLabel, labelMap);

                    // ループ終了
                    ctx.RepeatStates.Remove(index);
                    return ResolveGoTo(ra.AfterRepeatGoTo, index, steps, labelMap);
                }

            case EmbedMacroFileAction emb:
                {
                    var path = ExpandVariables(emb.MacroFilePath, ctx);
                    if (string.IsNullOrWhiteSpace(path))
                        throw new InvalidOperationException("EmbedMacroFile: MacroFilePath が空です。");

                    var loaded = _repo.Load(path);
                    await PlayMacroAsync(loaded, ctx, opt, depth + 1, token);
                    return index + 1;
                }

            case ExecuteProgramAction prog:
                {
                    var path = ExpandVariables(prog.ProgramPath, ctx);
                    if (string.IsNullOrWhiteSpace(path))
                        throw new InvalidOperationException("ExecuteProgram: ProgramPath が空です。");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });

                    return index + 1;
                }

            default:
                // 将来拡張用：未対応アクションは無視
                return index + 1;
        }
    }

    // ===== Context / helpers =====

    private sealed class PlaybackContext
    {
        public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Repeat の実行状態（キー: RepeatActionの行番号）
        /// </summary>
        public Dictionary<int, RepeatRuntimeState> RepeatStates { get; } = new();
    }

    private sealed class RepeatRuntimeState
    {
        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;
        public int CompletedIterations { get; set; } = 0;
    }

    private sealed record SearchResult(bool Success, Point? ScreenPoint);

    private static Dictionary<string, int> BuildLabelMap(IReadOnlyList<MacroStep> steps)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < steps.Count; i++)
        {
            var label = steps[i].Label?.Trim();
            if (!string.IsNullOrWhiteSpace(label))
                map[label] = i;
        }
        return map;
    }

    private static int ResolveLabel(string label, Dictionary<string, int> labelMap)
    {
        label = label?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Label が空です。");

        if (!labelMap.TryGetValue(label, out var idx))
            throw new InvalidOperationException($"Label が見つかりません: {label}");

        return idx;
    }

    private static int ResolveGoTo(GoToTarget target, int currentIndex, IReadOnlyList<MacroStep> steps, Dictionary<string, int> labelMap)
    {
        if (target is null) return currentIndex + 1;

        return target.Kind switch
        {
            GoToKind.Start => 0,
            GoToKind.End => steps.Count == 0 ? 0 : steps.Count - 1,
            GoToKind.Next => currentIndex + 1,
            GoToKind.Label => ResolveLabel(target.Label, labelMap),
            _ => currentIndex + 1
        };
    }

    private static string ExpandVariables(string text, PlaybackContext ctx)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 例: "C:\\tmp\\{FileName}.exe"
        return Regex.Replace(text, "\\{(?<name>[^}]+)\\}", m =>
        {
            var name = m.Groups["name"].Value.Trim();
            if (ctx.Variables.TryGetValue(name, out var v))
                return v;
            return m.Value;
        });
    }

    // ===== Stabilize =====

    private static async Task StabilizeAsync(PlaybackOptions opt, int ms, CancellationToken token)
    {
        if (!opt.EnableStabilizeWait) return;
        if (ms <= 0) return;
        await Task.Delay(ms, token);
    }

    // ===== Mouse =====

    private static async Task DoMouseClickAsync(MouseClickAction action, PlaybackOptions opt, CancellationToken token)
    {
        var pt = ResolvePoint(action.Relative, action.X, action.Y);
        Win32.SetCursorPos(pt.X, pt.Y);
        await StabilizeAsync(opt, opt.CursorSettleDelayMs, token);

        int clickCount = action.Action == MouseClickType.DoubleClick ? 2 : 1;
        bool doDown = action.Action is MouseClickType.Click or MouseClickType.DoubleClick or MouseClickType.Down;
        bool doUp = action.Action is MouseClickType.Click or MouseClickType.DoubleClick or MouseClickType.Up;

        for (int i = 0; i < clickCount; i++)
        {
            if (doDown) MouseEvent(GetMouseDownFlag(action.Button), GetXButtonData(action.Button));
            await StabilizeAsync(opt, opt.ClickHoldDelayMs, token);
            if (doUp) MouseEvent(GetMouseUpFlag(action.Button), GetXButtonData(action.Button));

            if (clickCount > 1 && i < clickCount - 1)
                await Task.Delay(Math.Max(30, opt.ClickHoldDelayMs), token);
        }
    }

    private static async Task DoMouseMoveAsync(MouseMoveAction action, PlaybackOptions opt, CancellationToken token)
    {
        var start = ResolvePoint(action.Relative, action.StartX, action.StartY);
        var end = ResolvePoint(action.Relative, action.EndX, action.EndY);

        Win32.SetCursorPos(start.X, start.Y);
        await StabilizeAsync(opt, opt.CursorSettleDelayMs, token);

        int duration = Math.Max(0, action.DurationMs);
        if (duration == 0)
        {
            Win32.SetCursorPos(end.X, end.Y);
            return;
        }

        int steps = Math.Max(1, duration / 15);
        for (int i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            int x = start.X + (end.X - start.X) * i / steps;
            int y = start.Y + (end.Y - start.Y) * i / steps;
            Win32.SetCursorPos(x, y);
            await Task.Delay(duration / steps, token);
        }
    }

    private static void DoMouseWheel(MouseWheelAction action)
    {
        uint flag = action.Orientation == WheelOrientation.Horizontal ? Win32.MOUSEEVENTF_HWHEEL : Win32.MOUSEEVENTF_WHEEL;
        MouseEvent(flag, unchecked((uint)action.Value));
    }

    private static Point ResolvePoint(bool relative, int x, int y)
    {
        if (!relative) return new Point(x, y);

        if (Win32.GetCursorPos(out var cur))
            return new Point(cur.x + x, cur.y + y);

        return new Point(x, y);
    }

    private static uint GetMouseDownFlag(MouseButton button)
        => button switch
        {
            MouseButton.Left => Win32.MOUSEEVENTF_LEFTDOWN,
            MouseButton.Right => Win32.MOUSEEVENTF_RIGHTDOWN,
            MouseButton.Middle => Win32.MOUSEEVENTF_MIDDLEDOWN,
            MouseButton.XButton1 or MouseButton.XButton2 => Win32.MOUSEEVENTF_XDOWN,
            _ => Win32.MOUSEEVENTF_LEFTDOWN
        };

    private static uint GetMouseUpFlag(MouseButton button)
        => button switch
        {
            MouseButton.Left => Win32.MOUSEEVENTF_LEFTUP,
            MouseButton.Right => Win32.MOUSEEVENTF_RIGHTUP,
            MouseButton.Middle => Win32.MOUSEEVENTF_MIDDLEUP,
            MouseButton.XButton1 or MouseButton.XButton2 => Win32.MOUSEEVENTF_XUP,
            _ => Win32.MOUSEEVENTF_LEFTUP
        };

    private static uint GetXButtonData(MouseButton button)
        => button switch
        {
            MouseButton.XButton2 => Win32.XBUTTON2,
            MouseButton.XButton1 => Win32.XBUTTON1,
            _ => 0
        };

    private static void MouseEvent(uint flags, uint mouseData = 0)
    {
        var input = new Win32.INPUT[1];
        input[0] = new Win32.INPUT
        {
            type = Win32.INPUT_MOUSE,
            U = new Win32.InputUnion
            {
                mi = new Win32.MOUSEINPUT
                {
                    dwFlags = flags,
                    mouseData = mouseData,
                    dwExtraInfo = InjectionTag.Value
                }
            }
        };

        Win32.SendInput(1, input, Marshal.SizeOf(typeof(Win32.INPUT)));
    }

    // ===== Keyboard =====

    private static async Task DoKeyPressAsync(KeyPressAction action, PlaybackOptions opt, CancellationToken token)
    {
        int count = Math.Max(1, action.Count);

        switch (action.Option)
        {
            case KeyPressOption.Down:
                DoKey(action.Key, isDown: true);
                return;

            case KeyPressOption.Up:
                DoKey(action.Key, isDown: false);
                return;

            default:
                for (int i = 0; i < count; i++)
                {
                    DoKey(action.Key, isDown: true);
                    await StabilizeAsync(opt, opt.ClickHoldDelayMs, token);
                    DoKey(action.Key, isDown: false);
                    if (count > 1 && i < count - 1)
                        await Task.Delay(Math.Max(20, opt.ClickHoldDelayMs), token);
                }
                return;
        }
    }

    private static void DoKey(VirtualKey key, bool isDown)
    {
        var input = new Win32.INPUT[1];
        input[0] = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            U = new Win32.InputUnion
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = key.Code,
                    dwFlags = isDown ? 0u : Win32.KEYEVENTF_KEYUP,
                    dwExtraInfo = InjectionTag.Value
                }
            }
        };

        Win32.SendInput(1, input, Marshal.SizeOf(typeof(Win32.INPUT)));
    }

    // ===== Wait =====

    private static async Task<bool> WaitForPixelColorAsync(WaitForPixelColorAction action, CancellationToken token)
    {
        var expected = ParseColor(action.ColorHex);
        var start = Stopwatch.StartNew();
        int timeout = action.TimeoutMs;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var actual = GetScreenPixel(action.X, action.Y);
            if (ColorClose(actual, expected, action.TolerancePercent))
                return true;

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return false;

            await Task.Delay(50, token);
        }
    }

    private sealed record ScreenChangeResult(bool Success, Point? ChangedPoint);

    private static async Task<ScreenChangeResult> WaitForScreenChangeAsync(WaitForScreenChangeAction action, PlaybackContext ctx, PlaybackOptions opt, CancellationToken token)
    {
        var rect = ResolveSearchRectangle(action.SearchArea);
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("WaitForScreenChange: 検索領域が不正です。");

        using var baseline = Capture(rect);
        var baselineBytes = ReadBytes(baseline, out int baseStride);

        var start = Stopwatch.StartNew();
        int timeout = action.TimeoutMs;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            using var nowBmp = Capture(rect);
            var nowBytes = ReadBytes(nowBmp, out int nowStride);

            if (TryFindScreenChange(baselineBytes, baseStride, nowBytes, nowStride, rect.Width, rect.Height, out var changed))
            {
                var screenPt = new Point(rect.Left + changed.X, rect.Top + changed.Y);
                ApplyCoordinateEffects(action.MouseActionEnabled, action.MouseAction, action.SaveCoordinate, action.SaveXVariable, action.SaveYVariable, screenPt, ctx, opt, token);
                return new ScreenChangeResult(true, screenPt);
            }

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return new ScreenChangeResult(false, null);

            await Task.Delay(100, token);
        }
    }

    private static async Task<bool> WaitForTextInputAsync(WaitForTextInputAction action, CancellationToken token)
    {
        var target = action.TextToWaitFor ?? string.Empty;
        if (target.Length == 0)
            return true; // 空なら即成功

        var input = await TextInputPrompt.ShowAsync($"入力してください: {target}", action.TimeoutMs, token);
        return string.Equals(input, target, StringComparison.Ordinal);
    }

    // ===== Detection =====

    private static async Task<SearchResult> FindImageAsync(FindImageAction action, PlaybackContext ctx, PlaybackOptions opt, CancellationToken token)
    {
        var rect = ResolveSearchRectangle(action.SearchArea);
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("FindImage: 検索領域が不正です。");

        using var template = LoadTemplate(action.Template);
        if (template is null)
            throw new InvalidOperationException("FindImage: テンプレートが設定されていません。");

        var start = Stopwatch.StartNew();
        int timeout = action.TimeoutMs;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            using var screen = Capture(rect);

            if (TryFindTemplate(screen, template, action.ColorTolerancePercent, out var foundTopLeft))
            {
                var anchor = ResolveAnchorPoint(foundTopLeft, template.Size, action.MousePosition);
                var screenPt = new Point(rect.Left + anchor.X, rect.Top + anchor.Y);

                ApplyCoordinateEffects(action.MouseActionEnabled, action.MouseAction, action.SaveCoordinate, action.SaveXVariable, action.SaveYVariable, screenPt, ctx, opt, token);
                return new SearchResult(true, screenPt);
            }

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return new SearchResult(false, null);

            await Task.Delay(150, token);
        }
    }

    private static async Task<SearchResult> FindTextOcrAsync(FindTextOcrAction action, PlaybackContext ctx, PlaybackOptions opt, CancellationToken token)
    {
        var rect = ResolveSearchRectangle(action.SearchArea);
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("FindTextOcr: 検索領域が不正です。");

        var start = Stopwatch.StartNew();
        int timeout = action.TimeoutMs;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            using var screen = Capture(rect);
            var found = await TryFindTextByOcrAsync(screen, action.TextToSearchFor ?? string.Empty, action.Language, token);

            if (found is not null)
            {
                var anchor = ResolveAnchorPoint(new Point(found.Value.Bounds.Left, found.Value.Bounds.Top), found.Value.Bounds.Size, action.MousePosition);
                var screenPt = new Point(rect.Left + anchor.X, rect.Top + anchor.Y);
                ApplyCoordinateEffects(action.MouseActionEnabled, action.MouseAction, action.SaveCoordinate, action.SaveXVariable, action.SaveYVariable, screenPt, ctx, opt, token);
                return new SearchResult(true, screenPt);
            }

            if (timeout > 0 && start.ElapsedMilliseconds >= timeout)
                return new SearchResult(false, null);

            await Task.Delay(200, token);
        }
    }

    // ===== If / Repeat =====

    private static bool EvaluateIf(IfAction action, PlaybackContext ctx)
    {
        var name = (action.VariableName ?? "").Trim();
        ctx.Variables.TryGetValue(name, out var v);
        v ??= "";

        var cmp = action.Value ?? "";
        var comp = StringComparison.OrdinalIgnoreCase;

        return action.Condition switch
        {
            IfConditionKind.TextEquals => string.Equals(v, cmp, comp),
            IfConditionKind.TextBeginsWith => v.StartsWith(cmp, comp),
            IfConditionKind.TextEndsWith => v.EndsWith(cmp, comp),
            IfConditionKind.TextIncludes => v.Contains(cmp, comp),

            IfConditionKind.TextNotEquals => !string.Equals(v, cmp, comp),
            IfConditionKind.TextNotBeginsWith => !v.StartsWith(cmp, comp),
            IfConditionKind.TextNotEndsWith => !v.EndsWith(cmp, comp),
            IfConditionKind.TextNotIncludes => !v.Contains(cmp, comp),

            IfConditionKind.TextLongerThan => v.Length > ParseInt(cmp),
            IfConditionKind.TextShorterThan => v.Length < ParseInt(cmp),

            IfConditionKind.ValueHigherThan => ParseDouble(v) > ParseDouble(cmp),
            IfConditionKind.ValueLowerThan => ParseDouble(v) < ParseDouble(cmp),
            IfConditionKind.ValueHigherOrEqual => ParseDouble(v) >= ParseDouble(cmp),
            IfConditionKind.ValueLowerOrEqual => ParseDouble(v) <= ParseDouble(cmp),

            IfConditionKind.RegEx => Regex.IsMatch(v, cmp),
            IfConditionKind.ValueDefined => !string.IsNullOrWhiteSpace(v),

            _ => false
        };
    }

    private static int ParseInt(string s)
        => int.TryParse(s, out var n) ? n : 0;

    private static double ParseDouble(string s)
        => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static bool EvaluateRepeat(RepeatAction action, int index, Dictionary<string, int> labelMap, PlaybackContext ctx)
    {
        // StartLabel の妥当性
        if (!string.IsNullOrWhiteSpace(action.StartLabel) && !labelMap.ContainsKey(action.StartLabel.Trim()))
            throw new InvalidOperationException($"Repeat: StartLabel が見つかりません: {action.StartLabel}");

        if (!ctx.RepeatStates.TryGetValue(index, out var state))
        {
            state = new RepeatRuntimeState { StartedAt = DateTimeOffset.Now, CompletedIterations = 0 };
            ctx.RepeatStates[index] = state;
        }

        // Repeat行に到達した＝1イテレーション完了
        state.CompletedIterations++;

        var cond = action.Condition ?? new RepeatCondition();
        return cond.Kind switch
        {
            RepeatConditionKind.Infinite => true,
            RepeatConditionKind.Repetitions => state.CompletedIterations < Math.Max(1, cond.Repetitions),
            RepeatConditionKind.Seconds => (DateTimeOffset.Now - state.StartedAt).TotalSeconds < Math.Max(0, cond.Seconds),
            RepeatConditionKind.Until => DateTime.Now < TodayAt(ParseTime(cond.UntilTime)),
            _ => false
        };
    }

    private static TimeSpan ParseTime(string s)
        => TimeSpan.TryParse(s, out var t) ? t : TimeSpan.Zero;

    private static DateTime TodayAt(TimeSpan t)
        => DateTime.Today + t;

    // ===== Screen / Image / OCR helpers =====

    private static Rectangle ResolveSearchRectangle(SearchArea area)
    {
        area ??= new SearchArea { Kind = SearchAreaKind.EntireDesktop };

        return area.Kind switch
        {
            SearchAreaKind.EntireDesktop => GetVirtualScreen(),
            SearchAreaKind.AreaOfDesktop => NormalizeRect(area.X1, area.Y1, area.X2, area.Y2),
            SearchAreaKind.FocusedWindow => GetForegroundWindowRect(),
            SearchAreaKind.AreaOfFocusedWindow => ResolveAreaOfFocusedWindow(area),
            _ => GetVirtualScreen()
        };
    }

    private static Rectangle ResolveAreaOfFocusedWindow(SearchArea area)
    {
        var win = GetForegroundWindowRect();
        var rel = NormalizeRect(area.X1, area.Y1, area.X2, area.Y2);
        return new Rectangle(win.Left + rel.Left, win.Top + rel.Top, rel.Width, rel.Height);
    }

    private static Rectangle GetVirtualScreen()
    {
        int x = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
        int y = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);
        int w = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
        int h = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);
        return new Rectangle(x, y, w, h);
    }

    private static Rectangle GetForegroundWindowRect()
    {
        var h = Win32.GetForegroundWindow();
        if (h == IntPtr.Zero)
            return GetVirtualScreen();

        if (!Win32.GetWindowRect(h, out var r))
            return GetVirtualScreen();

        return new Rectangle(r.Left, r.Top, Math.Max(0, r.Right - r.Left), Math.Max(0, r.Bottom - r.Top));
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

    private static bool TryFindScreenChange(byte[] baseBytes, int baseStride, byte[] nowBytes, int nowStride, int width, int height, out Point changed)
    {
        const int channelThreshold = 16; // 小さな揺らぎを無視

        int stride = Math.Min(baseStride, nowStride);

        for (int y = 0; y < height; y++)
        {
            int rowBase = y * baseStride;
            int rowNow = y * nowStride;
            for (int x = 0; x < width; x++)
            {
                int iBase = rowBase + x * 4;
                int iNow = rowNow + x * 4;

                int db = Math.Abs(nowBytes[iNow + 0] - baseBytes[iBase + 0]);
                int dg = Math.Abs(nowBytes[iNow + 1] - baseBytes[iBase + 1]);
                int dr = Math.Abs(nowBytes[iNow + 2] - baseBytes[iBase + 2]);

                if (db > channelThreshold || dg > channelThreshold || dr > channelThreshold)
                {
                    changed = new Point(x, y);
                    return true;
                }
            }
        }

        changed = default;
        return false;
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

        // 簡易的なサンプリング（テンプレが大きい場合に少し軽くする）
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

    private static void ApplyCoordinateEffects(
        bool mouseAction,
        MouseActionBehavior behavior,
        bool save,
        string saveX,
        string saveY,
        Point screenPt,
        PlaybackContext ctx,
        PlaybackOptions opt,
        CancellationToken token)
    {
        if (save)
        {
            if (!string.IsNullOrWhiteSpace(saveX)) ctx.Variables[saveX.Trim()] = screenPt.X.ToString();
            if (!string.IsNullOrWhiteSpace(saveY)) ctx.Variables[saveY.Trim()] = screenPt.Y.ToString();
        }

        if (!mouseAction) return;

        // fire & forget は避けて同期的に実行
        Win32.SetCursorPos(screenPt.X, screenPt.Y);
        if (opt.EnableStabilizeWait && opt.CursorSettleDelayMs > 0)
            Thread.Sleep(opt.CursorSettleDelayMs);

        switch (behavior)
        {
            case MouseActionBehavior.LeftClick:
                MouseEvent(Win32.MOUSEEVENTF_LEFTDOWN);
                if (opt.ClickHoldDelayMs > 0) Thread.Sleep(opt.ClickHoldDelayMs);
                MouseEvent(Win32.MOUSEEVENTF_LEFTUP);
                break;

            case MouseActionBehavior.RightClick:
                MouseEvent(Win32.MOUSEEVENTF_RIGHTDOWN);
                if (opt.ClickHoldDelayMs > 0) Thread.Sleep(opt.ClickHoldDelayMs);
                MouseEvent(Win32.MOUSEEVENTF_RIGHTUP);
                break;

            case MouseActionBehavior.MiddleClick:
                MouseEvent(Win32.MOUSEEVENTF_MIDDLEDOWN);
                if (opt.ClickHoldDelayMs > 0) Thread.Sleep(opt.ClickHoldDelayMs);
                MouseEvent(Win32.MOUSEEVENTF_MIDDLEUP);
                break;

            case MouseActionBehavior.DoubleClick:
                MouseEvent(Win32.MOUSEEVENTF_LEFTDOWN);
                if (opt.ClickHoldDelayMs > 0) Thread.Sleep(opt.ClickHoldDelayMs);
                MouseEvent(Win32.MOUSEEVENTF_LEFTUP);
                Thread.Sleep(Math.Max(30, opt.ClickHoldDelayMs));
                MouseEvent(Win32.MOUSEEVENTF_LEFTDOWN);
                if (opt.ClickHoldDelayMs > 0) Thread.Sleep(opt.ClickHoldDelayMs);
                MouseEvent(Win32.MOUSEEVENTF_LEFTUP);
                break;

            default:
                // Positioning は移動のみ
                break;
        }
    }

    private static Color ParseColor(string hex)
    {
        hex = (hex ?? "").Trim();
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length != 6) return Color.White;

        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return Color.FromArgb(r, g, b);
    }

    private static Color GetScreenPixel(int x, int y)
    {
        var hdc = Win32.GetDC(IntPtr.Zero);
        try
        {
            uint c = Win32.GetPixel(hdc, x, y);
            int r = (int)(c & 0x000000FF);
            int g = (int)((c & 0x0000FF00) >> 8);
            int b = (int)((c & 0x00FF0000) >> 16);
            return Color.FromArgb(r, g, b);
        }
        finally
        {
            Win32.ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    private static bool ColorClose(Color a, Color b, int tolerancePercent)
    {
        tolerancePercent = Math.Clamp(tolerancePercent, 0, 100);
        if (tolerancePercent == 0) return a.ToArgb() == b.ToArgb();

        double maxDist = 441.67295593 * tolerancePercent / 100.0;
        double thrSq = maxDist * maxDist;

        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        int distSq = dr * dr + dg * dg + db * db;

        return distSq <= thrSq;
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

    // ===== Prompt =====

    private static class TextInputPrompt
    {
        public static Task<string?> ShowAsync(string title, int timeoutMs, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            var thread = new Thread(() =>
            {
                using var form = new System.Windows.Forms.Form
                {
                    Text = title,
                    Width = 420,
                    Height = 160,
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                    TopMost = true
                };

                var tb = new System.Windows.Forms.TextBox
                {
                    Dock = System.Windows.Forms.DockStyle.Top,
                    Margin = new System.Windows.Forms.Padding(10),
                    Width = 380
                };

                var ok = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Dock = System.Windows.Forms.DockStyle.Right,
                    Width = 80
                };
                var cancel = new System.Windows.Forms.Button
                {
                    Text = "Cancel",
                    DialogResult = System.Windows.Forms.DialogResult.Cancel,
                    Dock = System.Windows.Forms.DockStyle.Right,
                    Width = 80
                };

                var panel = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 45 };
                panel.Controls.Add(cancel);
                panel.Controls.Add(ok);

                form.Controls.Add(tb);
                form.Controls.Add(panel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                System.Windows.Forms.Timer? timer = null;
                if (timeoutMs > 0)
                {
                    timer = new System.Windows.Forms.Timer { Interval = timeoutMs };
                    timer.Tick += (_, __) =>
                    {
                        timer.Stop();
                        form.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                        form.Close();
                    };
                    timer.Start();
                }

                using var reg = token.Register(() =>
                {
                    try
                    {
                        form.BeginInvoke(new Action(() =>
                        {
                            form.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                            form.Close();
                        }));
                    }
                    catch { }
                });

                var result = form.ShowDialog();
                timer?.Stop();
                timer?.Dispose();

                if (result == System.Windows.Forms.DialogResult.OK)
                    tcs.TrySetResult(tb.Text);
                else
                    tcs.TrySetResult(null);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }
    }
}
