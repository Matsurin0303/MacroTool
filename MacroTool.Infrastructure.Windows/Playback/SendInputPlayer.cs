using System;
using System.Threading;
using System.Threading.Tasks;
using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using MacroTool.Infrastructure.Windows.Interop;

namespace MacroTool.Infrastructure.Windows.Playback;

public sealed class SendInputPlayer : IPlayer
{
    public async Task PlayAsync(Macro macro, CancellationToken token)
    {
        foreach (var step in macro.Steps)
        {
            token.ThrowIfCancellationRequested();

            int ms = step.Delay.TotalMilliseconds;
            if (ms > 0)
                await Task.Delay(ms, token);

            Execute(step.Action);
        }
    }

    private static void Execute(MacroAction action)
    {
        switch (action)
        {
            case MouseClick mc:
                DoMouseClick(mc);
                break;

            case KeyDown kd:
                DoKey(kd.Key, isDown: true);
                break;

            case KeyUp ku:
                DoKey(ku.Key, isDown: false);
                break;

            default:
                // 未対応アクションは無視（将来拡張）
                break;
        }
    }

    private static void DoMouseClick(MouseClick mc)
    {
        Win32.SetCursorPos(mc.Point.X, mc.Point.Y);

        // 安定用に少し待つ（不要なら削除可）
        Thread.Sleep(10);

        if (mc.Button == MouseButton.Right)
        {
            MouseEvent(Win32.MOUSEEVENTF_RIGHTDOWN);
            MouseEvent(Win32.MOUSEEVENTF_RIGHTUP);
        }
        else
        {
            MouseEvent(Win32.MOUSEEVENTF_LEFTDOWN);
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
