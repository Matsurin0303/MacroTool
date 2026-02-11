using MacroTool.Domain.Macros;

namespace MacroTool.Domain.Tests;

public class MacroTests
{
    [Fact]
    public void TotalDuration_SumsDelays()
    {
        var m = new Macro();
        m.AddStep(MacroDelay.FromMilliseconds(100), new KeyDown(new VirtualKey(65)));
        m.AddStep(MacroDelay.FromMilliseconds(250), new KeyUp(new VirtualKey(65)));

        Assert.Equal(350, (int)m.TotalDuration().TotalMilliseconds);
    }
}
