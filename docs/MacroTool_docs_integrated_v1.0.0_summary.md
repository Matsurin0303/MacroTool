# MacroTool 統合版 docs サマリ

本ファイルは、これまで反映したチケット内容を統合した **配布用 docs 一式** のサマリです。

## 統合版の位置付け
- ベース: `docs.zip`
- 反映済み: `TKT-001` から `TKT-029`
- 本版対象外として整理済み:
  - `TKT-005` Pause/Resume
  - `TKT-020` Schedule macro
- 互換ポリシー:
  - `Macro_v1.0.0`
  - `CSV_v1.0`

## 主な反映内容
- `Macro仕様書_v7` 基準への整合
- v7対象外機能の除外または将来予定化
- UI / Domain / Playback / FileFormats の用語統一
- CSV / JSON / DTO の不足補完
- リンク切れ・旧パス参照の修正
- 空だった重要仕様書の補完
- 変数仕様、Recent Files、Playback Repeat、Repeat、ScreenCapture、OCR / 画像認識の詳細仕様確定
- 互換ポリシーを `CSV_v1.0` 運用へ整理

## 同梱内容
- `00_Index.md` から `09_Release/*` までの統合済み仕様書
- `images/` 配下の参照画像
- `05_UI/_sources/` 配下の参照用Excel

## この統合版で除外したもの
- チケットごとの `TKT-*_change_summary.md`
  - 作業ログは除外し、配布用 docs として整理しています

## 確認結果
- Markdown の相対リンク切れ: 0 件
- 画像参照リンク切れ: 0 件

## 補足
- `08_Test` は元資料構成のまま残しています
- `CSV_v1_to_v2.md` は将来予定 / 参考資料として残しています
