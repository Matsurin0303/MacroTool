# TKT-022 / Repeat 厳密ルール 変更サマリ

## 変更方針
- Repeat の曖昧だった実行ルールを本版仕様として確定した。
- 確定内容は次のとおり。
  - 繰り返し範囲は **`startLabel` の行から Repeat 行の直前まで**
  - `startLabel` 未解決は **保存時 / 読込時エラー**
  - Repeat の **ネストは禁止**
  - `Infinite` は **Stop操作またはエラー発生まで継続**
  - `finishGoTo` は **繰り返し完了後に1回だけ適用**

## 変更対象
- `02_Requirements/Functional_Spec.md`
- `04_Domain/Domain_Model.md`
- `06_Playback/Playback_Spec.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Schema_v1.0.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`

## 主な変更内容
1. `Functional_Spec.md`
   - Repeat の基本ルールを章立てで追加
   - Repeat 行、Start Label、Infinite、Finish Label の備考へ確定ルールを反映

2. `Domain_Model.md`
   - Repeat 集約ルールとして範囲、未解決Label、ネスト禁止、Infinite、finishGoToの適用タイミングを明記

3. `Playback_Spec.md`
   - 実行時ルールとして Repeat の範囲と停止条件を追加

4. `CSV_Import_Spec_v1.0.0.md`
   - Repeat の Import 制約へ `StartLabel` 解決必須、ネスト禁止、Infinite の扱いを追加
   - Import エラー一覧へ Repeat 関連エラーを追加

5. `CSV_Schema_v1.0.md`
   - `StartLabel` / `FinishGoTo` の意味を明確化
   - Repeat 実行ルール章を追加

6. `Macro_FileFormat_Spec.md`
   - `Repeat.data` の意味論を補完

## 完了条件
- Repeat の範囲・ネスト・Infinite・finishGoTo の挙動が文書間で一致している
- `startLabel` 未解決時の扱いが仕様として明記されている
- CSV / JSON / 実行仕様の間で Repeat の解釈差が残っていない
