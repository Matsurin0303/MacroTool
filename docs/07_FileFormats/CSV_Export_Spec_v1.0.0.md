---
date: 2026-02-26
title: Macro CSV Export Specification
version: Macro_v1.0.0
---

# Macro CSV Export Specification（Macro_v1.0.0）

## 1. 目的

本ドキュメントは、MacroTool における CSV Export 仕様を定義する。 対象は
v1.0.0 時点で実装済みの機能のみとする。 本仕様は
CSV_Import_Spec_v1.0.0.md と対になるものである。

------------------------------------------------------------------------

# 2. 基本方針

-   Exportフォーマットは Import仕様と完全一致させる
-   不要な列は出力しない（ただしヘッダは固定）
-   未使用列は空欄で出力する
-   内部ドメイン表現に依存しない形式で出力する

------------------------------------------------------------------------

# 3. ヘッダ（固定）

``` csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue
```

-   ヘッダ順は固定
-   将来列追加時は Macro_vX+1.0 とする

------------------------------------------------------------------------

# 4. 共通出力ルール

## 4.1 Order

-   0開始または1開始はどちらでもよい
-   出力順は画面表示順と一致させる

## 4.2 Label

-   未設定の場合は空欄
-   Domainで一意保証された値をそのまま出力

## 4.3 Comment

-   任意
-   改行はCSV仕様に従いダブルクォートで囲む

------------------------------------------------------------------------

# 5. GoTo出力形式

対象列： - GoTo - TrueGoTo - FalseGoTo - FinishGoTo

出力形式：

-   Start
-   Next
-   End
-   Label:`<ラベル名>`{=html}

例: Label:Jump先

------------------------------------------------------------------------

# 6. SearchArea出力

SearchAreaKind が Area系の場合のみ X1,Y1,X2,Y2 を出力。 それ以外は空欄。

------------------------------------------------------------------------

# 7. Action別 出力仕様

## 7.1 Mouse

### MouseClick

出力列: MouseButton, ClickType, Relative, X, Y

------------------------------------------------------------------------

### MouseMove

出力列: Relative, StartX, StartY, EndX, EndY, DurationMs

------------------------------------------------------------------------

### MouseWheel

出力列: WheelOrientation, WheelValue

------------------------------------------------------------------------

## 7.2 Key

### KeyPress

出力列: KeyOption, Key, Count

------------------------------------------------------------------------

## 7.3 Wait

### Wait

出力列: WaitingMs

------------------------------------------------------------------------

### WaitForPixelColor

出力列: X, Y, Color (#RRGGBB), Tolerance, WaitingMs, TrueGoTo, FalseGoTo

------------------------------------------------------------------------

### WaitForScreenChange

出力列: SearchAreaKind, WaitingMs, TrueGoTo, FalseGoTo

オプション: - MouseActionBehavior（空欄なら出力しない） - SaveXVariable
/ SaveYVariable（両方ある場合のみ出力）

------------------------------------------------------------------------

## 7.4 Detection

### FindImage

出力列: SearchAreaKind, Tolerance, BitmapKind, BitmapValue, WaitingMs,
TrueGoTo, FalseGoTo

オプション: MouseActionBehavior, MousePosition, SaveXVariable,
SaveYVariable

------------------------------------------------------------------------

### FindTextOcr

出力列: Text, Language, SearchAreaKind, WaitingMs, TrueGoTo, FalseGoTo

オプション: MouseActionBehavior, MousePosition, SaveXVariable,
SaveYVariable

------------------------------------------------------------------------

## 7.5 Control Flow

### GoTo

出力列: GoTo

------------------------------------------------------------------------

### If

出力列: VariableName, ConditionType, ConditionValue, TrueGoTo, FalseGoTo

------------------------------------------------------------------------

### Repeat

出力列: StartLabel, RepeatMode, FinishGoTo

RepeatMode別: - Seconds → Seconds - Repetitions → Repetitions - Until →
Until

------------------------------------------------------------------------

# 8. エンコード仕様

-   UTF-8（BOMなし推奨）
-   改行コード：CRLF

------------------------------------------------------------------------

# 9. バージョン管理

-   CSV列追加 → Macro_vX+1.0
-   列の意味変更 → Macro_vX+1.0
-   軽微な文書修正 → Macro_vX.Y+1

------------------------------------------------------------------------

# 10. 推奨配置場所

    docs/06_Persistence/CSV_Export_Spec_v1.0.0.md

Import仕様とは別ファイルで管理する。

------------------------------------------------------------------------

以上
