# 仕様書インデックス

本フォルダ（`/docs`）配下が本プロジェクトの仕様書の正（Single Source of Truth）です。  
リリースタグ `Macro_vX.Y.Z` を切る時点で、本フォルダの内容は凍結（Freeze）されていること。

---

# 1. まず読む（全体像）

- [プロダクト仕様（概要・範囲・用語）](./02_Requirements/Product_Spec.md)
- [機能仕様（状態付き）](./02_Requirements/Functional_Spec.md)
- [UI仕様（画像＋操作＋入力制約）](./05_UI/UI_Spec.md)

---

# 2. 実装設計（DDD / アーキテクチャ）

- [アーキテクチャ概要](./03_Architecture/Architecture_Overview.md)
- [Application Service 仕様](./03_Architecture/Application_Service_Spec.md)
- [ドメイン仕様（用語・不変条件）](./04_Domain/Domain_Model.md)
- [集約境界仕様](./03_Architecture/Aggregate_Boundary_Spec.md)

---

# 3. 実行（Playback）

- [再生仕様](./06_Playback/Playback_Spec.md)
- [再生状態遷移](./06_Playback/Playback_StateMachine.md)
- [入力シミュレーション仕様](./06_Playback/InputSimulation_Spec.md)

---

# 4. ファイル仕様

- [マクロJSON仕様](./07_FileFormats/Macro_FileFormat_Spec.md)
- [CSV Import仕様](./07_FileFormats/CSV_Import_Spec.md)
- [CSV Export仕様](./07_FileFormats/CSV_Export_Spec.md)
- [互換ポリシー](./02_Requirements/Compatibility_Policy.md)

---

# 5. 品質・運用

- [非機能仕様](./02_Requirements/NonFunctional_Spec.md)
- [テスト方針](./08_Test/Test_Strategy.md)
- [バージョニング規約](../README.md)

---

## UI画像

- 画像は `docs/images` に格納する
- GitHub参照用：`docs/images` 配下