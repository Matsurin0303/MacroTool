using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using MacroTool.Infrastructure.Windows.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MacroTool.Infrastructure.Windows.Recording;

public sealed class LowLevelHookRecorder : IRecorder, IDisposable
{
    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyHook = IntPtr.Zero;

    private Win32.LowLevelMouseProc? _mouseProc;
    private Win32.LowLevelKeyboardProc? _keyProc;
    private readonly Channel<RecordedAction> _channel =
    Channel.CreateUnbounded<RecordedAction>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private CancellationTokenSource? _cts;
    private Task? _pump;
    private Stopwatch? _sw;
    public event EventHandler<RecordedAction>? ActionRecorded;

    public bool IsRecording { get; private set; }

    public bool Start()
    {
        if (IsRecording) return true;

        _sw = Stopwatch.StartNew();

        _cts = new CancellationTokenSource();
        _pump = Task.Run(() => PumpAsync(_cts.Token));

        _mouseProc = MouseHookCallback;
        _keyProc = KeyboardHookCallback;

        _mouseHook = SetMouseHook(_mouseProc);
        _keyHook = SetKeyboardHook(_keyProc);

        IsRecording = _mouseHook != IntPtr.Zero && _keyHook != IntPtr.Zero;

        if (!IsRecording)
            Stop();

        return IsRecording;
    }


    public void Stop()
    {
        if (_mouseHook != IntPtr.Zero) Win32.UnhookWindowsHookEx(_mouseHook);
        if (_keyHook != IntPtr.Zero) Win32.UnhookWindowsHookEx(_keyHook);

        _mouseHook = IntPtr.Zero;
        _keyHook = IntPtr.Zero;

        IsRecording = false;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _sw = null;
    }

    private async Task PumpAsync(CancellationToken token)
    {
        var reader = _channel.Reader;
        while (await reader.WaitToReadAsync(token))
        {
            while (reader.TryRead(out var a))
            {
                ActionRecorded?.Invoke(this, a);
            }
        }
    }


    private IntPtr SetMouseHook(Win32.LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return Win32.SetWindowsHookEx(Win32.WH_MOUSE_LL, proc, Win32.GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr SetKeyboardHook(Win32.LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, proc, Win32.GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsRecording)
        {
            int msg = wParam.ToInt32();
            var info = Marshal.PtrToStructure<Win32.MSLLHOOKSTRUCT>(lParam);

            // ★ 自分がSendInputで注入したイベントだけ除外
            if (info.dwExtraInfo == InjectionTag.Value)
                return Win32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            if (msg == Win32.WM_LBUTTONDOWN || msg == Win32.WM_RBUTTONDOWN)
            {
                var button = (msg == Win32.WM_RBUTTONDOWN) ? MouseButton.Right : MouseButton.Left;
                var action = new MouseClick(new ScreenPoint(info.pt.x, info.pt.y), button);

                var elapsed = _sw?.Elapsed ?? TimeSpan.Zero;
                _channel.Writer.TryWrite(new RecordedAction(elapsed, action));
            }
        }

        return Win32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsRecording)
        {
            int msg = wParam.ToInt32();
            var info = Marshal.PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);

            // ★ 自分がSendInputで注入したイベントだけ除外
            if (info.dwExtraInfo == InjectionTag.Value)
                return Win32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            if (msg == Win32.WM_KEYDOWN || msg == Win32.WM_KEYUP)
            {
                var vk = new VirtualKey((ushort)info.vkCode);
                var elapsed = _sw?.Elapsed ?? TimeSpan.Zero;

                MacroAction action = (msg == Win32.WM_KEYDOWN)
                    ? new KeyDown(vk)
                    : new KeyUp(vk);
                
                _channel.Writer.TryWrite(new RecordedAction(elapsed, action));
            }
        }

        return Win32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
        _mouseProc = null;
        _keyProc = null;
    }
}
