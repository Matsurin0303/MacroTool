using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Domain.Macros;
using MacroTool.Infrastructure.Windows.Interop;
using Microsoft.Extensions.Options;

namespace MacroTool.Infrastructure.Windows.Playback;

public sealed class SendInputPlayer : IPlayer
{
    private readonly PlaybackOptions _opt;

    public SendInputPlayer(IOptions<PlaybackOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task PlayAsync(Macro macro, CancellationToken token)
    {
        foreach (var step in macro.Steps)
        {
            token.ThrowIfCancellationRequested();

            int ms = step.Delay.TotalMilliseconds;
            if (ms > 0)
                await Task.Delay(ms, token);

            await ExecuteAsync(step.Action, token);
        }
    }

    private async Task ExecuteAsync(MacroAction action, CancellationToken token)
    {
        switch (action)
        {
            case MouseClick mc:
                await DoMouseClickAsync(mc, token);
                break;

            case KeyDown kd:
                DoKey(kd.Key, isDown: true);
                break;

            case KeyUp ku:
                DoKey(ku.Key, isDown: false);
                break;

            default:
                break;
        }
    }

    private async Task StabilizeAsync(int ms, CancellationToken token)
    {
        if (!_opt.EnableStabilizeWait) return;
        if (ms <= 0) return;
        await Task.Delay(ms, token);
    }

    private async Task DoMouseClickAsync(MouseClick mc, CancellationToken token)
    {
        Win32.SetCursorPos(mc.Point.X, mc.Point.Y);
        await StabilizeAsync(_opt.CursorSettleDelayMs, token);

        if (mc.Button == MouseButton.Right)
        {
            MouseEvent(Win32.MOUSEEVENTF_RIGHTDOWN);
            await StabilizeAsync(_opt.ClickHoldDelayMs, token);
            MouseEvent(Win32.MOUSEEVENTF_RIGHTUP);
        }
        else
        {
            MouseEvent(Win32.MOUSEEVENTF_LEFTDOWN);
            await StabilizeAsync(_opt.ClickHoldDelayMs, token);
            MouseEvent(Win32.MOUSEEVENTF_LEFTUP);
        }
    }

    private static void MouseEvent(uint flags)
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
                    dwExtraInfo = InjectionTag.Value
                }
            }
        };

        Win32.SendInput(1, input, MarshalSizeOfInput());
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

        Win32.SendInput(1, input, MarshalSizeOfInput());
    }

    private static int MarshalSizeOfInput()
        => System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.INPUT));
}
