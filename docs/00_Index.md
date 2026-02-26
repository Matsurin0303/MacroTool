# 仕様書インデックス

本フォルダ（`/docs`）配下が本プロジェクトの仕様書の正（Single Source of Truth）です。  
リリースタグ `Macro_vX.Y.Z` を切る時点で、本フォルダの内容は凍結（Freeze）されていること。

---

## 入口（まず読む）
- [プロダクト仕様（概要・範囲・用語）](./01_ProductSpec.md)
- [バージョニング規約（vX.Y.Z / タグ / リリース運用）](./Versioning.md)

---

## 実装と直結する仕様
- [機能仕様（状態付き）](./02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md)
- [UI仕様（画像＋操作＋入力制約）](./03_UI/UI_Spec.md)
- [ドメイン仕様（DDD：用語・不変条件・状態遷移）](./04_Domain/Domain_Model.md)
- [再生（実行）仕様（実行順・失敗時・キャンセル）](./05_Playback/Playback_Spec.md)
- [永続化仕様（マクロファイル形式・互換性）](./06_Persistence/FileFormat_Spec.md)

---

## 品質・運用
- [非機能仕様（性能・ログ・DPI等）](./07_NFR/NonFunctional_Spec.md)
- [テスト方針（TDD運用）](./08_Test/Test_Strategy.md)

---

## UI画像
- 画像は `docs/images` に格納する（原則移動しない）
- GitHub参照用：`docs/images` 配下
