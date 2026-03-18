# CSV Column → DTO Mapping Specification
(Macro_v1.0.0)

---

## 1. 目的

本ドキュメントは、CSVヘッダと Domain 変換前の中間DTO（CsvRowDto）との完全マッピングを定義する。

目的：

- Import実装の明確化
- Export整合保証
- 列追加時の影響範囲明示
- TDDによる列単位検証の容易化

---

## 2. 設計方針

- CSVヘッダ順は固定
- DTOはヘッダと同名（PascalCase）で保持
- Domainへは直接変換しない
- CsvRowDto → StepDraftDto → Domain の順で変換する
- 未使用列は保持するが Domain に渡さない

---

## 3. 中間DTO定義方針

### 3.1 CsvRowDto（1行分）

- すべてのCSV列を保持
- 型変換後の値を保持
- 必須判定は Application 層で行う
- Domain 不変条件チェックは Domain 側で行う

---

## 4. 共通列マッピング

| CSV列 | DTOプロパティ | 型 | 必須 | 備考 |
|------|-------------|----|------|------|
| Order | Order | int | 必須 | Import後に再採番可 |
| Action | Action | string / enum | 必須 | 未知Actionはエラー |
| Label | Label | string? | 任意 | 重複時は連番付与 |
| Comment | Comment | string? | 任意 | 改行はCSV規約準拠 |

---

## 5. GoTo関連

| CSV列 | DTO | 型 | 必須条件 | 備考 |
|------|------|----|------------|------|
| GoTo | GoTo | GoToTargetDto? | Action=GoTo | |
| TrueGoTo | TrueGoTo | GoToTargetDto? | Action依存 | |
| FalseGoTo | FalseGoTo | GoToTargetDto? | Action依存 | |
| FinishGoTo | FinishGoTo | GoToTargetDto? | Repeat | |
| StartLabel | StartLabel | string? | Repeat | |
| RepeatMode | RepeatMode | enum? | Repeat | Seconds/Repetitions/Until |
| Seconds | Seconds | int? | RepeatMode=Seconds | |
| Repetitions | Repetitions | int? | RepeatMode=Repetitions | |
| Until | Until | string? | RepeatMode=Until | |

### GoToTargetDto

- Kind: Start / Next / End / Label
- LabelName: string?（Kind=Label の場合必須）

---

## 6. SearchArea系

| CSV列 | DTO | 型 | 必須条件 |
|------|------|----|------------|
| SearchAreaKind | SearchAreaKind | enum? | Action依存 |
| X1 | X1 | int? | Area系 |
| Y1 | Y1 | int? | Area系 |
| X2 | X2 | int? | Area系 |
| Y2 | Y2 | int? | Area系 |

ルール：

- Area系は X2 > X1 かつ Y2 > Y1
- 非Area系は座標空欄

---

## 7. Mouse系

### MouseClick

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| MouseButton | MouseButton | enum? | 必須 |
| ClickType | ClickType | enum? | 必須 |
| Relative | Relative | bool? | 必須 |
| X | X | int? | 必須 |
| Y | Y | int? | 必須 |

---

### MouseMove

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| Relative | Relative | bool? | 必須 |
| StartX | StartX | int? | 必須 |
| StartY | StartY | int? | 必須 |
| EndX | EndX | int? | 必須 |
| EndY | EndY | int? | 必須 |
| DurationMs | DurationMs | int? | 必須 |

---

### MouseWheel

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| WheelOrientation | WheelOrientation | enum? | 必須 |
| WheelValue | WheelValue | int? | 必須 |

---

## 8. Key系

### KeyPress

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| KeyOption | KeyOption | enum? | 必須 |
| Key | Key | string/enum? | 必須 |
| Count | Count | int? | 必須 |

---

## 9. Wait系

### Wait

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| WaitingMs | WaitingMs | int? | 必須 |

---

### WaitForPixelColor

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| X | X | int? | 必須 |
| Y | Y | int? | 必須 |
| Color | Color | string (#RRGGBB) | 必須 |
| Tolerance | Tolerance | int | 必須 |
| WaitingMs | WaitingMs | int | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto | 必須 |

---

### WaitForScreenChange

| CSV列 | DTO | 型 | 必須 |
|------|------|----|------|
| SearchAreaKind | SearchAreaKind | enum | 必須 |
| WaitingMs | WaitingMs | int | 必須 |
| TrueGoTo | TrueGoTo | GoToTargetDto | 必須 |
| FalseGoTo | FalseGoTo | GoToTargetDto | 必須 |

オプション：

| CSV列 | DTO | 型 | 条件 |
|------|------|----|------|
| MouseActionBehavior | MouseActionBehavior | enum? | 空欄なら無効 |
| SaveXVariable | SaveXVariable | string? | SaveYと両方必要 |
| SaveYVariable | SaveYVariable | string? | SaveXと両方必要 |

---

## 10. Detection系

### FindImage

必須：

| CSV列 | DTO |
|------|------|
| SearchAreaKind | SearchAreaKind |
| Tolerance | Tolerance |
| BitmapKind | BitmapKind |
| BitmapValue | BitmapValue |
| WaitingMs | WaitingMs |
| TrueGoTo | TrueGoTo |
| FalseGoTo | FalseGoTo |

オプション：

- MouseActionBehavior
- MousePosition
- SaveXVariable
- SaveYVariable

---

### FindTextOcr

必須：

| CSV列 | DTO |
|------|------|
| Text | Text |
| Language | Language |
| SearchAreaKind | SearchAreaKind |
| WaitingMs | WaitingMs |
| TrueGoTo | TrueGoTo |
| FalseGoTo | FalseGoTo |

オプション：

- MouseActionBehavior
- MousePosition
- SaveXVariable
- SaveYVariable

---

## 11. If

| CSV列 | DTO | 必須 |
|------|------|------|
| VariableName | VariableName | 必須 |
| ConditionType | ConditionType | 必須 |
| ConditionValue | ConditionValue | 必須 |
| TrueGoTo | TrueGoTo | 必須 |
| FalseGoTo | FalseGoTo | 必須 |

---

## 12. エラー判定責務

### Application層

- 必須列不足
- 型変換失敗
- 範囲外
- 未知Action
- SaveX/SaveY片側のみ

### Domain層

- Label一意制約
- 状態遷移不整合
- GoTo整合性

---

## 13. 将来列追加ルール

- ヘッダ追加は Breaking Change（X+1）
- DTOに同名プロパティ追加
- 未使用列は空欄出力

---

以上。