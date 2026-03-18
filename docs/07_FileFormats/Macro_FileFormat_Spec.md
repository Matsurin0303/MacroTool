# 永続化仕様（マクロファイル形式 / Export・Import）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 対象: MacroTool（WinForms / C#）
- 参照: `docs/02_Requirements/Functional_Spec.md` / `docs/04_Domain/Domain_Model.md` / `docs/06_Playback/Playback_Spec.md`

本書は、MacroTool の **保存（JSON）** と **Export / Import（CSV）** のファイル仕様を定義する。  
本版対象は `Macro仕様書_v7.xlsx` に存在する機能のみとする。

---

## 1. 方針

- 通常のマクロ保存形式は JSON
- Export / Import は CSV
- 互換性は `Macro_vX.Y.Z` で管理する

---

## 2. ファイル種別

| 種別 | 拡張子（推奨） | 用途 | SSOT |
|---|---|---|---|
| マクロ保存 | `.json` | ツールの保存 / 読込 | 正 |
| Export / Import | `.csv` | ユーザー編集用 | 交換用 |

---

## 3. JSON（マクロ保存）仕様

### 3.1 ルート構造
```json
{
  "format": "MacroTool.Macro",
  "formatVersion": "1.0.0",
  "specVersion": "Macro_v1.0.0",
  "macro": {
    "name": "Example",
    "steps": [
      {
        "order": 0,
        "label": "Jump先",
        "action": {
          "type": "MouseClick",
          "data": {}
        }
      }
    ]
  }
}
```

### 3.2 主要フィールド
- `format`: 固定文字列 `MacroTool.Macro`
- `formatVersion`: JSON構造の互換性バージョン
- `specVersion`: 機能仕様の版
- `macro.steps`: Step配列

---

## 4. Action type 一覧（本版）

### 4.1 Mouse
- `MouseClick`
- `MouseMove`
- `MouseWheel`

### 4.2 Key
- `KeyPress`

> Hotkey は編集時のみ存在するUI表現とし、保存時は KeyPress 群へ正規化する。ファイルには Hotkey として保存しない。

### 4.3 Wait
- `Wait`
- `WaitForPixelColor`
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

## 5. JSONデータ仕様（要点）

### 5.1 GoToTarget
```json
{ "kind": "Start|Next|End|Label", "label": "Jump先" }
```

### 5.2 SearchArea
```json
{ "kind": "EntireDesktop|AreaOfDesktop|FocusedWindow|AreaOfFocusedWindow", "rect": { "x1": 0, "y1": 0, "x2": 100, "y2": 100 } }
```
- `rect` は物理ピクセルで保持する
- `kind=AreaOfDesktop` の `rect` は仮想デスクトップ基準
- `kind=AreaOfFocusedWindow` の `rect` はフォーカス中ウィンドウ外枠左上基準
- 保存時は `x1<=x2`, `y1<=y2` になるよう正規化する

### 5.3 WaitForTextInput.data
- `textToWaitFor`: string（空不可）
- `waitingMs`: int（0以上）
- `trueGoTo`: GoToTarget
- `falseGoTo`: GoToTarget

### 5.4 FindImage.data
- `searchArea`: SearchArea
- `tolerance`: 0..100
- `bitmapSource`: object
  - `kind`: `CapturedBitmap | FilePath`
  - `value`: string
  - `kind=CapturedBitmap` の場合、`value` は Macro 内に保持される画像データに対応する値
  - `kind=FilePath` の場合、`value` は画像ファイルパス
  - `Variable` / `Embedded` / その他の画像ソース種別は本版対象外
- `mouseActionEnabled`: bool
- `mouseActionBehavior`: string?
- `mousePosition`: string?
- `saveCoordinateEnabled`: bool
- `saveXVariable`: string?
- `saveYVariable`: string?
- `waitingMs`: int
- `trueGoTo`: GoToTarget
- `falseGoTo`: GoToTarget

### 5.5 FindTextOcr.data
- `textToSearchFor`: string（空不可）
- `language`: English / Japanese
- `searchArea`: SearchArea
- `mouseActionEnabled`: bool
- `mouseActionBehavior`: string?
- `mousePosition`: string?
- `saveCoordinateEnabled`: bool
- `saveXVariable`: string?
- `saveYVariable`: string?
- `waitingMs`: int
- `trueGoTo`: GoToTarget
- `falseGoTo`: GoToTarget

### 5.6 Repeat.data
- `startLabel`
- `mode`: `Seconds / Repetitions / Until / Infinite`
- `seconds`
- `repetitions`
- `until`
- `finishGoTo`
- 繰り返し範囲は `startLabel` の行から Repeat 行の直前までとする
- `startLabel` は同一マクロ内の既存 `Label` を参照しなければならない
- Repeat のネストは禁止とする
- `Infinite` は Stop 操作またはエラー発生まで継続する
- `finishGoTo` は繰り返し完了後に1回だけ適用する

### 5.7 If.data
- `variableName`
- `conditionType`
- `value`
- `trueGoTo`
- `falseGoTo`

### 5.8 EmbedMacroFile.data / ExecuteProgram.data
- `path`: string（空不可）

### 5.9 列挙値標準
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`
- `WheelOrientation`: `Horizontal / Vertical`
- `KeyOption`: `Press / Down / Up`

### 5.10 Hotkey の保存 / 復元ルール
- JSON の `action.type` に `Hotkey` は出力しない
- Hotkey は保存時に複数の `KeyPress` Action へ正規化して `macro.steps` に出力する
- 正規化順は、修飾キー `Down` 群 → 主キー `Press` → 修飾キー `Up` 群（逆順）とする
- 例: `Ctrl+Shift+S` は 5 Step の `KeyPress` として保存する
- JSON 読込時は `KeyPress` 群をそのまま復元し、`Hotkey` Action への自動再構成は行わない

### 5.11 実行時変数ルール
- JSON / CSV は **変数定義や変数値そのものを保存しない**
- 変数は Playback 実行時に動的に生成される実行時コンテキストで管理する
- 変数名は `^[A-Za-z_][A-Za-z0-9_]*$` に従う
- 変数参照は大文字小文字を区別しない
- 実行時の値型は `String` または `Number` とする
- 未設定状態は `Undefined` とする
- `saveXVariable` / `saveYVariable` に保存される値は `Number` とする
- Playback 開始時に全変数を `Undefined` へ初期化し、Playback 終了時に破棄する
- 設定 `Reset variables and list counter on each playback cycle` が有効な場合、Repeat による cycle 開始時にも全変数を `Undefined` へ初期化する

---

## 6. CSV（Export / Import）仕様との関係
CSV列の定義は以下を参照する。
- `docs/07_FileFormats/CSV_Schema_v1.0.md`
- `docs/07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Column_To_DTO_Mapping.md`

---

## 7. 本書で未確定とする事項
- `bitmapSource.kind=CapturedBitmap` の `value` の具体エンコード方式

---
以上
