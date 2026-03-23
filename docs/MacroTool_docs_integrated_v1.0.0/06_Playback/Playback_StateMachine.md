# Playback ステートマシン詳細

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 参照: `docs/06_Playback/Playback_Spec.md`

---

## 1. 目的

本書は、`Playback_Spec.md` で定義した公開状態を、実装向けに**イベント・ガード条件・終了理由**へ分解して整理する。

---

## 2. 公開状態

本版の公開状態は次の2つのみとする。

- `Idle`
- `Playing`

> `Pause / Resume` は Macro_v1.0.0 の対象外とする。

---

## 3. 内部保持情報

再生中は、状態とは別に以下の内部情報を保持してよい。

- 対象Macro
- 実行モード
  - 全体再生
  - 選択Stepのみ再生
  - 選択Stepから再生
  - 選択Stepまで再生
- 現在StepIndex
- 終了条件
- 停止要求フラグ
- 実行中Step情報

これらは **内部制御情報** であり、公開状態を増やす理由にはしない。

---

## 4. イベント

### 4.1 外部イベント
- `PlayRequested`
- `PlaySelectedRequested`
- `PlayFromSelectedRequested`
- `PlayUntilSelectedRequested`
- `StopRequested`

### 4.2 内部イベント
- `ValidationSucceeded`
- `ValidationFailed`
- `PlaybackStarted`
- `StepStarted`
- `StepCompleted`
- `StepFailed`
- `StepErrored`
- `TargetStepReached`
- `PlaybackCompleted`
- `CancellationObserved`

---

## 5. ガード条件

### 5.1 再生開始ガード
再生開始要求は次を満たすときのみ有効。
- 現在状態が `Idle`
- 対象Macroが存在する
- 対象Step範囲が妥当
- Domain整合性検証に成功する

### 5.2 停止要求ガード
- `Playing` 中のみ停止要求を意味のあるイベントとして扱う
- `Idle` 中の `StopRequested` は無視する

---

## 6. 遷移詳細

### 6.1 Idle -> Playing
1. `Play*Requested` を受領する
2. 対象範囲を確定する
3. バリデーションを行う
4. 成功時に `PlaybackStarted`
5. `Playing` へ遷移する

### 6.2 Playing 継続
- `StepStarted` -> 実行 -> `StepCompleted` を繰り返す
- `StepFailed` は分岐制御の材料であり、即時終了理由ではない
- `GoTo` / `Repeat` / `If` により次Stepを決定する

### 6.3 Playing -> Idle
以下のいずれかで `Idle` へ戻る。
- `PlaybackCompleted`
- `TargetStepReached`
- `CancellationObserved`
- `StepErrored`
- `ValidationFailed`（開始拒否時。状態は実質 Idle 維持）

---

## 7. 終了理由との対応

| 事象 | 終了理由 |
|---|---|
| 全Step完了 | `Completed` |
| Stop操作 | `Cancelled` |
| 入力検知等の中断 | `Aborted` |
| 例外・継続不能エラー | `ErrorTerminated` |
| 開始前検証失敗 | `ValidationRejected` |

---

## 8. 部分再生ルール

### 8.1 PlaySelected
- 対象Stepを1件実行して終了する

### 8.2 PlayFromSelected
- 対象Stepから通常再生を開始する

### 8.3 PlayUntilSelected
- 指定Stepの評価完了で終了する
- 指定Stepを「実行前で止める」のか「実行後で止める」のかは `Playback_Spec.md` に従い、**評価完了で終了** とする

---

## 9. 実装上の注意

- 内部的にサブ状態を持ってもよいが、公開状態に昇格させない
- Stop 要求は協調的キャンセルで扱う
- 進捗通知は状態遷移とは別に扱う
- UIの実行中行追従は状態機械ではなく通知機構の責務とする

---

## 10. テスト観点

- Idle 中 StopRequested が無効であること
- ValidationFailed で Playing へ遷移しないこと
- StepFailed で即時終了しないこと
- StepErrored で Idle へ戻ること
- PlayUntilSelected が指定Step評価完了で止まること

