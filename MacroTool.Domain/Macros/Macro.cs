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

    /// <summary>
    /// Stepsの指定位置を新しいStepで置き換える
    /// </summary>
    /// <param name="index"></param>
    /// <param name="newStep"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>

    public void ReplaceStep(int index, MacroStep newStep)
    {
        if (index < 0 || index >= _steps.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _steps[index] = newStep ?? throw new ArgumentNullException(nameof(newStep));
    }

    /// <summary>
    /// Stepsの指定位置のStepを削除する
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>

    public void RemoveStepAt(int index)
    {
        if (index < 0 || index >= _steps.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _steps.RemoveAt(index);
    }

    /// <summary>
    /// Stepsの指定位置のStepを移動する
    /// </summary>
    /// <param name="fromIndex"></param>
    /// <param name="toIndex"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>

    public void MoveStep(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _steps.Count)
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        if (toIndex < 0 || toIndex >= _steps.Count)
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        if (fromIndex == toIndex) return;

        var item = _steps[fromIndex];
        _steps.RemoveAt(fromIndex);
        _steps.Insert(toIndex, item);
    }
    public void InsertSteps(int index, IEnumerable<MacroStep> steps)
    {
        if (steps is null) throw new ArgumentNullException(nameof(steps));

        if (index < 0) index = 0;
        if (index > _steps.Count) index = _steps.Count;

        var list = steps.ToList();
        if (list.Count == 0) return;

        _steps.InsertRange(index, list);
    }

    public void ReplaceAllSteps(IEnumerable<MacroStep> steps)
    {
        if (steps is null) throw new ArgumentNullException(nameof(steps));
        _steps.Clear();
        _steps.AddRange(steps);
    }


}
