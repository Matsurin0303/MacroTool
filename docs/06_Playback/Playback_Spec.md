# 再生（Playback）仕様書

- Version: **Macro_v1.0.0**
- 更新日: 2026-02-26
- 対象: MacroTool（WinForms / C#）
- 参照: `docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md` / `docs/04_Domain/Domain_Model.md`

本書は、マクロの **再生（実行）** に関する仕様を定義する。  
※「将来実装予定」と明記された機能は本書の対象外。

---

## 1. スコープ

### 1.1 対象（Macro_v1.0.0）
- Play / Stop / Play selected / Play from selected / Play until selected
- 実行中ステップの自動選択（UI追従）
- 実行速度（Playback Speed）
- Repeat / GoTo / If
- Wait / Wait for pixel color / Wait for screen changes / Wait for text input
- Find image / Find text (OCR)
- Embed macro file / Execute program
- 変数（Variable）の読み書き（保存先指定があるもの）

### 1.2 対象外
- 仕様書で「将来実装予定」とされる機能一式
  - 例：Wait for hotkey press, Wait for file, Capture text/image, Barcode/QR, 2-8-6〜2-8-14 など

---

## 2. 用語

| 用語 | 説明 |
|---|---|
| Step | マクロの1行（順序を持つ） |
| Action | Stepが持つ具体的動作 |
| PC（Program Counter） | 現在実行位置（Step index） |
| Success / Failure | Actionの判定結果（分岐に利用） |
| Cancel | ユーザー操作等により即時中断（結果は“中断”） |
| Abort | 設定により入力検知で中断（Cancelと同義に扱う） |
| Error | 例外・致命条件で実行継続不可（結果は“失敗”） |

---

## 3. 再生コマンドの仕様

### 3.1 Play（全体再生）
- 開始位置：先頭（PC=0）
- 終了条件：PC が最終行を超えた時点で完了（Success）

### 3.2 Play selected（選択行のみ）
- 開始位置：選択行（PC=selected）
- 終了条件：**そのStepを1回評価した時点で完了**
  - ただし Step が GoTo/Repeat/If を含む場合でも、**分岐先を追跡しない**（1Stepのみ）
  - 例：GoToAction を選択行で再生 → GoTo 自体を評価して完了

### 3.3 Play from selected（選択行から再生）
- 開始位置：選択行（PC=selected）
- 終了条件：全体再生と同じ

### 3.4 Play until selected（選択行まで再生）
- 開始位置：先頭（PC=0）
- 終了条件：PC が **選択行を評価し終えた時点**で完了
  - 途中で GoTo/Repeat/If により PC が後方/前方に移動した場合でも、**実際に評価したStep**の流れに従う
  - 無限ループ等に備えて「最大ステップ数」または「最大実行時間」を内部安全策として設けてもよい（UI仕様外のためログのみ）

### 3.5 Stop（停止）
- 再生中に Stop が押された場合：**Cancel**（即時中断）
- Stop はできるだけ早く反映する
  - 待機中/検出待機中：監視ループを抜けて中断
  - 外部プログラム起動中：起動済みプロセスは終了させない（本版では未規定）。ただし再生は中断する。

---

## 4. 再生中UI挙動（必須）

### 4.1 実行中ステップの自動選択
- 再生中は、実行中の Step を **リスト上で自動選択状態**にする
- スクロール位置は、選択行が見えるように必要に応じて追従してよい（UI実装）

### 4.2 再生中の編集制限
- Playing 中は、Stepの追加/削除/編集/並べ替えを禁止（操作を無効化）
- ただし Stop（中断）は常に可能

---

## 5. 再生前検証（推奨）

再生開始時に MacroValidator を実行し、致命的な不整合がある場合は開始しない。

**致命例**
- Area系 SearchArea なのに Rect 未設定
- GoToTarget が存在しない Label を参照
- Repeat の startLabel が存在しない

---

## 6. 実行モデル

### 6.1 基本ループ
1. PC の Step を取得
2. Step.Action を評価（ActionExecutor）
3. 評価結果に応じて次の PC を決定（FlowResolver）
4. PC が範囲外になれば終了

### 6.2 次行遷移（デフォルト）
- Action が分岐を持たない場合：`PC = PC + 1`

### 6.3 GoTo 解決（共通）
GoTo 指定可能値：
- Start：PC=0
- End：PC=lastIndex（最終行）
- Next：PC=PC+1
- Label：Label行の index

**境界**
- Next が lastIndex+1 になった場合は終了（Success）

---

## 7. Action別の再生仕様

> ここでは「成功/失敗」判定と、分岐（trueGoTo/falseGoTo）と、必要な副作用（変数保存等）を定義する。

### 7.1 Mouse
- Click / Move / Wheel は実行できたら Success
- OS側要因で実行不能（SendInput失敗等）は Error（Failure）

### 7.2 Key
- Key press は実行できたら Success
- Hotkey は UIで Key press 群へ展開済みの前提（再生時は Key press のみを評価）

### 7.3 Wait（時間待機）
- 指定ミリ秒待機して Success
- waitingMs=0 の場合は待機せず Success
- Cancel が来たら中断

### 7.4 Wait for pixel color
- 監視ループ：一定周期（実装依存）で指定座標の色を取得し比較
- timeout：`waitingMs` 経過で打ち切り（Failure）
- 判定：
  - 色一致 → Success
  - タイムアウト → Failure
- 分岐：
  - Success → `trueGoTo` に従って PC を更新
  - Failure → `falseGoTo` に従って PC を更新

### 7.5 Wait for screen changes
- 基準画像（ベースライン）を取得し、監視領域の差分を検出
- timeout：`waitingMs` 経過で打ち切り（Failure）
- 判定：
  - 変化検出 → Success
  - タイムアウト → Failure
- 成功時オプション（Action設定に従う）：
  - Mouse action ON：指定の Mouse action behavior を実行
  - Save Coordinate ON：検出座標を Variable に保存（X/Y それぞれ）
- 分岐：pixel color と同様（trueGoTo / falseGoTo）

### 7.6 Wait for text input
- ユーザーのキーボード入力ストリームを監視し、指定文字列が入力されたら Success
- 仕様：部分一致（機能仕様に準拠）
- timeout：`waitingMs` 経過で Failure
- 分岐：trueGoTo / falseGoTo

### 7.7 Find image
- 指定 searchArea 内でテンプレート画像（bitmapSource）を探索
- timeout：`waitingMs` 経過で Failure
- 判定：
  - 検出 → Success（検出矩形を得る）
  - 未検出/タイムアウト → Failure
- 成功時オプション：
  - Mouse action ON：behavior と position に従ってマウス動作
  - Save Coordinate ON：検出座標（positionに応じた点、または矩形中心等）を Variable に保存（X/Y）
- 分岐：trueGoTo / falseGoTo

### 7.8 Find text (OCR)
- 指定 searchArea を OCR し、指定文字列が見つかれば Success（完全一致）
- timeout：`waitingMs` 経過で Failure
- 成功時オプション：
  - Mouse action ON：検出位置に応じたマウス動作（UI仕様の position に準拠）
  - Save Coordinate ON：検出座標を Variable に保存（X/Y）
- 分岐：trueGoTo / falseGoTo

### 7.9 Repeat
- startLabel から RepeatAction 自身の行までを “繰り返し区間” とする
- 条件（排他）：
  - Seconds：経過秒数が条件を満たすまで繰り返す
  - Repetitions：指定回数まで繰り返す
  - Until：指定時刻まで繰り返す
  - Infinite：無限
- 反復制御：
  - 繰り返し継続 → PC を startLabel 行へ移動
  - 終了 → finishGoTo に従って PC を更新
- 途中 Cancel で中断

### 7.10 Go to
- target に従って PC を更新（Success）
- 不正な Label 参照は再生前検証で防ぐ（到達した場合は Error）

### 7.11 If
- variableName の値を取得
- 条件種別に従って評価し Success/Failure を決定
  - Success（条件成立）→ trueGoTo
  - Failure（条件不成立）→ falseGoTo
- 例外（型変換不可など）は Error

### 7.12 Embed macro file
- 指定パスのマクロをロードし、**そのマクロを“サブルーチン”として実行**する
- 実行完了後：呼び出し元の次行へ復帰（PC+1）
- 変数のスコープ：本版は **共有（同一コンテキスト）** を推奨
  - ※ Reset variables 設定が有効の場合の扱いは 8.2 に従う

### 7.13 Execute program
- 指定パスのプログラムを起動して Success
- 起動失敗は Error
- 本版では「終了待ち」はしない（起動のみ）

---

## 8. Playback 設定の反映

### 8.1 Playback Speed（速度）
- 速度（%）は Wait 系、Move の duration 等の時間パラメータにスケールを掛ける
- 例：Speed=200% → 待機時間は 1/2（実装側で丸め）
- 0% は無効（入力制限で防ぐ）。最小は 1% とする。

### 8.2 Reset variables and list counter on each playback cycle
- 「Cycle」の定義：**Play を1回開始して1回完了/中断するまで**
- 有効の場合：再生開始時に Variable を初期化（空にする）
- 無効の場合：前回値を保持（アプリの仕様に合わせる。推奨は保持）

### 8.3 入力による中断（Playback設定）
- `Abort playback on key press` が有効：任意のキー入力を検知したら Cancel
- `Abort playback on mouse move` が有効：一定以上のマウス移動を検知したら Cancel
- `Block key presses during playback` は、再生中のユーザー入力を抑止（実装依存）
  - 抑止できない場合でも「Abort設定」が有効なら中断できること

---

## 9. 失敗・例外・ログ

### 9.1 結果区分
- Success：通常完了
- Canceled：Stop/Abort による中断
- Failed：Error による失敗（例外、必須リソース欠落など）

### 9.2 例外時
- 例外を握りつぶさず、Failed で終了
- UI には “どの Step（開発ID/行）で失敗したか” を表示できる情報を保持する（ログ/メッセージ）

### 9.3 ログ（推奨）
- 再生開始/終了（結果）
- 実行した Step index と Action種別
- 分岐先（GoTo/Repeat/If の遷移）

---

## 10. 受け入れ基準（最小）

- Play / Stop / 各種 Play* が仕様通りに開始位置・終了条件を満たす
- 分岐（trueGoTo/falseGoTo）が Start/Next/End/Label に従って動作する
- 実行中ステップが UI で自動選択される
- waitingMs により timeout で Failure 分岐に落ちる
- Embed macro file 実行後に呼び出し元へ復帰する
- 例外発生時に Failed で終了し、失敗行が分かる

---
以上
