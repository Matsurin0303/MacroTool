using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using System.Text.Json;


namespace MacroTool.Application.Services;

public sealed class MacroAppService : IDisposable
{
    private readonly IRecorder _recorder;
    private readonly IPlayer _player;
    private readonly IMacroRepository _repo;
    private const int MaxUndoHistory = 100;

    private IReadOnlyList<int>? _playIndexMap;

    private readonly Stack<List<MacroStep>> _undo = new();
    private readonly Stack<List<MacroStep>> _redo = new();

    private CancellationTokenSource? _playCts;

    private readonly System.Diagnostics.Stopwatch _stateSw = new();
    private TimeSpan? _lastRecordAt;

    public Macro CurrentMacro { get; } = new();
    public AppState State { get; private set; } = AppState.Stopped;

    private List<MacroStep> SnapshotSteps()
    {
        // 現在のStepsをList化（MacroStepが実質不変ならこれで十分）
        return CurrentMacro.Steps.ToList();
    }

    private void PushUndoPoint()
    {
        // 現在状態を保存
        _undo.Push(SnapshotSteps());

        // 上限を超えたら古いものを削除
        if (_undo.Count > MaxUndoHistory)
        {
            // Stackは下から削除できないので、一旦配列にして詰め直す
            var arr = _undo.Reverse().Take(MaxUndoHistory).ToList();
            _undo.Clear();

            // 元の順序に戻す（最新が上になるように）
            for (int i = arr.Count - 1; i >= 0; i--)
                _undo.Push(arr[i]);
        }

        // 新しい操作が入ったらRedoは無効
        _redo.Clear();
    }

    public bool CanUndo => _undo.Count > 0 && State == AppState.Stopped;
    public bool CanRedo => _redo.Count > 0 && State == AppState.Stopped;


    public event EventHandler<AppState>? StateChanged;
    public event EventHandler? MacroChanged;
    public event EventHandler<string>? UserNotification; // UIでメッセージ出したい場合用（任意）
    public event EventHandler<StepExecutingEventArgs>? StepExecuting;

    public MacroAppService(IRecorder recorder, IPlayer player, IMacroRepository repo)
    {
        _recorder = recorder;
        _player = player;
        _repo = repo;

        _recorder.ActionRecorded += OnActionRecorded;
        _player.StepExecuting += OnPlayerStepExecuting;

    }

    // ===== 状態関連 =====
    public TimeSpan Elapsed()
    {
        if (State is AppState.Recording or AppState.Playing)
            return _stateSw.Elapsed;
        return TimeSpan.Zero;
    }

    /// <summary>
    /// 右側に出す「完了まで」。再生中は残り、停止/録画中は総再生時間。
    /// </summary>
    public TimeSpan UntilDone()
    {
        var total = CurrentMacro.TotalDuration();
        if (State == AppState.Playing)
            return CurrentMacro.Remaining(Elapsed());

        return total;
    }

    public int ActionCount => CurrentMacro.Count;

    // ===== 録画 =====

    public bool StartRecording(bool clearExisting)
    {
        StopAll();

        if (clearExisting) CurrentMacro.Clear();

        _lastRecordAt = null;               // ← ここが重要

        _undo.Clear();
        _redo.Clear();

        bool ok = _recorder.Start();
        if (!ok)
        {
            SetState(AppState.Stopped);
            UserNotification?.Invoke(this, "録画開始に失敗しました。管理者権限が必要な場合があります。");
            return false;
        }

        SetState(AppState.Recording);
        return true;
    }

    public void StopRecording()
    {
        if (State != AppState.Recording) return;
        _recorder.Stop();
        SetState(AppState.Stopped);
    }

    // ===== 再生 =====
    public void Play()
    {
        var map = Enumerable.Range(0, CurrentMacro.Count).ToList();
        PlayInternal(CurrentMacro, map);
    }

    /// <summary>
    /// 選択行まで再生（0..inclusiveIndex）
    /// </summary>
    public void PlayUntil(int inclusiveIndex)
    {
        if (inclusiveIndex < 0) return;
        if (inclusiveIndex >= CurrentMacro.Count) { Play(); return; }
        var(m, map) = SliceWithMap(0, inclusiveIndex + 1);
        PlayInternal(m, map);
    }

    /// <summary>
    /// 選択行から再生（startIndex..end）
    /// </summary>
    public void PlayFrom(int startIndex)
    {
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= CurrentMacro.Count) return;
        var(m, map) = SliceWithMap(startIndex, CurrentMacro.Count - startIndex);
        PlayInternal(m, map);
    }

    /// <summary>
    /// 選択行のみ（昇順）を再生
    /// </summary>
    public void PlaySelected(IEnumerable<int> indices)
    {
        var list = indices
            .Where(i => i >= 0 && i < CurrentMacro.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToList();
        if (list.Count == 0) return;

        var macro = new Macro();
        foreach (var i in list)
            macro.AddStep(CurrentMacro.Steps[i]);

        PlayInternal(macro, list);
    }

    private (Macro macro, IReadOnlyList<int> map) SliceWithMap(int start, int count)
    {
        var m = new Macro();
        var map = new List<int>(count);

        for (int i = 0; i < count; i++)
        {
            var src = start + i;
            map.Add(src);
            m.AddStep(CurrentMacro.Steps[src]);
        }
        return (m, map);
    }

    private void PlayInternal(Macro macro, IReadOnlyList<int> indexMap)
    {
        if (State == AppState.Playing) return;
        if (macro.Count == 0) return;

        // 録画中なら止める
        if (State == AppState.Recording)
            _recorder.Stop();

        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = new CancellationTokenSource();
        _playIndexMap = indexMap;

        SetState(AppState.Playing);

        var token = _playCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await _player.PlayAsync(macro, token);
            }
            catch (OperationCanceledException)
            {
                // StopAllによるキャンセルは正常
            }
            catch (Exception ex)
            {
                UserNotification?.Invoke(this, $"再生中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                _playIndexMap = null;
                // Play中のままならStoppedへ戻す
                if (State == AppState.Playing)
                    SetState(AppState.Stopped);
            }
        }, token);
    }

    // ===== 停止 =====

    public void StopAll()
    {
        _recorder.Stop();

        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = null;

        SetState(AppState.Stopped);
    }

    // ===== 保存/読み込み =====
    public void Save(string path) => _repo.Save(path, CurrentMacro);

    public void Load(string path)
    {
        StopAll();

        var loaded = _repo.Load(path);

        CurrentMacro.Clear();
        _undo.Clear();
        _redo.Clear();

        foreach (var s in loaded.Steps)
            CurrentMacro.AddStep(s);

        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    // ===== 録画イベント =====
    private void OnActionRecorded(object? sender, RecordedAction e)
    {
        if (State != AppState.Recording) return;

        try
        {
            int delayMs = 0;

            if (_lastRecordAt is not null)
            {
                delayMs = (int)(e.Elapsed - _lastRecordAt.Value).TotalMilliseconds;
                if (delayMs < 0) delayMs = 0;
            }

            _lastRecordAt = e.Elapsed;

            // v1.0: 待機はアクションとして表現する
            if (delayMs > 0)
                CurrentMacro.AddStep(new MacroStep(new WaitTimeAction { Milliseconds = delayMs }));

            CurrentMacro.AddStep(new MacroStep(e.Action, label: "", comment: ""));
            MacroChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            UserNotification?.Invoke(this, $"録画反映で例外: {ex.Message}");
        }
    }

    private void SetState(AppState newState)
    {
        if (State == newState) return;

        State = newState;

        if (newState is AppState.Recording or AppState.Playing)
        {
            _stateSw.Restart();
        }
        else
        {
            _stateSw.Reset();
        }

        StateChanged?.Invoke(this, newState);
    }


    public void New()
    {
        StopAll();
        CurrentMacro.Clear();
        _undo.Clear();
        _redo.Clear();
        MacroChanged?.Invoke(this, EventArgs.Empty);

        // 保存先はUI側が持っているので、ここでは触らない（UIで _currentMacroPath を null にする）
    }

    public void Dispose()
    {
        _player.StepExecuting -= OnPlayerStepExecuting;
        StopAll();
        _recorder.ActionRecorded -= OnActionRecorded;
        _playCts?.Dispose();

    }

    private void OnPlayerStepExecuting(object? sender, StepExecutingEventArgs e)
    {
        var map = _playIndexMap;
        var idx = e.StepIndex;
        if (map is not null && idx >= 0 && idx<map.Count)
            idx = map[idx];

        StepExecuting?.Invoke(this, new StepExecutingEventArgs(idx));
    }

    public void UpdateStepMetadata(int index, string? label, string? comment)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ編集できます。");
            return;
        }

        if (index < 0 || index >= CurrentMacro.Count)
            return;

        label ??= "";
        comment ??= "";

        // 既存ステップを取得
        var old = CurrentMacro.Steps[index];

        // 変更が無いなら何もしない（無駄なDirty/更新を防ぐ）
        if (old.Label == label && old.Comment == comment)
            return;

        PushUndoPoint();

        // Stepを置換（Actionは維持）
        var replaced = new MacroStep(old.Action, label, comment);
        CurrentMacro.ReplaceStep(index, replaced);

        // UI更新通知（既存のイベントを流用）
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteStep(int index)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ削除できます。");
            return;
        }

        if (index < 0 || index >= CurrentMacro.Count)
            return;
        PushUndoPoint();

        CurrentMacro.RemoveStepAt(index);
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteSteps(IEnumerable<int> indices)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ削除できます。");
            return;
        }

        var list = indices
            .Where(i => i >= 0 && i < CurrentMacro.Count)
            .Distinct()
            .OrderByDescending(i => i)
            .ToList();

        if (list.Count == 0) return;

        foreach (var i in list)
            CurrentMacro.RemoveStepAt(i);

        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ClipboardDto
    {
        public int Version { get; set; } = 2;
        public List<ClipboardStepDto> Steps { get; set; } = new();
    }

    private sealed class ClipboardStepDto
    {
        public MacroAction Action { get; set; } = new WaitTimeAction();
        public string Label { get; set; } = "";
        public string Comment { get; set; } = "";
    }
    public string CopyStepsToClipboardText(IEnumerable<int> indices)
    {
        if (State != AppState.Stopped)
            throw new InvalidOperationException("停止中のみ操作できます。");

        var list = indices
            .Distinct()
            .Where(i => i >= 0 && i < CurrentMacro.Count)
            .OrderBy(i => i)
            .ToList();

        if (list.Count == 0) return "";

        var dto = new ClipboardDto
        {
            Steps = list.Select(i => ToClipboardStep(CurrentMacro.Steps[i])).ToList()
        };

        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false });
    }

    public string CutStepsToClipboardText(IEnumerable<int> indices)
    {
        if (State != AppState.Stopped)
            throw new InvalidOperationException("停止中のみ操作できます。");

        var text = CopyStepsToClipboardText(indices);
        if (string.IsNullOrWhiteSpace(text)) return "";

        // Cut は後ろから削除
        var del = indices
            .Distinct()
            .Where(i => i >= 0 && i < CurrentMacro.Count)
            .OrderByDescending(i => i)
            .ToList();
        PushUndoPoint();

        foreach (var i in del)
            CurrentMacro.RemoveStepAt(i);

        MacroChanged?.Invoke(this, EventArgs.Empty);
        return text;
    }

    public int PasteStepsFromClipboardText(int insertIndex, string clipboardText)
    {
        if (State != AppState.Stopped)
            throw new InvalidOperationException("停止中のみ操作できます。");

        if (string.IsNullOrWhiteSpace(clipboardText))
            return 0;

        ClipboardDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ClipboardDto>(clipboardText);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("クリップボードの内容が不正です（JSON解析に失敗）。", ex);
        }

        if (dto is null || dto.Steps is null || dto.Steps.Count == 0)
            return 0;

        var steps = dto.Steps.Select(FromClipboardStep).ToList();

        PushUndoPoint();
        CurrentMacro.InsertSteps(insertIndex, steps);
        MacroChanged?.Invoke(this, EventArgs.Empty);
        return steps.Count;
    }

    private static ClipboardStepDto ToClipboardStep(MacroTool.Domain.Macros.MacroStep step)
    {
        return new ClipboardStepDto
        {
            Action = step.Action,
            Label = step.Label ?? "",
            Comment = step.Comment ?? ""
        };
    }

    private static MacroTool.Domain.Macros.MacroStep FromClipboardStep(ClipboardStepDto dto)
    {
        if (dto.Action is null)
            throw new InvalidDataException("クリップボードのStepに Action がありません。");

        return new MacroTool.Domain.Macros.MacroStep(dto.Action, dto.Label ?? "", dto.Comment ?? "");
    }

    // ===== 編集（挿入/置換） =====

    public void InsertStep(int index, MacroStep step)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ編集できます。");
            return;
        }

        PushUndoPoint();
        CurrentMacro.InsertSteps(index, new[] { step });
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InsertSteps(int index, IEnumerable<MacroStep> steps)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ編集できます。");
            return;
        }

        var list = steps?.ToList() ?? new List<MacroStep>();
        if (list.Count == 0) return;

        PushUndoPoint();
        CurrentMacro.InsertSteps(index, list);
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ReplaceAction(int index, MacroAction action)
    {
        if (State != AppState.Stopped)
        {
            UserNotification?.Invoke(this, "停止中のみ編集できます。");
            return;
        }

        if (index < 0 || index >= CurrentMacro.Count) return;
        var old = CurrentMacro.Steps[index];

        PushUndoPoint();
        CurrentMacro.ReplaceStep(index, new MacroStep(action, old.Label, old.Comment));
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }
    public void Undo()
    {
        if (State != AppState.Stopped) return;
        if (_undo.Count == 0) return;

        // 現在をRedoへ
        _redo.Push(SnapshotSteps());

        // Undoから復元
        var prev = _undo.Pop();
        CurrentMacro.ReplaceAllSteps(prev);

        MacroChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (State != AppState.Stopped) return;
        if (_redo.Count == 0) return;

        // 現在をUndoへ
        _undo.Push(SnapshotSteps());

        // Redoから復元
        var next = _redo.Pop();
        CurrentMacro.ReplaceAllSteps(next);

        MacroChanged?.Invoke(this, EventArgs.Empty);
    }
}
