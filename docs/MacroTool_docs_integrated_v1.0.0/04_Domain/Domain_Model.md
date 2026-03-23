# ドメイン仕様（Domain Model）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 対象: **MacroTool**（WinForms / C# / DDD + TDD）

本書は、`Macro仕様書_v7.xlsx` および `docs/02_Requirements/Functional_Spec.md` に基づき、ドメイン用語・集約・不変条件を定義する。

---

## 1. スコープ

### 1.1 本版で扱うもの
- Macro / Step / Action
- GoTo / Repeat / If の参照整合性
- SearchArea、Color、Duration などの値オブジェクト
- 本版対象Actionの永続化に必要な不変条件

### 1.2 本版で扱わないもの
`Macro仕様書_v7.xlsx` に存在しないもの、または将来実装予定とするものは本書に含めない。  
例:
- Wait for screen change
- Wait for hotkey
- Wait for file change
- Capture text / Capture image
- Window focus / Show notification / Show message box
- 変数操作群 / Web抽出

---

## 2. ユビキタス言語

| 用語 | 意味 |
|---|---|
| Macro | Action列を持つ実行単位（集約ルート） |
| Step | Macro内の1行 |
| Action | Stepが実行する操作 |
| Label | GoTo / Repeat の参照先となる一意な行識別子 |
| GoToTarget | Start / Next / End / Label で表される遷移先 |
| VariableName | 実行時に利用する変数名 |
| SearchArea | 監視 / 検索対象（EntireDesktop / AreaOfDesktop / FocusedWindow / AreaOfFocusedWindow）。AreaOfDesktop は仮想デスクトップ基準、AreaOfFocusedWindow はフォーカス中ウィンドウ外枠左上基準の物理ピクセルを用いる |
| Rect | X1, Y1, X2, Y2 で定義される矩形。物理ピクセルで保持し、保存時は min/max へ正規化する |
| Playback | Macroを順に評価し、必要に応じて待機や分岐を行う処理 |

---

## 3. 集約設計

### 3.1 Macro（集約ルート）
責務:
- Step順序の整合性を保つ
- Label一意性を保つ
- Label参照を持つActionの整合性を検証可能にする

代表プロパティ:
- `MacroId`
- `IReadOnlyList<MacroStep> Steps`
- `MacroVersion SpecVersion`

### 3.2 MacroStep
責務:
- 1行の順序を表す
- 任意でLabelを持つ
- 1つのActionを持つ

代表プロパティ:
- `StepId`
- `int Order`
- `StepLabel? Label`
- `MacroAction Action`

---

## 4. 値オブジェクト

### 4.1 StepLabel
- `string Value`
- 前後空白除去後の値を保持する

### 4.2 GoToTarget
- Kind: `Start | Next | End | Label`
- `Label` の場合のみ `StepLabel` を保持する

### 4.3 SearchArea / Rect
- SearchAreaKind が Area系なら `Rect` 必須
- Rectは `X2 > X1` かつ `Y2 > Y1`

### 4.4 ColorCode / Percentage / Milliseconds
- `ColorCode`: `#RRGGBB`
- `Percentage`: 0..100
- `Milliseconds`: 0以上

### 4.5 VariableName
- UIで入力される変数名を値オブジェクトとして扱う
- 形式は `^[A-Za-z_][A-Za-z0-9_]*$` とする
- 英数字と `_` のみを許可し、先頭数字は禁止する
- 変数参照は大文字小文字を区別しない
- `Count` と `count` は同一の `VariableName` として扱う

### 4.6 VariableValue
- 実行時値は `Undefined / String / Number` のいずれかとする
- 未設定状態は `Undefined` とする
- 空文字列や `0` を未設定値として扱わない
- `Save Coordinate` により保存される X / Y は `Number` とする

---

## 5. アクション体系

### 5.1 Mouse
- `MouseClickAction`
- `MouseMoveAction`
- `MouseWheelAction`

### 5.2 Key
- `KeyPressAction`
  - 永続化および再生の正規表現とする
- `HotkeyAction`
  - 編集時のみ利用する複合入力表現とする
  - 永続化前に、順序付きの `KeyPressAction` 群へ正規化する
  - 復元時は `HotkeyAction` へ自動再構成せず、`KeyPressAction` 群として読み込む

### 5.3 Wait
- `WaitAction`
- `WaitForPixelColorAction`
- `WaitForTextInputAction`

### 5.4 Detection
- `FindImageAction`
- `FindTextOcrAction`

### 5.5 ControlFlow
- `RepeatAction`
- `GoToAction`
- `IfAction`
- `EmbedMacroFileAction`
- `ExecuteProgramAction`

---

## 6. ドメイン不変条件

### 6.1 Macro / Step
1. Step順序は 0..n-1 の連番
2. UI表示順とOrderは一致する
3. StepLabel は Macro 内で一意

### 6.2 Label一意性
- 重複時は末尾に連番を付与して一意化する
- 末尾が数字の場合はその数字をインクリメントする

### 6.3 GoToTarget
- `Start / Next / End` は常に有効
- `Label` は Macro内に存在するLabelのみ指定可能

### 6.4 SearchArea / Rect
- Area系の場合のみ Rect 必須
- 負座標は許容する
- 幅 / 高さが0以下のRectは無効

### 6.5 WaitForPixelColor
- `Tolerance` は 0..100
- `ColorCode` は `#RRGGBB`
- `waitingMs` は 0以上

### 6.6 WaitForTextInput
- `textToWaitFor` は空不可
- `waitingMs` は 0以上
- `trueGoTo` / `falseGoTo` は必須

### 6.7 FindImage / FindTextOcr
- FindImage は検索対象画像が必須
- FindImage の画像ソースは `CapturedBitmap` または `FilePath` に限定する
- `Variable` / `Embedded` / その他の画像ソース種別は本版対象外とする
- FindImage の `Tolerance` は **許容差** とし、`0` が最も厳密、`100` が最も緩い
- FindImage で複数候補が見つかった場合は、一致度が最も高い候補を採用し、同点時は左上の候補を採用する
- FindTextOcr は検索文字列が空不可
- FindTextOcr の文字列比較は、前後空白を除去したうえで完全一致とする
- FindTextOcr で複数候補が見つかった場合は、最も左上の候補を採用する
- 検出成功時の代表点は `MousePosition` で算出し、`MouseActionBehavior` と `SaveXVariable` / `SaveYVariable` の双方で共通利用する
- `Test` ボタン状態は永続化しない

### 6.8 Repeat
- 条件 `Seconds / Repetitions / Until / Infinite` は排他
- 繰り返し範囲は **`startLabel` の行から Repeat 行の直前まで** とする
- `startLabel` は存在するLabelであること。未解決の `startLabel` を含むマクロは保存時 / 読込時エラーとする
- `finishGoTo` は有効なGoToTargetであること
- Repeat のネストは禁止とする
- `Infinite` は Stop 操作またはエラー発生まで継続する
- `finishGoTo` は繰り返し完了後に1回だけ適用する
- Playback Repeat は現在の再生対象全体を1 cycleとして繰り返す
- Playback Repeat が有効な場合でも `RepeatAction` は内部制御としてそのまま実行する
- `RepeatAction(Infinite)` が存在する場合、外側の Playback Repeat の次 cycle へは到達しない

### 6.9 If
- `variableName` は空不可
- `variableName` は `VariableName` の命名規則に従う
- 変数参照は大文字小文字を区別しない
- `conditionType` により `value` の必須 / 任意が変わる
- 文字列比較は `String`、数値比較は `Number` に対して評価する
- `Undefined` に対して数値比較・文字列比較を行う場合の評価結果はアプリケーション層の条件評価仕様に従う
- 本版の条件種別は `Macro仕様書_v7.xlsx` に存在する列挙値に限定する

### 6.10 実行時変数コンテキスト
- 変数ストアは Playback 開始時に生成し、Playback 終了時に破棄する
- Playback 開始時、全変数は `Undefined` へ初期化する
- 設定 `Reset variables and list counter on each playback cycle` が有効な場合、Playback Repeat の cycle 開始時に全変数を `Undefined` へ初期化する
- 変数値は JSON / CSV へ永続化しない

### 6.11 Hotkey 正規化
- `HotkeyAction` は保存前に `KeyPressAction` 群へ必ず変換する
- 正規化順は、修飾キー `Down` 群 → 主キー `Press` → 修飾キー `Up` 群（逆順）とする
- 例: `Ctrl+Shift+S` は `Ctrl Down` → `Shift Down` → `S Press` → `Shift Up` → `Ctrl Up`
- 永続化層の Action 種別に `Hotkey` は存在しない
- 復元後の内部表現は `KeyPressAction` 群とし、`HotkeyAction` への自動再構成は行わない

---

## 7. ドメインサービス（推奨）
- `LabelUniquenessService`
- `MacroValidator`
- `GoToResolver`

---

## 8. アプリケーション層との責務分担
- UI状態制御はアプリケーション層が担う
- ドメインは編集可能な正しいデータ構造を提供する
- 再生状態の詳細遷移は `docs/06_Playback` 側で定義する

---
以上
