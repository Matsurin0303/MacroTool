# TKT-027 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-027
- 内容: Save確認を含む未保存変更確認ダイアログの文言・タイトル・ボタン構成を統一

## 反映内容

- 共通の未保存変更確認ダイアログ仕様を追加
- タイトルを **`未保存の変更`** に統一
- 本文を **`変更内容は保存されていません。保存しますか？`** に統一
- ボタン構成を **`保存` / `保存しない` / `キャンセル`** に統一
- `New` / `Open` / `Recent Files` / `New Record` / `Exit` の起動元操作継続ルールを明記
- `Save確認` の誤った遷移説明（既存マクロを開く等）を除去

## 主な更新ファイル

- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Architecture_Overview.md`
- `03_Architecture/Error_Handling_Policy.md`
- `05_UI/UI_Spec.md`

## 備考

- 未保存変更確認はエラーではなく分岐として扱う。
- 保存失敗時は起動元操作へ進まない。
