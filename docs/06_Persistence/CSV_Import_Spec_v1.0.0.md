---
date: 2026-02-26
title: Macro CSV Import Specification
version: Macro_v1.0.0
---

# Macro CSV Import Specification（Macro_v1.0.0）

## 1. 目的

本ドキュメントは、MacroTool における CSV Import 仕様を定義する。 対象は
v1.0.0 時点で実装済みの機能のみとする。
将来実装予定の機能は本仕様に含めない。

------------------------------------------------------------------------

# 2. 共通仕様

## 2.1 共通必須列

-   `Order`
-   `Action`

## 2.2 共通任意列

-   `Label`
-   `Comment`

### Order

-   数値必須
-   Import後に内部で再採番してよい

### Action

-   固定トークン（英語）
-   未知Actionは Importエラー

### Label

-   Macro内で一意
-   重複時は末尾に連番付与

------------------------------------------------------------------------

# 3. GoToターゲット形式

対象列：

-   `GoTo`
-   `TrueGoTo`
-   `FalseGoTo`
-   `FinishGoTo`

## 許容値

-   `Start`
-   `Next`
-   `End`
-   `Label:<ラベル名>`

例:

Label:Jump先

### バリデーション

-   `Label:` の後ろが空はエラー
-   参照ラベルが存在しない場合は Importエラー

------------------------------------------------------------------------

# 4. SearchArea仕様

対象列:

-   `SearchAreaKind`
-   `X1,Y1,X2,Y2`

## 許容値

-   `EntireDesktop`
-   `AreaOfDesktop`
-   `FocusedWindow`
-   `AreaOfFocusedWindow`

### ルール

-   Area系は X1,Y1,X2,Y2 必須
-   X2 \> X1 かつ Y2 \> Y1

------------------------------------------------------------------------

# 5. Action別 必須列一覧

## 5.1 Mouse

### MouseClick

必須: - MouseButton - ClickType - Relative - X - Y

------------------------------------------------------------------------

### MouseMove

必須: - Relative - StartX - StartY - EndX - EndY - DurationMs

------------------------------------------------------------------------

### MouseWheel

必須: - WheelOrientation - WheelValue

------------------------------------------------------------------------

## 5.2 Key

### KeyPress

必須: - KeyOption - Key - Count

------------------------------------------------------------------------

## 5.3 Wait

### Wait

必須: - WaitingMs

------------------------------------------------------------------------

### WaitForPixelColor

必須: - X - Y - Color (#RRGGBB) - Tolerance (0-100) - WaitingMs -
TrueGoTo - FalseGoTo

------------------------------------------------------------------------

### WaitForScreenChange

必須: - SearchAreaKind - WaitingMs - TrueGoTo - FalseGoTo

MouseActionBehavior が空欄の場合は無効。 SaveXVariable と SaveYVariable
は両方セットで指定。

------------------------------------------------------------------------

## 5.4 Detection

### FindImage

必須: - SearchAreaKind - Tolerance - BitmapKind - BitmapValue -
WaitingMs - TrueGoTo - FalseGoTo

------------------------------------------------------------------------

### FindTextOcr

必須: - Text - Language - SearchAreaKind - WaitingMs - TrueGoTo -
FalseGoTo

------------------------------------------------------------------------

## 5.5 Control Flow

### GoTo

必須: - GoTo

------------------------------------------------------------------------

### If

必須: - VariableName - ConditionType - ConditionValue - TrueGoTo -
FalseGoTo

------------------------------------------------------------------------

### Repeat

必須: - StartLabel - RepeatMode - FinishGoTo

RepeatMode別必須:

-   Seconds → Seconds
-   Repetitions → Repetitions
-   Until → Until

------------------------------------------------------------------------

# 6. エラー方針

以下は Importエラーとする:

-   必須列が空
-   数値変換不可
-   範囲外
-   未知Action
-   ラベル参照不整合
-   SaveXVariable / SaveYVariable の片側のみ指定

------------------------------------------------------------------------

# 7. 配置場所

本仕様書は以下に配置することを推奨する:

    docs/06_Persistence/CSV_Import_Spec_v1.0.0.md

FileFormat_Spec.md とは分離し、
永続化仕様とImportバリデーション仕様を分けて管理する。

------------------------------------------------------------------------

以上
