---
date: 2026-02-26
title: "CSV Migration Spec: CSV_v1.0 to CSV_v2.0"
version: Macro_v1.0.0
---

# CSVマイグレーション仕様：CSV_v1.0 → CSV_v2.0

## 1. 目的

本ドキュメントは、`CSV_v1.0` 形式のマクロCSVを、Import時に自動変換して
`CSV_v2.0` として扱うための
**具体的な差分仕様（変換規約・既定値・失敗条件）**を定義する。

-   変換は **段階的マイグレーション**の1ステップ（v1→v2）である
-   変換後は **必ずCSV_v2.0として検証可能**な状態になることを保証する

------------------------------------------------------------------------

## 2. スキーマ差分の概要（v1 → v2）

### 2.1 変更点（結論）

-   **CSV列（ヘッダ）は変更しない（v1.0 と同一）**
-   **メタ行（コメント行）を導入**し、v2では `#SchemaVersion=CSV_v2.0`
    を先頭に付与する
-   値の表記ゆれを吸収するため、いくつかの値を
    **正規化（normalize）**する

> v2は「列定義の互換を維持したまま、識別と安定運用のためのメタ情報を導入する版」とする。

------------------------------------------------------------------------

## 3. 入力（CSV_v1.0）の成立条件

入力が以下を満たさない場合、マイグレーション以前に **スキーマ判定不可 →
Importエラー** とする。

-   先頭行がヘッダ行であり、以下の
    **CSV_v1.0固定ヘッダと完全一致**すること

``` csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue
```

※ v1.0ではメタ行は存在しない前提。もし `#`
から始まる行が先頭にある場合は
**v2以降**として扱う（本マイグレーション対象外）。

------------------------------------------------------------------------

## 4. 出力（CSV_v2.0）の成立条件

### 4.1 ファイル構造

-   1行目：必須メタ行
-   2行目以降：任意メタ行（0行以上）
-   最初の非メタ行：ヘッダ行（v1と同一）
-   以降：データ行

### 4.2 必須メタ行（v2.0）

    #SchemaVersion=CSV_v2.0

### 4.3 任意メタ行（推奨）

以下は任意（Importは未知キーを無視してよい）。Exportは将来追加してよい。

-   `#ExportedBy=MacroTool`
-   `#ExportedAt=YYYY-MM-DDThh:mm:ss+09:00`

------------------------------------------------------------------------

## 5. 変換規約（v1→v2）

### 5.1 メタ行の付与（必須）

入力（v1.0）に対し、出力先頭に以下を挿入する：

1.  `#SchemaVersion=CSV_v2.0`

※ 既存のCSVデータ行やヘッダは変更しない（次項の正規化を除く）。

------------------------------------------------------------------------

### 5.2 値の正規化（normalize）

v1で許容していた（または現場で発生しうる）表記ゆれを、v2の正規表記に統一する。

#### 5.2.1 GoToターゲット（GoTo / TrueGoTo / FalseGoTo / FinishGoTo）

-   トリム（前後空白除去）
-   大文字小文字の揺れを吸収（ケースインセンシティブ）し、正規表記へ統一

  ----------------------------------------------------------------------------------------------
  入力例                              出力（正規）
  ----------------------------------- ----------------------------------------------------------
  `start`, `START`                    `Start`

  `next`, `NEXT`                      `Next`

  `end`, `END`                        `End`

  `label:Jump先`, `Label:Jump先`      `Label:Jump先`（`Label:`は先頭固定、ラベル名はそのまま）
  ----------------------------------------------------------------------------------------------

#### 5.2.2 bool（Relative）

-   トリム
-   `true/false` の大小文字を吸収し `True/False`
    に統一（または実装のboolパーサに合わせて統一）

  入力例             出力（正規）
  ------------------ --------------
  `true`, `TRUE`     `True`
  `false`, `FALSE`   `False`

#### 5.2.3 Color

-   トリム
-   `#RRGGBB` 以外は変換しない（`0xRRGGBB` や `RRGGBB` を許容するかは
    v2.0では **許容しない**）
-   小文字16進は許容し、出力は大文字に統一してよい（例：`#ff00aa`→`#FF00AA`）

#### 5.2.4 enum類（MouseActionBehavior / MousePosition / WheelOrientation / ClickType 等）

-   トリム
-   大文字小文字の揺れを吸収し、正規表記へ統一
-   既知値にマッピングできない場合は Importエラー（丸めない）

------------------------------------------------------------------------

### 5.3 既定値補完（Default）

v2.0では、以下の
**暗黙既定値**を明文化し、v1入力で空欄の場合に補完してよい。

#### 5.3.1 MousePosition（FindImage / FindTextOcr で MouseActionBehavior が有効な場合）

-   条件：`MouseActionBehavior` が空欄ではない AND `MousePosition`
    が空欄
-   補完：`MousePosition=Centered`

※ MouseActionBehavior が空欄（Mouse action無効）なら MousePosition
は空欄のままでよい。

------------------------------------------------------------------------

## 6. 変換不能（Importエラー）条件

以下に該当する場合、v1→v2変換は失敗（Importエラー）とする。

-   ヘッダが v1.0 と一致しない（スキーマ判定不能）
-   `Order` が数値に変換できない
-   `WaitingMs / DurationMs / Count / Seconds / Repetitions / WheelValue`
    等の数値が変換できない、または範囲外
-   `Tolerance` が数値に変換できない、または `0..100` 外
-   `Color` が `#RRGGBB` 形式ではない（WaitForPixelColor行で必須）
-   enum値（MouseActionBehavior 等）が既知の値にマッピングできない
-   `SaveXVariable` と `SaveYVariable`
    の片側だけが指定されている（ペア不整合）
-   GoToターゲットが仕様形式ではない（`Start/Next/End/Label:`以外）

> 注意：GoTo参照先ラベルの実在チェックは「マイグレーション後のImport検証フェーズ」で実施してよい。

------------------------------------------------------------------------

## 7. 出力例

### 7.1 v1（入力）

``` csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue
0,Wait,,,EntireDesktop,,,,,1000,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
```

### 7.2 v2（出力）

``` csv
#SchemaVersion=CSV_v2.0
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue
0,Wait,,,EntireDesktop,,,,,1000,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,
```

------------------------------------------------------------------------

## 8. 推奨配置場所

    docs/06_Persistence/Migrations/CSV_v1_to_v2.md

------------------------------------------------------------------------

以上
