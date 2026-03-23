# CSV Column → DTO Mapping Specification
(Macro_v1.0.0)

---

## 1. 目的

本ドキュメントは、CSVヘッダと Domain 変換前の中間DTO（`CsvRowDto`）とのマッピングを定義する。

---

## 2. 設計方針

- CSVヘッダ順は固定
- DTOはヘッダに対応するプロパティを持つ
- `CsvRowDto -> StepDraftDto -> Domain` の順で変換する
- 未使用列もDTOでは保持する

---

## 3. 共通列

| CSV列 | DTOプロパティ | 型 | 必須 | 備考 |
|---|---|---|---|---|
| Order | Order | int | 必須 | Import後に再採番可 |
| Action | Action | string | 必須 | 未知Actionはエラー |
| Label | Label | string? | 任意 | 重複時は連番付与 |
| Comment | Comment | string? | 任意 | |

---

## 4. GoTo関連

| CSV列 | DTO | 型 | 必須条件 |
|---|---|---|---|
| GoTo | GoTo | GoToTargetDto? | Action=GoTo |
| TrueGoTo | TrueGoTo | GoToTargetDto? | Action依存 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | Action依存 |
| FinishGoTo | FinishGoTo | GoToTargetDto? | Action=Repeat |
| StartLabel | StartLabel | string? | Action=Repeat |
| RepeatMode | RepeatMode | string? | Action=Repeat |
| Seconds | Seconds | int? | RepeatMode=Seconds |
| Repetitions | Repetitions | int? | RepeatMode=Repetitions |
| Until | Until | string? | RepeatMode=Until |

### GoToTargetDto
- `Kind`: `Start / Next / End / Label`
- `LabelName`: `string?`（Kind=Label の場合必須）

---

## 5. SearchArea関連

| CSV列 | DTO | 型 | 必須条件 |
|---|---|---|---|
| SearchAreaKind | SearchAreaKind | string? | Action依存 |
| X1 | X1 | int? | Area系 |
| Y1 | Y1 | int? | Area系 |
| X2 | X2 | int? | Area系 |
| Y2 | Y2 | int? | Area系 |

---

## 6. Mouse系

### MouseClick
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| MouseButton | MouseButton | string? | 必須 |
| ClickType | ClickType | string? | 必須 |
| Relative | Relative | bool? | 必須 |
| X | X | int? | 必須 |
| Y | Y | int? | 必須 |

### MouseMove
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| Relative | Relative | bool? | 必須 |
| StartX | StartX | int? | 必須 |
| StartY | StartY | int? | 必須 |
| EndX | EndX | int? | 必須 |
| EndY | EndY | int? | 必須 |
| DurationMs | DurationMs | int? | 必須 |

### MouseWheel
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| WheelOrientation | WheelOrientation | string? | 必須 |
| WheelValue | WheelValue | int? | 必須 |

---

## 7. Key系

### KeyPress
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| KeyOption | KeyOption | string? | 必須 |
| Key | Key | string? | 必須 |
| Count | Count | int? | 必須 |

補足:
- `Hotkey` 用の独立DTOは持たない
- Hotkey は複数行の `KeyPress` DTO 群として受け取り、そのまま Domain へ渡す
- `Action=Hotkey` は DTO 変換前のバリデーションでエラーとする

---

## 8. Wait系

### Wait
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| WaitingMs | WaitingMs | int? | 必須 |

### WaitForPixelColor
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| X | X | int? | 必須 |
| Y | Y | int? | 必須 |
| Color | Color | string? | 必須 |
| Tolerance | Tolerance | int? | 必須 |
| WaitingMs | WaitingMs | int? | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto? | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | 必須 |

### WaitForTextInput
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| Text | Text | string? | 必須 |
| WaitingMs | WaitingMs | int? | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto? | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | 必須 |

---

## 9. Detection系

### FindImage
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| SearchAreaKind | SearchAreaKind | string? | 必須 |
| X1 | X1 | int? | Area系 |
| Y1 | Y1 | int? | Area系 |
| X2 | X2 | int? | Area系 |
| Y2 | Y2 | int? | Area系 |
| Tolerance | Tolerance | int? | 必須 |
| BitmapKind | BitmapKind | string? | 必須 (`CapturedBitmap / FilePath`) |
| BitmapValue | BitmapValue | string? | 必須 (`BitmapKind` に従う) |
| WaitingMs | WaitingMs | int? | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto? | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | 必須 |
| MouseActionBehavior | MouseActionBehavior | string? | 任意 |
| MousePosition | MousePosition | string? | 任意 |
| SaveXVariable | SaveXVariable | string? | 任意 |
| SaveYVariable | SaveYVariable | string? | 任意 |

### FindTextOcr
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| Text | Text | string? | 必須 |
| Language | Language | string? | 必須 |
| SearchAreaKind | SearchAreaKind | string? | 必須 |
| X1 | X1 | int? | Area系 |
| Y1 | Y1 | int? | Area系 |
| X2 | X2 | int? | Area系 |
| Y2 | Y2 | int? | Area系 |
| WaitingMs | WaitingMs | int? | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto? | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | 必須 |
| MouseActionBehavior | MouseActionBehavior | string? | 任意 |
| MousePosition | MousePosition | string? | 任意 |
| SaveXVariable | SaveXVariable | string? | 任意 |
| SaveYVariable | SaveYVariable | string? | 任意 |

---

## 10. Control Flow系

### If
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| VariableName | VariableName | string? | 必須 |
| ConditionType | ConditionType | string? | 必須 |
| ConditionValue | ConditionValue | string? | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto? | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto? | 必須 |

### EmbedMacroFile
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| Path | Path | string? | 必須 |

### ExecuteProgram
| CSV列 | DTO | 型 | 必須 |
|---|---|---|---|
| Path | Path | string? | 必須 |

---

## 11. 列挙値標準

### 11.1 MouseClick
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`

### 11.2 Detection
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`

### 11.3 その他
- `WheelOrientation`: `Horizontal / Vertical`
- `KeyOption`: `Press / Down / Up`

## 12. 変数関連補足
- `VariableName` / `SaveXVariable` / `SaveYVariable` は DTO 上では `string?` のまま保持する
- 命名規則 `^[A-Za-z_][A-Za-z0-9_]*$` の検証は Application 層で行う
- 変数参照は大文字小文字を区別しないため、比較・解決時は正規化して扱う
- DTO は変数値を保持しない。変数値は Playback 実行時コンテキストで管理する
- `Save Coordinate` により保存される X / Y は Domain 変換後に数値変数として扱う

## 13. エラー判定責務

### Application層
- 必須列不足
- 型変換失敗
- 範囲外
- 未知Action

### Domain層
- Label一意制約
- GoTo整合性
- SearchArea整合性

---

## 14. 本書で未確定とする事項
- `BitmapKind=CapturedBitmap` の CSV 上の具体表現

---
以上
