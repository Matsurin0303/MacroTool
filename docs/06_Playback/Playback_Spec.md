# Playback 状態遷移仕様
- Version: **Macro_v1.0.0**
- 対象: MacroTool（WinForms / C# / DDD + TDD）
- 方針: **Pause/Resume を対象外とし、公開状態は Idle / Playing のみとする**

---

## 1. 状態一覧

本バージョンでの公開状態は以下の2つとする。

| 状態 | 説明 |
|---|---|
| Idle | 再生していない状態 |
| Playing | 再生中状態 |

※ Pause / Resume は Macro_v1.0.0 の対象外とする

---

## 2. イベント一覧

### 2.1 外部イベント（ユーザー操作）

| イベント | 説明 |
|---|---|
| PlayRequested | 通常再生開始 |
| PlaySelectedRequested | 選択行のみ再生 |
| PlayFromSelectedRequested | 選択行から再生 |
| PlayUntilSelectedRequested | 選択行まで再生 |
| StopRequested | 再生停止要求 |

---

### 2.2 内部イベント（再生処理）

| イベント | 説明 |
|---|---|
| PlaybackStarted | 再生開始 |
| StepStarted | Step実行開始 |
| StepCompleted | Step正常完了 |
| StepFailed | Step失敗（分岐用） |
| StepErrored | Step異常終了 |
| TargetStepReached | 指定Step到達 |
| PlaybackCompleted | 全Step完了 |
| CancellationObserved | 停止要求検知 |

---

## 3. 状態遷移表

### 3.1 Idle 状態

| 現在状態 | イベント | 次状態 | 備考 |
|---|---|---|---|
| Idle | PlayRequested | Playing | 通常再生開始 |
| Idle | PlaySelectedRequested | Playing | 単一Step再生 |
| Idle | PlayFromSelectedRequested | Playing | 指定位置から再生 |
| Idle | PlayUntilSelectedRequested | Playing | 指定位置まで再生 |
| Idle | StopRequested | Idle | 無効（何もしない） |

---

### 3.2 Playing 状態

| 現在状態 | イベント | 次状態 | 備考 |
|---|---|---|---|
| Playing | StopRequested | Idle | 停止要求 |
| Playing | CancellationObserved | Idle | 停止処理完了 |
| Playing | PlaybackCompleted | Idle | 正常終了 |
| Playing | StepErrored | Idle | 異常終了 |
| Playing | TargetStepReached | Idle | 部分再生終了 |

---

## 4. 状態遷移ルール

### 4.1 再生開始
- Idle 状態でのみ再生開始可能
- 再生開始前に Validator を実行する
- 検証失敗時は状態遷移しない（Idleのまま）

---

### 4.2 再生中
- Stepは順次実行される
- StepFailed は再生継続（分岐制御用）
- StepErrored は即時終了

---

### 4.3 停止処理
- StopRequested は即時反映を試みる
- 実行中Stepが停止不能な場合は
  - キャンセル可能境界で停止する
- CancellationObserved 発生時に Idle へ遷移する

---

### 4.4 部分再生
- PlaySelected
  - 対象Step実行完了で終了
- PlayFromSelected
  - 通常再生と同様
- PlayUntilSelected
  - 指定Stepの評価完了時に終了

---

## 5. 終了理由

Playbackの終了理由は以下とする。

| 種別 | 説明 |
|---|---|
| Completed | 全Step正常完了 |
| Cancelled | Stop操作による停止 |
| Aborted | 入力検知等による停止 |
| ErrorTerminated | 例外による停止 |
| ValidationRejected | 再生開始拒否 |

---

## 6. 無効イベントの扱い

| 状態 | イベント | 動作 |
|---|---|---|
| Idle | StopRequested | 無視 |
| Playing | Play系イベント | 無視 |

---

## 7. 副作用

### 7.1 再生開始時
- Validator実行
- CancelToken生成
- 実行コンテキスト初期化
- 進捗通知開始

---

### 7.2 再生中
- Step開始通知
- Step完了通知
- 進捗更新通知

---

### 7.3 終了時
- CancelToken破棄
- 最終結果通知
- 状態 Idle へ遷移

---

## 8. 補足

- Macro_v1.0.0 では Pause / Resume は実装しない
- 再開機能は提供しない
- 再生中のユーザー介入は Stop のみとする
- 停止後は必ず Idle 状態に戻る