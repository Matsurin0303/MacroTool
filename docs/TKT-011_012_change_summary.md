# TKT-011 / TKT-012 修正サマリ

## 対象チケット
- TKT-011 文書リンク・画像リンク修正
- TKT-012 旧ディレクトリ参照の一掃

## 今回修正した文書
- `00_Index.md`
- `02_Requirements/Product_Spec.md`
- `02_Requirements/Compatibility_Policy.md`
- `07_FileFormats/Version_Mapping_Table.md`
- `07_FileFormats/Migrations/CSV_v1_to_v2.md`

## 修正内容
### 1. クリック可能リンクの修正
- `00_Index.md` の CSV Import / CSV Export リンクを実在する `*_v1.0.0.md` へ修正
- `00_Index.md` のバージョニング規約リンクを `09_Release/Versioning_Rule.md` へ修正
- `Product_Spec.md` のドメイン仕様・UI仕様・Versioning リンクを相対パスで修正

### 2. 旧ディレクトリ参照の更新
- `Product_Spec.md` の旧 `docs/02_FunctionSpec` / `docs/03_UI` 参照を現行構成へ更新
- `Compatibility_Policy.md` の旧 `docs/06_Persistence` 参照を現行構成へ更新
- `Version_Mapping_Table.md` の旧 `docs/06_Persistence` 参照を現行構成へ更新
- `CSV_v1_to_v2.md` の推奨配置場所を現行構成へ更新

## 完了確認
- Markdownリンクのリンク切れが解消されている
- 旧ディレクトリ `docs/02_FunctionSpec` `docs/03_UI` `docs/06_Persistence` への参照が、実運用文書から除去されている
- `Docs_Reorg_Plan.md` のような移行計画書は、意図的に旧配置比較を残しているため今回の修正対象外
