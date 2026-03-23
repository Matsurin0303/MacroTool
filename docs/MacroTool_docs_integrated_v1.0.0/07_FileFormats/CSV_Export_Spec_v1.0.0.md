---
date: 2026-03-18
title: Macro CSV Export Specification
version: Macro_v1.0.0
---

# Macro CSV Export Specification（Macro_v1.0.0）

## 1. 目的

本ドキュメントは、MacroTool における CSV Export 仕様を定義する。  
対象は `Macro仕様書_v7.xlsx` に存在する本版機能のみとする。

---

## 2. 基本方針

- ExportフォーマットはImport仕様と一致させる
- ヘッダは固定
- 未使用列は空欄で出力する
- 内部ドメイン表現に依存しない

---

## 3. ヘッダ（固定）

```csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue,Path
```

---

## 4. 共通出力ルール

- `Order` は表示順で出力する
- `Label` 未設定時は空欄
- `Comment` は任意
- GoTo列は `Start / Next / End / Label:<ラベル名>` 形式で出力する

---

## 5. 変数名 / 変数値ルール
- `VariableName` / `SaveXVariable` / `SaveYVariable` は `^[A-Za-z_][A-Za-z0-9_]*$` を満たす名称で出力する
- 変数参照は大文字小文字を区別しない
- CSV へは変数名のみを出力し、実行時の変数値は出力しない
- `Save Coordinate` により保存される X / Y は実行時に数値として扱う

## 6. Action別出力仕様

### 6.1 Mouse
- `MouseClick` → `MouseButton, ClickType, Relative, X, Y`
- `MouseMove` → `Relative, StartX, StartY, EndX, EndY, DurationMs`
- `MouseWheel` → `WheelOrientation, WheelValue`

### 6.2 Key
- `KeyPress` → `KeyOption, Key, Count`
- 編集上の `Hotkey` は、Export 時に複数行の `KeyPress` へ正規化して出力する
  - 正規化順は、修飾キー `Down` 群 → 主キー `Press` → 修飾キー `Up` 群（逆順）
  - 例: `Ctrl+Shift+S` → `Ctrl/Down`, `Shift/Down`, `S/Press`, `Shift/Up`, `Ctrl/Up`
- `Action=Hotkey` 行は Export しない

### 6.3 Wait
- `Wait` → `WaitingMs`
- `WaitForPixelColor` → `X, Y, Color, Tolerance, WaitingMs, TrueGoTo, FalseGoTo`
- `WaitForTextInput` → `Text, WaitingMs, TrueGoTo, FalseGoTo`

### 6.4 Detection
- `FindImage` → `SearchAreaKind, X1, Y1, X2, Y2, Tolerance, BitmapKind, BitmapValue, WaitingMs, TrueGoTo, FalseGoTo, MouseActionBehavior, MousePosition, SaveXVariable, SaveYVariable`
  - `BitmapKind` は `CapturedBitmap / FilePath` のみ
  - `Variable` / `Embedded` / その他の画像ソース種別は Export 対象外
  - `BitmapKind=FilePath` の場合、`BitmapValue` は画像ファイルパス
  - `BitmapKind=CapturedBitmap` の CSV 上の具体表現は未確定
- `FindTextOcr` → `Text, Language, SearchAreaKind, X1, Y1, X2, Y2, WaitingMs, TrueGoTo, FalseGoTo, MouseActionBehavior, MousePosition, SaveXVariable, SaveYVariable`

### 6.5 Control Flow
- `GoTo` → `GoTo`
- `If` → `VariableName, ConditionType, ConditionValue, TrueGoTo, FalseGoTo`
- `Repeat` → `StartLabel, RepeatMode, Seconds, Repetitions, Until, FinishGoTo`
- `EmbedMacroFile` → `Path`
- `ExecuteProgram` → `Path`

---

## 7. 列挙値標準

### 6.1 MouseClick
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`

### 6.2 Detection
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`

### 6.3 その他
- `WheelOrientation`: `Horizontal / Vertical`
- `KeyOption`: `Press / Down / Up`

## 8. エンコード仕様
- UTF-8（BOMなし推奨）
- 改行コードは CRLF を推奨

---

## 9. 本書で未確定とする事項
- Hotkey 編集UIへ再構成するための補助メタデータは本版では出力しない

---
以上
