---
date: 2026-03-18
title: Macro CSV Import Specification
version: Macro_v1.0.0
---

# Macro CSV Import Specification（Macro_v1.0.0）

## 1. 目的

本ドキュメントは、MacroTool における CSV Import 仕様を定義する。  
対象は `Macro仕様書_v7.xlsx` に存在する本版機能のみとする。

---

## 2. 共通仕様

### 2.1 共通必須列
- `Order`
- `Action`

### 2.2 共通任意列
- `Label`
- `Comment`

### 2.3 共通ルール
- `Order` は数値必須。Import後に内部再採番してよい。
- `Action` は固定トークン（英語）。未知ActionはImportエラー。
- `Label` はMacro内で一意。重複時は末尾に連番付与。

---

## 3. GoToターゲット形式

対象列:
- `GoTo`
- `TrueGoTo`
- `FalseGoTo`
- `FinishGoTo`

許容値:
- `Start`
- `Next`
- `End`
- `Label:<ラベル名>`

バリデーション:
- `Label:` の後ろが空はエラー
- 参照ラベルが存在しない場合はImportエラー

---

## 4. SearchArea仕様

対象列:
- `SearchAreaKind`
- `X1`
- `Y1`
- `X2`
- `Y2`

許容値:
- `EntireDesktop`
- `AreaOfDesktop`
- `FocusedWindow`
- `AreaOfFocusedWindow`

ルール:
- Area系は `X1,Y1,X2,Y2` 必須
- `X2 > X1` かつ `Y2 > Y1`

---

## 5. Action別必須列

### 5.1 Mouse

#### MouseClick
必須:
- `MouseButton`
- `ClickType`
- `Relative`
- `X`
- `Y`

#### MouseMove
必須:
- `Relative`
- `StartX`
- `StartY`
- `EndX`
- `EndY`
- `DurationMs`

#### MouseWheel
必須:
- `WheelOrientation`
- `WheelValue`

### 5.2 Key

#### KeyPress
必須:
- `KeyOption`
- `Key`
- `Count`

### 5.3 Wait

#### Wait
必須:
- `WaitingMs`

#### WaitForPixelColor
必須:
- `X`
- `Y`
- `Color`
- `Tolerance`
- `WaitingMs`
- `TrueGoTo`
- `FalseGoTo`

#### WaitForTextInput
必須:
- `Text`
- `WaitingMs`
- `TrueGoTo`
- `FalseGoTo`

### 5.4 Detection

#### FindImage
必須:
- `SearchAreaKind`
- `Tolerance`
- `BitmapKind`
- `BitmapValue`
- `WaitingMs`
- `TrueGoTo`
- `FalseGoTo`

制約:
- `BitmapKind` は `CapturedBitmap` または `FilePath` のみを許可する
- `Variable` / `Embedded` / その他の画像ソース種別は Import エラーとする
- `BitmapKind=FilePath` の場合、`BitmapValue` は画像ファイルパスとする
- `BitmapKind=CapturedBitmap` の CSV 上の具体表現は未確定とする

#### FindTextOcr
必須:
- `Text`
- `Language`
- `SearchAreaKind`
- `WaitingMs`
- `TrueGoTo`
- `FalseGoTo`

### 5.5 Control Flow

#### GoTo
必須:
- `GoTo`

#### If
必須:
- `VariableName`
- `ConditionType`
- `ConditionValue`
- `TrueGoTo`
- `FalseGoTo`

#### Repeat
必須:
- `StartLabel`
- `RepeatMode`
- `FinishGoTo`

RepeatMode別必須:
- `Seconds` → `Seconds`
- `Repetitions` → `Repetitions`
- `Until` → `Until`

#### EmbedMacroFile
必須:
- `Path`

#### ExecuteProgram
必須:
- `Path`

---

## 6. 列挙値標準

### 6.1 MouseClick
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`

### 6.2 Detection
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`

### 6.3 その他
- `WheelOrientation`: `Horizontal / Vertical`
- `KeyOption`: `Press / Down / Up`

## 7. エラー方針

以下はImportエラーとする。
- 必須列が空
- 数値変換不可
- 範囲外
- 未知Action
- ラベル参照不整合

---

## 8. Import from CSV の取り込み先
- 取り込み先は **現在編集中のMacro** に固定する。
- CSVの内容は **選択された行位置から追加** する。
- 新規Macroを自動作成するImportモードは本版では持たない。

---
以上
