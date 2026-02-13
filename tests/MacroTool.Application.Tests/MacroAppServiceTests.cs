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
        rec.Raise(TimeSpan.Zero, new KeyPressAction { Option = KeyPressOption.Down, Key = new VirtualKey(65), Count = 1 });
        rec.Raise(TimeSpan.FromMilliseconds(120), new KeyPressAction { Option = KeyPressOption.Up, Key = new VirtualKey(65), Count = 1 });

        // v1.0: 差分は Wait アクションとして挿入される
        Assert.Equal(3, app.CurrentMacro.Steps.Count);

        Assert.Equal("KeyPress", app.CurrentMacro.Steps[0].Action.Kind);
        Assert.Equal("Wait", app.CurrentMacro.Steps[1].Action.Kind);
        Assert.Equal(120, ((WaitTimeAction)app.CurrentMacro.Steps[1].Action).Milliseconds);
        Assert.Equal("KeyPress", app.CurrentMacro.Steps[2].Action.Kind);
    }

    [Fact]
    public void DeleteStep_WhenStopped_RemovesStep()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer();
        var repo = new FakeRepo();
        var app = new MacroAppService(rec, player, repo);

        app.CurrentMacro.AddStep(new MacroStep(new KeyPressAction { Option = KeyPressOption.Down, Key = new VirtualKey(65), Count = 1 }));
        app.CurrentMacro.AddStep(new MacroStep(new KeyPressAction { Option = KeyPressOption.Up, Key = new VirtualKey(65), Count = 1 }));

        app.DeleteStep(0);

        Assert.Equal(1, app.CurrentMacro.Count);
        Assert.Equal("KeyPress", app.CurrentMacro.Steps[0].Action.Kind);
        Assert.Equal(KeyPressOption.Up, ((KeyPressAction)app.CurrentMacro.Steps[0].Action).Option);
    }
    [Fact]
    public void UndoRedo_Works_For_UpdateStepMetadata()
    {
        var rec = new FakeRecorder();
        var player = new FakePlayer();
        var repo = new FakeRepo();
        var app = new MacroAppService(rec, player, repo);

        app.CurrentMacro.AddStep(new MacroStep(new KeyPressAction { Option = KeyPressOption.Down, Key = new VirtualKey(65), Count = 1 }, "A", ""));

        app.UpdateStepMetadata(0, "B", "C");
        Assert.Equal("B", app.CurrentMacro.Steps[0].Label);

        app.Undo();
        Assert.Equal("A", app.CurrentMacro.Steps[0].Label);

        app.Redo();
        Assert.Equal("B", app.CurrentMacro.Steps[0].Label);
    }

}
