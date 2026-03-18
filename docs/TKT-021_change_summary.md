# TKT-021 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-021
- 内容: Hotkey の内部表現 / 保存表現 / 復元ルールを明文化

---

## 1. 背景

既存文書には「Hotkey は KeyPress 群へ展開する」という方針はあったが、以下が未完了だった。

- 編集時の内部表現
- 保存時の正規化順
- JSON / CSV 読込時の復元ルール
- `Action=Hotkey` を許可するかどうか

---

## 2. 今回の確定内容

- `Hotkey` は編集時のみ存在する複合入力表現とする
- 永続化および再生の正規表現は `KeyPress` とする
- 保存時は Hotkey を複数の `KeyPress` へ正規化する
- 正規化順は「修飾キー Down 群 → 主キー Press → 修飾キー Up 群（逆順）」とする
- JSON / CSV では `Hotkey` Action を保存しない
- 読込後は `KeyPress` 群として扱い、`Hotkey` への自動再構成は行わない
- CSV Import で `Action=Hotkey` が来た場合はエラーとする

---

## 3. 修正ファイル

- `04_Domain/Domain_Model.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`
- `07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Column_To_DTO_Mapping.md`
- `07_FileFormats/CSV_Schema_v1.0.md`

---

## 4. 補足

本対応により、保存形式上の Action 一覧は `KeyPress` のみで一貫する。  
一方で、読込時に `KeyPress` 群を UI 上の 1つの Hotkey 入力へ再統合する仕様は、本版では定義しない。

---
以上
