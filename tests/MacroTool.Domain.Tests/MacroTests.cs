using MacroTool.Domain.Macros;

namespace MacroTool.Domain.Tests;

public class MacroTests
{
    [Fact]
    public void TotalDuration_SumsWaitActions()
    {
        var m = new Macro();
        m.AddStep(new MacroStep(new WaitTimeAction { Milliseconds = 100 }));
        m.AddStep(new MacroStep(new KeyPressAction { Option = KeyPressOption.Down, Key = new VirtualKey(65), Count = 1 }));
        m.AddStep(new MacroStep(new WaitTimeAction { Milliseconds = 250 }));
        m.AddStep(new MacroStep(new KeyPressAction { Option = KeyPressOption.Up, Key = new VirtualKey(65), Count = 1 }));

        Assert.Equal(350, (int)m.TotalDuration().TotalMilliseconds);
    }
}
