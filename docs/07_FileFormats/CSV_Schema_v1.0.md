---
date: 2026-02-26
macro_version: Macro_v1.0.0
title: CSV Schema Dictionary (CSV_v1.0)
version: CSV_v1.0
---

# CSVスキーマ定義書（CSV_v1.0）

## 1. 目的

本書は、MacroTool の CSV列（ヘッダ）を **単一の真実（Single Source of
Truth）** として定義する。 Import/Export 仕様書は本書の列定義に従う。

-   対象：CSV_v1.0（Macro_v1.0.0）
-   方針：ヘッダ順固定／未使用列は空欄許容
-   将来実装予定機能の列は含めない

------------------------------------------------------------------------

## 2. 固定ヘッダ（順序固定）

``` csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue
```

------------------------------------------------------------------------

## 3. データ型表記

-   `int`：整数（符号付き）
-   `bool`：`True/False`（大小文字は正規化してよい）
-   `string`：任意文字列（空欄可否は列定義に従う）
-   `enum`：許容値リストに一致する文字列（大小文字は正規化してよい）
-   `color`：`#RRGGBB`（16進6桁）

------------------------------------------------------------------------

## 4. 列定義（辞書）

> 「必須」は **当該Actionで値が空欄不可**を意味する。\
> 「適用Action」はその列を読むActionの代表例（列は他Actionでは無視してよい）。

  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  列名                  型                必須 既定値         制約/範囲                                                         適用Action（例）                            備考
  --------------------- --------- ------------ -------------- ----------------------------------------------------------------- ------------------------------------------- ------------------------------------------
  Order                 int               全行 なし           任意（並び決定に使用）                                            全て                                        Import後の再採番可

  Action                string            全行 なし           既知Actionのみ                                                    全て                                        未知はImportエラー

  Label                 string            任意 空             一意（重複時は自動採番）                                          全て                                        参照先として使用

  Comment               string            任意 空             任意                                                              全て                                        改行はCSV標準でクォート

  SearchAreaKind        enum              条件 なし           `EntireDesktop/AreaOfDesktop/FocusedWindow/AreaOfFocusedWindow`   FindImage/FindTextOcr/WaitForScreenChange   Area系は座標必須

  X1                    int               条件 空             Area系のみ必須                                                    SearchArea                                  範囲は画面座標

  Y1                    int               条件 空             Area系のみ必須                                                    SearchArea                                  同上

  X2                    int               条件 空             Area系のみ必須＆`X2>X1`                                           SearchArea                                  同上

  Y2                    int               条件 空             Area系のみ必須＆`Y2>Y1`                                           SearchArea                                  同上

  WaitingMs             int               条件 0              `>=0`                                                             Wait/Find*/WaitFor*                         タイムアウト/待機

  GoTo                  string            条件 なし           `Start/Next/End/Label:<name>`                                     GoTo                                        ターゲット形式は共通仕様

  TrueGoTo              string            条件 なし           同上                                                              If/WaitFor\* /Find\*                        成功/真

  FalseGoTo             string            条件 なし           同上                                                              If/WaitFor\* /Find\*                        失敗/偽

  FinishGoTo            string            条件 なし           同上                                                              Repeat                                      終了時の遷移先

  MouseActionBehavior   enum              任意 空             `Positioning/LeftClick/RightClick/MiddleClick/DoubleClick`        Find\*/WaitForScreenChange                  空欄なら無効

  MousePosition         enum              任意 `Centered`\*   `Centered/TopLeft/TopRight/BottomLeft/BottomRight`                Find\*                                      \*MouseAction有効時のみ既定値適用可

  SaveXVariable         string            任意 空             SaveYとペア                                                       Find\*/WaitForScreenChange                  両方空=無効、片側のみ=エラー推奨

  SaveYVariable         string            任意 空             SaveXとペア                                                       Find\*/WaitForScreenChange                  同上

  Tolerance             int               条件 0              `0..100`                                                          FindImage/WaitForPixelColor                 画像/色の許容差

  Text                  string            条件 なし           空不可                                                            FindTextOcr                                 検索文字列

  Language              string            条件 なし           空不可                                                            FindTextOcr                                 UIで選べる言語コード/名称

  BitmapKind            enum              条件 なし           v1.0は `File` のみ推奨                                            FindImage                                   将来拡張はv2以降

  BitmapValue           string            条件 なし           空不可                                                            FindImage                                   ファイルパスなど

  MouseButton           enum              条件 なし           `Left/Right/Middle`                                               MouseClick                                  

  ClickType             enum              条件 なし           `Single/Double`（UI準拠）                                         MouseClick                                  実装UIに合わせて固定

  Relative              bool              条件 False          `True/False`                                                      MouseClick/MouseMove                        相対/絶対

  X                     int               条件 0              任意                                                              MouseClick/WaitForPixelColor                Pixel/Click用途。Actionで意味が異なる

  Y                     int               条件 0              任意                                                              MouseClick/WaitForPixelColor                同上

  Color                 color             条件 なし           `#RRGGBB`                                                         WaitForPixelColor                           当該Actionで必須

  StartX                int               条件 0              任意                                                              MouseMove                                   

  StartY                int               条件 0              任意                                                              MouseMove                                   

  EndX                  int               条件 0              任意                                                              MouseMove                                   

  EndY                  int               条件 0              任意                                                              MouseMove                                   

  DurationMs            int               条件 0              `>=0`                                                             MouseMove                                   

  WheelOrientation      enum              条件 なし           `Horizontal/Vertical`                                             MouseWheel                                  

  WheelValue            int               条件 0              任意（符号あり）                                                  MouseWheel                                  

  KeyOption             enum              条件 なし           UI準拠                                                            KeyPress                                    修飾/入力方式など

  Key                   string            条件 なし           空不可                                                            KeyPress                                    

  Count                 int               条件 1              `>=1`                                                             KeyPress                                    

  StartLabel            string            条件 なし           空不可                                                            Repeat                                      Repeatブロック開始ラベル

  RepeatMode            enum              条件 なし           `Seconds/Repetitions/Until`                                       Repeat                                      

  Seconds               int               条件 0              `>=0`                                                             Repeat                                      RepeatMode=Secondsで必須

  Repetitions           int               条件 1              `>=1`                                                             Repeat                                      RepeatMode=Repetitionsで必須

  Until                 string            条件 なし           空不可                                                            Repeat                                      RepeatMode=Untilで必須（式は文字列扱い）

  VariableName          string            条件 なし           空不可                                                            If                                          

  ConditionType         enum              条件 なし           UI準拠                                                            If                                          例：Equals/Contains等（UI確定値に従う）

  ConditionValue        string            条件 なし           空不可                                                            If                                          
  --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 5. 既知Action一覧（CSV_v1.0）

-   `MouseClick`
-   `MouseMove`
-   `MouseWheel`
-   `KeyPress`
-   `Wait`
-   `WaitForPixelColor`
-   `WaitForScreenChange`
-   `FindImage`
-   `FindTextOcr`
-   `GoTo`
-   `If`
-   `Repeat`

> 上記以外は将来実装予定または対象外のため、CSV_v1.0
> Importではエラーとする。

------------------------------------------------------------------------

## 6. 参考（関連仕様書）

-   `docs/06_Persistence/CSV_Import_Spec_v1.0.0.md`
-   `docs/06_Persistence/CSV_Export_Spec_v1.0.0.md`
-   `docs/06_Persistence/Version_Mapping_Spec.md`
-   `docs/06_Persistence/Compatibility_Policy.md`

------------------------------------------------------------------------

以上
