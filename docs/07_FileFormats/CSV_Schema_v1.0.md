---
date: 2026-03-18
macro_version: Macro_v1.0.0
title: CSV Schema Dictionary (CSV_v1.0)
version: CSV_v1.0
---

# CSVスキーマ定義書（CSV_v1.0）

## 1. 目的

本書は、MacroTool の CSV列（ヘッダ）を SSOT として定義する。  
Import / Export 仕様は本書に従う。

- 対象: `Macro仕様書_v7.xlsx` に存在する本版機能
- 方針: ヘッダ順固定 / 未使用列は空欄許容

---

## 2. 固定ヘッダ

```csv
Order,Action,Label,Comment,SearchAreaKind,X1,Y1,X2,Y2,WaitingMs,GoTo,TrueGoTo,FalseGoTo,FinishGoTo,MouseActionBehavior,MousePosition,SaveXVariable,SaveYVariable,Tolerance,Text,Language,BitmapKind,BitmapValue,MouseButton,ClickType,Relative,X,Y,Color,StartX,StartY,EndX,EndY,DurationMs,WheelOrientation,WheelValue,KeyOption,Key,Count,StartLabel,RepeatMode,Seconds,Repetitions,Until,VariableName,ConditionType,ConditionValue,Path
```

---

## 3. 列定義

| 列名 | 型 | 必須条件 | 主な適用Action | 備考 |
|---|---|---|---|---|
| Order | int | 全Action | 全Action | 内部再採番可 |
| Action | string | 全Action | 全Action | 固定トークン |
| Label | string | 任意 | 全Action | 重複時は連番付与 |
| Comment | string | 任意 | 全Action | メモ |
| SearchAreaKind | enum | Action依存 | FindImage / FindTextOcr | Area系は座標必須 |
| X1 | int | Area系 | FindImage / FindTextOcr | 左上X |
| Y1 | int | Area系 | FindImage / FindTextOcr | 左上Y |
| X2 | int | Area系 | FindImage / FindTextOcr | 右下X |
| Y2 | int | Area系 | FindImage / FindTextOcr | 右下Y |
| WaitingMs | int | Action依存 | Wait系 / Detection系 | 0以上 |
| GoTo | string | GoTo | GoTo | `Start/Next/End/Label:<name>` |
| TrueGoTo | string | Action依存 | Wait系 / If / Detection系 | 同上 |
| FalseGoTo | string | Action依存 | Wait系 / If / Detection系 | 同上 |
| FinishGoTo | string | Repeat | Repeat | 同上 |
| MouseActionBehavior | enum | 任意 | FindImage / FindTextOcr | `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick` |
| MousePosition | enum | 任意 | FindImage / FindTextOcr | `Center / TopLeft / TopRight / BottomLeft / BottomRight` |
| SaveXVariable | string | 任意 | FindImage / FindTextOcr | SaveYVariableと組で扱う |
| SaveYVariable | string | 任意 | FindImage / FindTextOcr | SaveXVariableと組で扱う |
| Tolerance | int | Action依存 | WaitForPixelColor / FindImage | 0..100 |
| Text | string | Action依存 | WaitForTextInput / FindTextOcr | 空不可 |
| Language | string | FindTextOcr | FindTextOcr | UI選択値 |
| BitmapKind | enum | FindImage | FindImage | `CapturedBitmap / FilePath` |
| BitmapValue | string | FindImage | FindImage | `BitmapKind` に従う値 |
| MouseButton | enum | MouseClick | MouseClick | `Left / Right / Middle / SideButton1 / SideButton2` |
| ClickType | enum | MouseClick | MouseClick | `Click / DoubleClick / Down / Up` |
| Relative | bool | MouseClick / MouseMove | MouseClick / MouseMove | True / False |
| X | int | Action依存 | MouseClick / WaitForPixelColor | 用途はAction依存 |
| Y | int | Action依存 | MouseClick / WaitForPixelColor | 用途はAction依存 |
| Color | color | WaitForPixelColor | WaitForPixelColor | `#RRGGBB` |
| StartX | int | MouseMove | MouseMove | |
| StartY | int | MouseMove | MouseMove | |
| EndX | int | MouseMove | MouseMove | |
| EndY | int | MouseMove | MouseMove | |
| DurationMs | int | MouseMove | MouseMove | 0以上 |
| WheelOrientation | enum | MouseWheel | MouseWheel | Horizontal / Vertical |
| WheelValue | int | MouseWheel | MouseWheel | |
| KeyOption | enum | KeyPress | KeyPress | Press / Down / Up |
| Key | string | KeyPress | KeyPress | 空不可 |
| Count | int | KeyPress | KeyPress | 1以上 |
| StartLabel | string | Repeat | Repeat | 空不可 |
| RepeatMode | enum | Repeat | Repeat | Seconds / Repetitions / Until |
| Seconds | int | RepeatMode=Seconds | Repeat | |
| Repetitions | int | RepeatMode=Repetitions | Repeat | |
| Until | string | RepeatMode=Until | Repeat | `HH:mm:ss` |
| VariableName | string | If | If | 空不可 |
| ConditionType | string | If | If | v7列挙値 |
| ConditionValue | string | If | If | `Value is defined` 以外で必須 |
| Path | string | EmbedMacroFile / ExecuteProgram | EmbedMacroFile / ExecuteProgram | 空不可 |

---

## 4. 既知Action一覧

- `MouseClick`
- `MouseMove`
- `MouseWheel`
- `KeyPress`
- `Wait`
- `WaitForPixelColor`
- `WaitForTextInput`
- `FindImage`
- `FindTextOcr`
- `GoTo`
- `If`
- `Repeat`
- `EmbedMacroFile`
- `ExecuteProgram`

> 上記以外は CSV_v1.0 Import ではエラーとする。

---

## 5. 列挙値標準

### 5.1 MouseClick
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`

### 5.2 Detection
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`

### 5.3 その他
- `WheelOrientation`: `Horizontal / Vertical`
- `KeyOption`: `Press / Down / Up`
- `RepeatMode`: `Seconds / Repetitions / Until / Infinite`

## 6. BitmapKind / BitmapValue ルール
- `BitmapKind` は `CapturedBitmap` または `FilePath` のみを許可する
- `BitmapKind=FilePath` の場合、`BitmapValue` は画像ファイルパスを格納する
- `BitmapKind=CapturedBitmap` の場合、`BitmapValue` は Macro 内に保持される画像データに対応する値を格納する
- `Variable` / `Embedded` / その他の画像ソース種別は本版対象外とする

## 7. 本書で未確定とする事項
- `BitmapKind=CapturedBitmap` の CSV 上の具体表現

---
以上
