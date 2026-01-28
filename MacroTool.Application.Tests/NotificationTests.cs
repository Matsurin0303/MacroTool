using System;
using System.Threading;
using MacroTool.Application.Services;
using MacroTool.Domain.Macros;
using Xunit;

namespace MacroTool.Application.Tests;

public class NotificationTests
{
    [Fact]
    public void Play_WhenPlayerThrows_RaisesUserNotification()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer { ThrowOnPlay = true };
        var repo = new FakeRepo();

        var app = new MacroAppService(rec, player, repo);

        // 再生できるように1ステップ入れる
        app.CurrentMacro.AddStep(MacroDelay.Zero, new KeyDown(new VirtualKey(65)));

        string? message = null;
        app.UserNotification += (_, msg) => message = msg;

        app.Play();

        // 非同期で戻る可能性があるので少し待つ（必要最小限）
        Thread.Sleep(50);

        Assert.NotNull(message);
        Assert.Contains("再生", message);
    }
}
