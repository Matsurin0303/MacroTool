# TKT-020 / Schedule macro 将来予定化 変更サマリ

## 変更方針
- `Schedule macro` は本版対象から外し、**将来予定**として扱う。
- そのため、実行衝突時挙動を決める `TKT-020` は **本版ではクローズ** とする。

## 変更対象
- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Architecture_Overview.md`
- `05_UI/UI_Spec.md`

## 主な変更内容
1. `Functional_Spec.md`
   - `1-8 Schedule macro` 配下を **将来予定 / 本版対象外** に変更
   - 実装しない旨を明記

2. `Architecture_Overview.md`
   - `Schedule macro` ダイアログを **将来予定 / 本版対象外** と明記
   - 本版のアーキテクチャ対象外であることを追記

3. `UI_Spec.md`
   - 画像一覧の `Schedule macro` を **参考画像 / 将来予定 / 本版対象外** に変更
   - 未確定事項ではなく、将来予定として扱う記述へ変更

## 完了条件
- `Schedule macro` が本版の対象機能として残っていない
- 本版での詳細挙動の確定チケットが不要になっている
