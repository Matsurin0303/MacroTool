using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace MacroTool.Application.Services;

public sealed class MacroAppService : IDisposable
{
    private readonly IRecorder _recorder;
    private readonly IPlayer _player;
    private readonly IMacroRepository _repo;

    private CancellationTokenSource? _playCts;

    private DateTime _stateStartedAt = DateTime.Now;
    private DateTime? _lastRecordAt; // ← DateTime? に変更

    public Macro CurrentMacro { get; } = new();

    public AppState State { get; private set; } = AppState.Stopped;

    public event EventHandler<AppState>? StateChanged;
    public event EventHandler? MacroChanged;
    public event EventHandler<string>? UserNotification; // UIでメッセージ出したい場合用（任意）

    public MacroAppService(IRecorder recorder, IPlayer player, IMacroRepository repo)
    {
        _recorder = recorder;
        _player = player;
        _repo = repo;

        _recorder.ActionRecorded += OnActionRecorded;
    }

    // ===== 状態関連 =====
    public TimeSpan Elapsed()
    {
        if (State is AppState.Recording or AppState.Playing)
            return DateTime.Now - _stateStartedAt;

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
        _stateStartedAt = DateTime.Now;

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
        if (State == AppState.Playing) return;
        if (CurrentMacro.Count == 0) return;

        // 録画中なら止める
        if (State == AppState.Recording)
            _recorder.Stop();

        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = new CancellationTokenSource();

        _stateStartedAt = DateTime.Now;
        SetState(AppState.Playing);

        var token = _playCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await _player.PlayAsync(CurrentMacro, token);
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
                delayMs = (int)(e.Timestamp - _lastRecordAt.Value).TotalMilliseconds;
                if (delayMs < 0) delayMs = 0; // 負値ガード
            }

            _lastRecordAt = e.Timestamp;

            var delay = MacroDelay.FromMilliseconds(delayMs);
            CurrentMacro.AddStep(new MacroStep(delay, e.Action));

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

        // 状態開始時刻は Recording/Playing に入った時のみ更新
        if (newState is AppState.Recording or AppState.Playing)
            _stateStartedAt = DateTime.Now;

        StateChanged?.Invoke(this, newState);
    }

    public void New()
    {
        StopAll();

        CurrentMacro.Clear();
        MacroChanged?.Invoke(this, EventArgs.Empty);

        // 保存先はUI側が持っているので、ここでは触らない（UIで _currentMacroPath を null にする）
    }

    public void Dispose()
    {
        StopAll();
        _recorder.ActionRecorded -= OnActionRecorded;
        _playCts?.Dispose();
    }

}
