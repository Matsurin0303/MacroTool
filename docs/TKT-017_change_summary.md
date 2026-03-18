# TKT-017 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: `TKT-017`
- 確定内容:
  - 変数型は `文字列 / 数値`
  - 未設定状態は `Undefined`
  - 変数スコープは `1回のPlayback実行単位`
  - Playback開始時に全変数を初期化
  - 変数名は `^[A-Za-z_][A-Za-z0-9_]*$`
  - 変数参照は大文字小文字を区別しない
  - `Save Coordinate` の X / Y は数値として格納

## 修正ファイル
- `02_Requirements/Functional_Spec.md`
- `04_Domain/Domain_Model.md`
- `06_Playback/Playback_Spec.md`
- `07_FileFormats/CSV_Schema_v1.0.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Column_To_DTO_Mapping.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`

## 反映要点
- 変数の型・初期状態・スコープ・命名規則を仕様書へ明文化
- Playback の開始時 / cycle開始時 / 終了時の変数ストア挙動を追加
- CSV / JSON では変数名のみを保持し、変数値は永続化しない方針を明文化
- `Save Coordinate` の保存値を数値として扱うことを明文化
