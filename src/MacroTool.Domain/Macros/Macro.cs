namespace MacroTool.Domain.Macros;
using System.Globalization;
using System.Text.RegularExpressions;

public sealed class Macro
{
    private readonly List<MacroStep> _steps = new();
    public IReadOnlyList<MacroStep> Steps => _steps;

    public int Count => _steps.Count;

    public void Clear() => _steps.Clear();

    /// <summary>
    /// 現在定義されているラベル一覧（空白除外・重複なし・登場順）
    /// </summary>
    public IReadOnlyList<string> GetDefinedLabels()
        => _steps
            .Select(s => NormalizeLabel(s.Label))
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public void AddStep(MacroStep step)
    {
        if (step is null) throw new ArgumentNullException(nameof(step));
        // ルール（不変条件）をここに集約できる
        // - Step/Action は null 不可
        var used = CollectUsedLabels(excludeIndex: -1);
        var normalized = NormalizeStepLabel(step, used);
        _steps.Add(normalized);
    }

    public void AddStep(MacroAction action, string? label = "", string? comment = "")
        => AddStep(new MacroStep(action, label, comment));

    private static string NormalizeLabel(string? label)
        => (label ?? string.Empty).Trim();

    private HashSet<string> CollectUsedLabels(int excludeIndex)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < _steps.Count; i++)
        {
            if (i == excludeIndex) continue;
            var l = NormalizeLabel(_steps[i].Label);
            if (l.Length > 0) used.Add(l);
        }
        return used;
    }

    private static MacroStep NormalizeStepLabel(MacroStep step, HashSet<string> used)
    {
        var label = NormalizeLabel(step.Label);
        if (label.Length == 0)
        {
            // 空は一意性対象外
            return step.Label == string.Empty ? step : new MacroStep(step.Action, "", step.Comment);
        }

        var unique = MakeUniqueLabel(label, used);
        if (unique == step.Label) return step;
        return new MacroStep(step.Action, unique, step.Comment);
    }

    /// <summary>
    /// Label一意化：
    /// - 同名が無ければそのまま
    /// - 同名があれば末尾に数字付与（Jump先 -> Jump先1）
    /// - 末尾に数字があればインクリメント（Jump先1 -> Jump先2）
    /// </summary>
    private static string MakeUniqueLabel(string requested, HashSet<string> used)
    {
        if (!used.Contains(requested))
        {
            used.Add(requested);
            return requested;
        }

        string baseName;
        int n;

        var m = Regex.Match(requested, @"^(.*?)(\d+)$");
        if (m.Success)
        {
            baseName = m.Groups[1].Value;
            n = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) + 1;
        }
        else
        {
            baseName = requested;
            n = 1;
        }

        while (true)
        {
            var candidate = baseName + n.ToString(CultureInfo.InvariantCulture);
            if (!used.Contains(candidate))
            {
                used.Add(candidate);
                return candidate;
            }
            n++;
        }
    }

    /// <summary>完了までの総時間（Delay合計）</summary>
    public TimeSpan TotalDuration()
    {
        // v1.0 では「時間待機」(Wait) がアクションとして入る。
        // ここでは「最悪ケースの目安」として、
        // - Wait: Milliseconds
        // - 条件待機/検出: TimeoutMs（>0のとき）
        // を合算する。
        long ms = 0;

        foreach (var s in _steps)
        {
            ms += s.Action switch
            {
                WaitTimeAction w => w.Milliseconds,
                WaitForPixelColorAction w => w.TimeoutMs > 0 ? w.TimeoutMs : 0,
                WaitForScreenChangeAction w => w.TimeoutMs > 0 ? w.TimeoutMs : 0,
                WaitForTextInputAction w => w.TimeoutMs > 0 ? w.TimeoutMs : 0,
                FindImageAction f => f.TimeoutMs > 0 ? f.TimeoutMs : 0,
                FindTextOcrAction f => f.TimeoutMs > 0 ? f.TimeoutMs : 0,
                _ => 0
            };
        }

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

        if (newStep is null) throw new ArgumentNullException(nameof(newStep));
        var used = CollectUsedLabels(excludeIndex: index);
        _steps[index] = NormalizeStepLabel(newStep, used);
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

        var used = CollectUsedLabels(excludeIndex: -1);
        var normalized = new List<MacroStep>(list.Count);
        foreach (var s in list)
        {
            if (s is null) throw new ArgumentNullException(nameof(steps), "steps contains null.");
            normalized.Add(NormalizeStepLabel(s, used));
        }

        _steps.InsertRange(index, normalized);
    }

    public void ReplaceAllSteps(IEnumerable<MacroStep> steps)
    {
        if (steps is null) throw new ArgumentNullException(nameof(steps));
        _steps.Clear();
        // 一意化しながら入れ直す
        InsertSteps(0, steps);
    }


}
