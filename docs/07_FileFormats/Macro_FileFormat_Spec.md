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

> Hotkey は UI操作で KeyPress 群に展開する。ファイルには Hotkey として保存しない。

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

### 5.3 WaitForTextInput.data
- `textToWaitFor`: string（空不可）
- `waitingMs`: int（0以上）
- `trueGoTo`: GoToTarget
- `falseGoTo`: GoToTarget

### 5.4 FindImage.data
- `searchArea`: SearchArea
- `tolerance`: 0..100
- `bitmapSource`: object
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

### 5.7 If.data
- `variableName`
- `conditionType`
- `value`
- `trueGoTo`
- `falseGoTo`

### 5.8 EmbedMacroFile.data / ExecuteProgram.data
- `path`: string（空不可）

---

## 6. CSV（Export / Import）仕様との関係
CSV列の定義は以下を参照する。
- `docs/07_FileFormats/CSV_Schema_v1.0.md`
- `docs/07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Column_To_DTO_Mapping.md`

---

## 7. 本書で未確定とする事項
- `bitmapSource` の厳密な許容種別
- `MouseButton` / `ClickType` の厳密な enum
- Importの追加位置詳細

---
以上
