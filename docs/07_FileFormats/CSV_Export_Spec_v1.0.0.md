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

## 5. Action別出力仕様

### 5.1 Mouse
- `MouseClick` → `MouseButton, ClickType, Relative, X, Y`
- `MouseMove` → `Relative, StartX, StartY, EndX, EndY, DurationMs`
- `MouseWheel` → `WheelOrientation, WheelValue`

### 5.2 Key
- `KeyPress` → `KeyOption, Key, Count`

### 5.3 Wait
- `Wait` → `WaitingMs`
- `WaitForPixelColor` → `X, Y, Color, Tolerance, WaitingMs, TrueGoTo, FalseGoTo`
- `WaitForTextInput` → `Text, WaitingMs, TrueGoTo, FalseGoTo`

### 5.4 Detection
- `FindImage` → `SearchAreaKind, X1, Y1, X2, Y2, Tolerance, BitmapKind, BitmapValue, WaitingMs, TrueGoTo, FalseGoTo, MouseActionBehavior, MousePosition, SaveXVariable, SaveYVariable`
- `FindTextOcr` → `Text, Language, SearchAreaKind, X1, Y1, X2, Y2, WaitingMs, TrueGoTo, FalseGoTo, MouseActionBehavior, MousePosition, SaveXVariable, SaveYVariable`

### 5.5 Control Flow
- `GoTo` → `GoTo`
- `If` → `VariableName, ConditionType, ConditionValue, TrueGoTo, FalseGoTo`
- `Repeat` → `StartLabel, RepeatMode, Seconds, Repetitions, Until, FinishGoTo`
- `EmbedMacroFile` → `Path`
- `ExecuteProgram` → `Path`

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

## 7. エンコード仕様
- UTF-8（BOMなし推奨）
- 改行コードは CRLF を推奨

---

## 8. 本書で未確定とする事項
なし

---
以上
