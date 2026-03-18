# TKT-007 / TKT-010 / TKT-014 / TKT-015 反映サマリ

更新日: 2026-03-18

## 対応対象
- TKT-007 Functional 名称を v7 に統一
- TKT-010 Hotkey / Text の混線修正
- TKT-014 MouseClick 表現の統一
- TKT-015 列名・値名の揺れ修正

## 主な修正点

### 1. Functional / UI の名称整理
- `Functional_Spec.md` に名称統一ルールを追加
- UI表示名と Action token を区別するルールを明記
- `Text` は独立Actionではなく、本版対象は `Key press` / `Hotkey` のみであることを追記
- `UI_Spec.md` に `2-5-3_Text.png` を参考画像 / 本版対象外として明記

### 2. MouseClick の永続化表現を確定
- `MouseButton`: `Left / Right / Middle / SideButton1 / SideButton2`
- `ClickType`: `Click / DoubleClick / Down / Up`

### 3. Detection 関連の列挙値を確定
- `MouseActionBehavior`: `Positioning / LeftClick / RightClick / MiddleClick / DoubleClick`
- `MousePosition`: `Center / TopLeft / TopRight / BottomLeft / BottomRight`

### 4. 用語揺れを統一
- CSVヘッダは `Action` を正とする
- 待機時間系は `WaitingMs` を正とする
- 位置表現は `Center` を正とし、`Centered` は使用しない
- マイグレーション文書のヘッダにも `Path` を追加して現行CSV定義と整合させた

## 更新ファイル
- `02_Requirements/Functional_Spec.md`
- `05_UI/UI_Spec.md`
- `07_FileFormats/CSV_Schema_v1.0.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Column_To_DTO_Mapping.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`
- `07_FileFormats/Migrations/CSV_v1_to_v2.md`

## 補足
今回も以下は未確定のまま残している。
- `BitmapKind` / `BitmapValue` の厳密ルール
- `Import from CSV` の追加位置詳細
