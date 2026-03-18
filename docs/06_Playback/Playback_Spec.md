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
- 変数ストア生成
- 全変数を `Undefined` へ初期化
- 進捗通知開始

---

### 7.2 再生中
- Step開始通知
- Step完了通知
- 進捗更新通知

---

### 7.3 終了時
- CancelToken破棄
- 変数ストア破棄
- 最終結果通知
- 状態 Idle へ遷移

---

### 7.4 変数コンテキスト
- 変数スコープは 1回のPlayback実行単位とする
- Playback 終了後に変数値は保持しない
- 実行時型は `String` または `Number` とする
- 未設定状態は `Undefined` とする
- `Save Coordinate` により保存される X / Y は `Number` とする
- 設定 `Reset variables and list counter on each playback cycle` が有効な場合、Playback Repeat の cycle 開始時に全変数を `Undefined` へ初期化する
- Repeat Action の内部周回では、上記設定による初期化は行わない

---

## 7.5 Repeat 実行ルール
- Repeat の対象範囲は `startLabel` の行から Repeat 行の直前までとする
- `startLabel` が解決できないマクロは実行対象とせず、保存時 / 読込時エラーで除外する
- Repeat のネストは禁止とし、ネストを含むマクロは妥当なマクロとして扱わない
- `Infinite` は Stop 操作またはエラー発生まで継続する
- `finishGoTo` は繰り返し完了後に1回だけ適用する

## 7.6 Playback Repeat 実行ルール
- Playback Repeat は現在の再生対象全体を1 cycle として繰り返す
- 対象全体とは、通常再生時はマクロ全体、`Play until selected` / `Play from selected` / `Play selected` 時はその再生対象範囲を指す
- Playback Repeat が有効な場合でも、Macro 内の `Repeat` Action は内部制御としてそのまま実行する
- `Repeat(Infinite)` が存在する場合は内側の `Repeat` が優先され、外側の Playback Repeat の次 cycle へは到達しない
- 設定 `Reset variables and list counter on each playback cycle` が有効な場合、Playback Repeat の各 cycle 開始時に変数およびリストカウンタを初期化する

## 7.7 Playback Speed 適用ルール
- `Playback Speed` は `Wait` Action の待機時間にのみ適用する
- `FindImage` / `FindText` / `WaitForTextInput` のタイムアウトおよびポーリング間隔には適用しない
- `MouseClick` / `MouseMove` / `MouseWheel` / `KeyPress` には適用しない


## 7.8 Detection 実行ルール
- `FindImage` / `FindTextOcr` は、各ポーリング時点の**最新画面**を対象として再評価する
- `WaitingMs` 経過まで未検出の場合は、エラーにせず `FalseGoTo` 側へ分岐する
- 画像読込失敗、OCR 実行失敗、画像検出サービス失敗などの処理異常は `StepErrored` とする
- `FindImage` で複数候補が存在する場合は、一致度が最も高い候補を採用し、同点時は左上の候補を採用する
- `FindTextOcr` は前後空白を除去して完全一致で判定し、複数候補が存在する場合は最も左上の候補を採用する
- 検出成功時の代表点は `MousePosition` で算出し、マウス操作と `Save Coordinate` の保存座標に共通使用する

## 8. 補足

- Macro_v1.0.0 では Pause / Resume は実装しない
- 再開機能は提供しない
- 再生中のユーザー介入は Stop のみとする
- 停止後は必ず Idle 状態に戻る