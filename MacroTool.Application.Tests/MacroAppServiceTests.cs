using System;
using MacroTool.Application.Services;
using MacroTool.Domain.Macros;
using Xunit;

namespace MacroTool.Application.Tests;

public class MacroAppServiceTests
{
    [Fact]
    public void Recording_AddsSteps_WithDelayDiff()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer();
        var repo = new FakeRepo();

        var app = new MacroAppService(rec, player, repo);

        // 録画開始
        Assert.True(app.StartRecording(clearExisting: true));

        // 2イベント発火（時刻差 120ms）
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        rec.Raise(t0, new KeyDown(new VirtualKey(65)));
        rec.Raise(t0.AddMilliseconds(120), new KeyUp(new VirtualKey(65)));

        Assert.Equal(2, app.CurrentMacro.Steps.Count);
        Assert.Equal(0, app.CurrentMacro.Steps[0].Delay.TotalMilliseconds);      // 最初は差分0想定（実装次第でOK）
        Assert.Equal(120, app.CurrentMacro.Steps[1].Delay.TotalMilliseconds);
    }
}
