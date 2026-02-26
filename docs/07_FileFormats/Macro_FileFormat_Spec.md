# 永続化仕様（マクロファイル形式 / Export・Import）

- Version: **Macro_v1.0.0**
- 更新日: 2026-02-26
- 対象: MacroTool（WinForms / C#）
- 参照: `docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md` / `docs/04_Domain/Domain_Model.md` / `docs/05_Playback/Playback_Spec.md`

本書は、MacroTool の **保存（JSON）** と **Export/Import（CSV）** のファイル仕様を定義する。  
※「将来実装予定」と明記された機能は本書の対象外。

---

## 1. 方針

- **通常のマクロ保存形式は JSON**（ツールが保存・読込する正規フォーマット）
- **Export/Import は CSV**（ユーザーがオフラインで容易に編集・作成できることを最優先）
- 互換性は **vX.Y.Z** で管理する
  - 互換性が壊れる変更 → **X**
  - 互換性を壊さない機能追加 → **Y**
  - バグ修正 → **Z**

---

## 2. ファイル種別

| 種別 | 拡張子（推奨） | 用途 | 正（SSOT） |
|---|---|---|---|
| マクロ保存 | `.json`（例：`*.macro.json`） | ツールの保存/読込 | ✅ 正 |
| Export/Import | `.csv` | ユーザー編集用 | ⛳ 交換用 |

> 注：拡張子は運用上の推奨。実装では拡張子で判定してもよい。

---

## 3. JSON（マクロ保存）仕様

### 3.1 文字・改行
- 文字コード：UTF-8（BOMなし推奨）
- 改行：LF/CRLF どちらも許容
- JSON は **インデント有り**を推奨（Git差分が見やすい）

### 3.2 ルート構造（概要）
```json
{
  "format": "MacroTool.Macro",
  "formatVersion": "1.0.0",
  "specVersion": "Macro_v1.0.0",
  "createdAt": "2026-02-12T00:00:00+09:00",
  "updatedAt": "2026-02-26T00:00:00+09:00",
  "macro": {
    "name": "Example",
    "steps": [
      {
        "order": 0,
        "label": "Jump先",
        "action": {
          "type": "MouseClick",
          "data": { }
        }
      }
    ]
  }
}
```

### 3.3 フィールド定義（ルート）
- `format`：固定文字列 `MacroTool.Macro`
- `formatVersion`：ファイル形式バージョン（**JSON構造の互換性**）
  - Macro_vX.Y.Z の X が上がる可能性がある変更は、原則ここも上げる
- `specVersion`：仕様（機能）バージョン（Macro_vX.Y.Z）
- `createdAt` / `updatedAt`：ISO 8601 文字列（任意）
- `macro`：マクロ本体

### 3.4 Macro 構造
- `name`：表示名（任意）
- `steps`：Step配列（必須）

### 3.5 Step 構造
- `order`：0..n-1 の連番（必須）
- `label`：文字列 or null（任意）
- `action`：Action定義（必須）

> 不変条件：label は Macro 内で一意（詳細は Domain_Model.md）

### 3.6 Action 構造（判別子 + データ）
- `type`：Action種別（必須、文字列）
- `data`：種別固有データ（必須、オブジェクト）

**例：Wait**
```json
{
  "type": "Wait",
  "data": {
    "valueMs": 500
  }
}
```

---

## 4. Action type 一覧（Macro_v1.0.0）

本版で扱う Action type は以下。将来実装予定の type は **保存対象にしない**。

### 4.1 Mouse
- `MouseClick`
- `MouseMove`
- `MouseWheel`

### 4.2 Key
- `KeyPress`
> Hotkey は UI 操作で KeyPress 群に展開される前提。ファイルには保存しない（保存する場合は別版で規定）。

### 4.3 Wait
- `Wait`
- `WaitForPixelColor`
- `WaitForScreenChange`
- `WaitForTextInput`

### 4.4 Detection
- `FindImage`
- `FindTextOcr`

### 4.5 ControlFlow
- `Repeat`
- `GoTo`
- `If`
- `EmbedMacroFile`
- `ExecuteProgram`

---

## 5. JSONデータ仕様（主要 Action の data）

> 以下は「最低限の保存項目」。UIの Test ボタン等の一時値は保存しない。

### 5.1 共通：GoToTarget
- 形式：
```json
{ "kind": "Start|Next|End|Label", "label": "Jump先" }
```
- `kind="Label"` のときのみ `label` 必須

### 5.2 SearchArea
- 形式：
```json
{ "kind": "EntireDesktop|AreaOfDesktop|FocusedWindow|AreaOfFocusedWindow", "rect": { "x1":0,"y1":0,"x2":100,"y2":100 } }
```
- Area系の場合のみ `rect` 必須
- rect の幅・高さは正（x2>x1, y2>y1）

### 5.3 MouseClick.data
- `button`：Left/Right/Middle/X1/X2
- `clickType`：Click/DoubleClick/Down/Up
- `relative`：bool
- `x`,`y`：int

### 5.4 MouseMove.data
- `relative`：bool
- `startX`,`startY`,`endX`,`endY`：int
- `durationMs`：int（0以上）

### 5.5 MouseWheel.data
- `orientation`：Horizontal/Vertical
- `value`：int

### 5.6 KeyPress.data
- `option`：Press/Down/Up
- `key`：文字列（例："A", "Enter", "F1" 等。実装はキー辞書で解決）
- `count`：int（1以上）

### 5.7 Wait.data
- `valueMs`：int（0以上）

### 5.8 WaitForPixelColor.data
- `x`,`y`：int
- `color`："#RRGGBB"
- `tolerance`：0..100
- `waitingMs`：0以上
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.9 WaitForScreenChange.data
- `searchArea`：SearchArea
- `mouseActionEnabled`：bool
- `mouseActionBehavior`：Positioning/LeftClick/RightClick/MiddleClick/DoubleClick（Enabled のとき必須）
- `saveCoordinateEnabled`：bool
- `saveXVariable`,`saveYVariable`：変数名（Enabled のとき必須）
- `waitingMs`：0以上
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.10 WaitForTextInput.data
- `textToWaitFor`：string（空不可）
- `waitingMs`：0以上
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.11 FindImage.data
- `searchArea`：SearchArea
- `tolerance`：0..100
- `bitmapSource`：下記のいずれか
  - `{ "kind":"File", "path":"relative/or/absolute" }`
  - `{ "kind":"Variable", "name":"VarName" }`
  - `{ "kind":"Embedded", "base64":"..." }`（本版は非推奨：ファイル肥大化のため）
- `mouseActionEnabled` / `mouseActionBehavior` / `mousePosition`
- `saveCoordinateEnabled` / `saveXVariable` / `saveYVariable`
- `waitingMs`：0以上
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.12 FindTextOcr.data
- `textToSearchFor`：string（空不可）
- `language`：English/Japanese
- `searchArea`：SearchArea
- `mouseActionEnabled` / `mouseActionBehavior` / `mousePosition`
- `saveCoordinateEnabled` / `saveXVariable` / `saveYVariable`
- `waitingMs`：0以上
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.13 Repeat.data
- `startLabel`：string（存在必須）
- `mode`：Seconds/Repetitions/Until/Infinite（排他）
- `seconds`：mode=Seconds のとき必須（0以上）
- `repetitions`：mode=Repetitions のとき必須（1以上）
- `until`：mode=Until のとき必須（"HH:mm:ss"）
- `finishGoTo`：GoToTarget

### 5.14 If.data
- `variableName`：string（空不可）
- `conditionType`：機能仕様の一覧値（文字列）
- `value`：conditionTypeにより必須/任意
- `trueGoTo`,`falseGoTo`：GoToTarget

### 5.15 EmbedMacroFile.data / ExecuteProgram.data
- `path`：string（空不可）
- path は相対/絶対どちらも許容（相対はマクロファイルの場所基準を推奨）

---

## 6. 互換性ポリシー（JSON）

### 6.1 読み込み側の基本方針
- **未知フィールドは無視**して読み込む（Forward Compatibility）
- 既知フィールド欠落は：
  - 必須フィールド欠落 → 読込失敗（Failed）
  - 任意フィールド欠落 → 既定値で補完

### 6.2 未知 Action type の扱い
- 本版では、未知 `action.type` を含むファイルは **読込失敗**とする  
  （ユーザーが気づかずに再生できない状態になるのを防ぐため）
- ただし将来版で「UnknownAction として保持」等の仕様を導入する場合は、formatVersion を上げる。

### 6.3 enum 文字列
- enum は **文字列で保存**し、大小文字は厳密一致を推奨
- 読込側は大小文字の揺れを許容してもよい（互換性向上）

---

## 7. CSV（Export / Import）仕様

### 7.1 基本方針
- **1ファイルで完結**（ユーザーがExcel等で編集しやすい）
- Action種別ごとに必要な列を定義し、不要列は空欄
- 文字コード：UTF-8（BOMあり/なしは両対応推奨）
- 区切り：カンマ `,`
- 文字列のクォート：RFC4180 準拠（カンマ・改行・ダブルクォートを含む場合は `"..."`、内部の `"` は `""`）

### 7.2 CSV ヘッダ（固定）
> 列は増やしてもよいが、Import の互換性を壊す場合は **formatVersion** を上げる。

**共通列**
- `Order`（int）
- `Label`（string, 任意）
- `ActionType`（string, 必須：上記 Action type）
- `Comment`（string, 任意：UIの備考として扱う。再生には影響しない）

**GoToTarget 共通（true/false/finish で使う）**
- `TrueGoToKind` / `TrueGoToLabel`
- `FalseGoToKind` / `FalseGoToLabel`
- `GoToKind` / `GoToLabel`（GoToAction用）
- `FinishGoToKind` / `FinishGoToLabel`（Repeat用）

**SearchArea 共通**
- `SearchAreaKind`
- `X1`,`Y1`,`X2`,`Y2`（Area系のみ使用）

**Mouse action / Save Coordinate 共通（WaitForScreenChange/FindImage/FindTextOcr）**
- `MouseActionEnabled`（true/false）
- `MouseActionBehavior`
- `MousePosition`
- `SaveCoordinateEnabled`（true/false）
- `SaveXVariable`
- `SaveYVariable`

**時間・待機**
- `ValueMs`（Wait用）
- `WaitingMs`（待機系/検出系）

**Action固有（例）**
- MouseClick：`MouseButton`,`ClickType`,`Relative`,`X`,`Y`
- MouseMove：`Relative`,`StartX`,`StartY`,`EndX`,`EndY`,`DurationMs`
- MouseWheel：`WheelOrientation`,`WheelValue`
- KeyPress：`KeyOption`,`Key`,`Count`
- PixelColor：`X`,`Y`,`Color`,`Tolerance`
- FindTextOcr：`Text`,`Language`
- FindImage：`BitmapKind`,`BitmapPath`,`BitmapVariable`
- Repeat：`StartLabel`,`RepeatMode`,`Seconds`,`Repetitions`,`Until`
- If：`VariableName`,`ConditionType`,`ConditionValue`
- Embed/Execute：`Path`

### 7.3 CSV 例（最小）
```csv
Order,Label,ActionType,Comment,ValueMs
0,,Wait,,500
1,Jump先,GoTo,,,
```

### 7.4 Import 時のルール
- `Order` は 0..n-1 の連番でなくてもよい  
  → Import 時に **昇順で並び替え**、内部 order は 0..n-1 に採番し直す
- `Label` は Domain の一意性ルールに従い **自動一意化**してよい  
  （ただし参照（GoTo/Repeat）と整合が崩れないよう、同一Import内で同じ変換を適用する）
- `ActionType` が未知の場合は Import 失敗
- 必須列が欠落、または値が不正（例：色コード不正、Rect不正）の場合は Import 失敗

### 7.5 Export 時のルール
- 現在の Macro の Step 順に `Order` を出力
- すべての Action を 1行に展開して出力
- UIの「状態」列は Export/Import では扱わない（空欄/無視）

---

## 8. 受け入れ基準（最小）

- JSON 保存 → 読込 → 再保存で、内容が同等（Actionが欠落しない）
- CSV Export → ユーザーがExcelで編集 → CSV Import が可能
- CSV Import の不正値は適切に失敗し、失敗理由（行/列）が分かる
- specVersion=Macro_v1.0.0 のファイルが Macro_v1.0.0 で再生できる

---
以上
