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
| SearchArea | 監視 / 検索対象（EntireDesktop / AreaOfDesktop / FocusedWindow / AreaOfFocusedWindow） |
| Rect | X1, Y1, X2, Y2 で定義される矩形 |
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
- 型体系やスコープは別チケットで確定する

---

## 5. アクション体系

### 5.1 Mouse
- `MouseClickAction`
- `MouseMoveAction`
- `MouseWheelAction`

### 5.2 Key
- `KeyPressAction`
- `HotkeyAction`
  - 編集操作として扱い、永続化時は KeyPress 群へ展開する

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
- FindTextOcr は検索文字列が空不可
- `Test` ボタン状態は永続化しない

### 6.8 Repeat
- 条件 `Seconds / Repetitions / Until / Infinite` は排他
- `startLabel` は存在するLabelであること
- `finishGoTo` は有効なGoToTargetであること

### 6.9 If
- `variableName` は空不可
- `conditionType` により `value` の必須 / 任意が変わる
- 本版の条件種別は `Macro仕様書_v7.xlsx` に存在する列挙値に限定する

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
