# TKT-001〜TKT-004 修正内容まとめ

## 対応対象
- TKT-001 Functional_Spec を Macro仕様書_v7 基準へ更新
- TKT-002 v7 対象外機能の混入整理
- TKT-003 UI_Spec に v7 対象機能を戻す
- TKT-004 CSV / JSON / DTO の不足補完

## 今回修正したファイル
- `docs/02_Requirements/Functional_Spec.md`
- `docs/05_UI/UI_Spec.md`
- `docs/04_Domain/Domain_Model.md`
- `docs/07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Schema_v1.0.md`
- `docs/07_FileFormats/CSV_Column_To_DTO_Mapping.md`
- `docs/07_FileFormats/Macro_FileFormat_Spec.md`

## 今回の修正ポイント
### TKT-001
- `Functional_Spec.md` の基準資料を `Macro仕様書_v7.xlsx` に更新
- 旧版基準の説明を削除
- 一覧を v7 の `v1.0` シートに合わせて再整理

### TKT-002
- 本版対象外として以下を除外
  - `WaitForScreenChange`
  - `After playback`
  - `Playback filter`
  - `RegEx`
- `Domain_Model.md` と `Macro_FileFormat_Spec.md` も同じスコープに統一

### TKT-003
- `UI_Spec.md` で以下を本版対象へ修正
  - `Schedule macro`
  - `Wait for text input`
  - `Embed macro file`
  - `Execute program`
- `Wait for screen change` は参考画像 / 本版対象外へ修正
- `Text` 表記を `Hotkey` 表記へ修正

### TKT-004
- CSV / JSON / DTO へ以下を追加
  - `WaitForTextInput`
  - `EmbedMacroFile`
  - `ExecuteProgram`
  - `Path` 列
- `WaitForScreenChange` をファイル形式定義から除外

## 今回あえて未確定のまま残した事項
以下は別チケットで確定する前提で、今回の文書では決め打ちしていない。
- `MouseButton` / `ClickType` の厳密な列挙値
- `BitmapKind` / `BitmapValue` の厳密ルール
- `Import from CSV` の追加位置詳細
- Schedule macro の実行衝突時の扱い
