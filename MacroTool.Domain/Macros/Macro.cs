using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroTool.Domain.Macros;

public sealed class Macro
{
    private readonly List<MacroStep> _steps = new();
    public IReadOnlyList<MacroStep> Steps => _steps;

    public int Count => _steps.Count;

    public void Clear() => _steps.Clear();

    public void AddStep(MacroStep step)
    {
        // ルール（不変条件）をここに集約できる
        // - Delayは0以上に正規化済み（MacroDelayで保証）
        // - Actionはnull不可（recordなのでnullになりにくい）
        _steps.Add(step);
    }

    public void AddStep(MacroDelay delay, MacroAction action)
        => AddStep(new MacroStep(delay, action));

    /// <summary>完了までの総時間（Delay合計）</summary>
    public TimeSpan TotalDuration()
    {
        long ms = 0;
        foreach (var s in _steps)
            ms += s.Delay.TotalMilliseconds;

        if (ms < 0) ms = 0;
        return TimeSpan.FromMilliseconds(ms);
    }

    /// <summary>再生中の「残り時間」</summary>
    public TimeSpan Remaining(TimeSpan elapsed)
    {
        var remain = TotalDuration() - elapsed;
        return remain < TimeSpan.Zero ? TimeSpan.Zero : remain;
    }
}
