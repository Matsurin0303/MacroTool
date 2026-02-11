using MacroTool.Application.Services;
using MacroTool.Domain.Macros;

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
        rec.Raise(TimeSpan.Zero, new KeyDown(new VirtualKey(65)));
        rec.Raise(TimeSpan.FromMilliseconds(120), new KeyUp(new VirtualKey(65)));


        Assert.Equal(2, app.CurrentMacro.Steps.Count);
        Assert.Equal(0, app.CurrentMacro.Steps[0].Delay.TotalMilliseconds);      // 最初は差分0想定（実装次第でOK）
        Assert.Equal(120, app.CurrentMacro.Steps[1].Delay.TotalMilliseconds);
    }

    [Fact]
    public void DeleteStep_WhenStopped_RemovesStep()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer();
        var repo = new FakeRepo();
        var app = new MacroAppService(rec, player, repo);

        app.CurrentMacro.AddStep(new MacroStep(MacroDelay.FromMilliseconds(0), new KeyDown(new VirtualKey(65))));
        app.CurrentMacro.AddStep(new MacroStep(MacroDelay.FromMilliseconds(0), new KeyUp(new VirtualKey(65))));

        app.DeleteStep(0);

        Assert.Equal(1, app.CurrentMacro.Count);
        Assert.Equal("KeyUp", app.CurrentMacro.Steps[0].Action.Kind);
    }
    [Fact]
    public void UndoRedo_Works_For_UpdateStepMetadata()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer();
        var repo = new FakeRepo();
        var app = new MacroAppService(rec, player, repo);

        app.CurrentMacro.AddStep(new MacroStep(MacroDelay.FromMilliseconds(0), new KeyDown(new VirtualKey(65)), "A", ""));

        app.UpdateStepMetadata(0, "B", "C");
        Assert.Equal("B", app.CurrentMacro.Steps[0].Label);

        app.Undo();
        Assert.Equal("A", app.CurrentMacro.Steps[0].Label);

        app.Redo();
        Assert.Equal("B", app.CurrentMacro.Steps[0].Label);
    }

}
