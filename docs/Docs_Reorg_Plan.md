# Docs 再配置プラン（MacroTool）
対象: 添付 docs.zip の現行構成を、推奨テンプレートへ寄せるための移動表と追加ファイル一覧。
## 1. 推奨ルート配置
- `src/MacroTool.Docs/docs/` に **Markdown本体**
- `src/MacroTool.Docs/docs/images/` に **画像**（現状維持）
- `src/MacroTool.Docs/README.md` を入口にし、ルート `README.md` は入口リンクのみ
## 2. 移動・リネーム表（現行 → 推奨）
| 現行パス | 移動先(推奨) | 補足 |
|---|---|---|
| `docs/00_Index.md` | `docs/00_Index.md` | そのまま（入口） |
| `docs/01_ProductSpec.md` | `docs/02_Requirements/Product_Spec.md` | 内容を維持しつつ配置変更（必要なら後でFunctionalへ統合） |
| `docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md` | `docs/02_Requirements/Functional_Spec.md` | 完成イメージ仕様。ファイル名を機能仕様に統一 |
| `docs/03_UI/UI_Spec.md` | `docs/05_UI/UI_Spec.md` | UI章へ移動 |
| `docs/03_UI/Macro仕様書_v1.xlsx` | `docs/05_UI/_sources/Macro仕様書_v1.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v2.xlsx` | `docs/05_UI/_sources/Macro仕様書_v2.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v3.xlsx` | `docs/05_UI/_sources/Macro仕様書_v3.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v4.xlsx` | `docs/05_UI/_sources/Macro仕様書_v4.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v5.xlsx` | `docs/05_UI/_sources/Macro仕様書_v5.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v6.xlsx` | `docs/05_UI/_sources/Macro仕様書_v6.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/03_UI/Macro仕様書_v7.xlsx` | `docs/05_UI/_sources/Macro仕様書_v7.xlsx` | 元資料は_sourcesに隔離（仕様本文と分離） |
| `docs/04_Domain/Domain_Model.md` | `docs/04_Domain/Domain_Model.md` | そのまま |
| `docs/05_Playback/Playback_Spec.md` | `docs/06_Playback/Playback_Spec.md` | 章番号合わせ |
| `docs/06_Persistence/Compatibility_Policy.md` | `docs/02_Requirements/Compatibility_Policy.md` | 互換ポリシーはRequirementsに寄せる |
| `docs/06_Persistence/CSV_Import_Spec_v1.0.0.md` | `docs/07_FileFormats/CSV_Import_Spec.md` | 版数は本文で管理しファイル名から除去 |
| `docs/06_Persistence/CSV_Export_Spec_v1.0.0.md` | `docs/07_FileFormats/CSV_Export_Spec.md` | 同上 |
| `docs/06_Persistence/CSV_Schema_v1.0.md` | `docs/07_FileFormats/CSV_Schema.md` | 同上 |
| `docs/06_Persistence/FileFormat_Spec.md` | `docs/07_FileFormats/Macro_FileFormat_Spec.md` | マクロJSON仕様の正式名に寄せる |
| `docs/06_Persistence/Version_Mapping_Spec.md` | `docs/07_FileFormats/Version_Mapping_Table.md` | 対応表として一本化 |
| `docs/06_Persistence/Migrations/CSV_v1_to_v2.md` | `docs/07_FileFormats/Migrations/CSV_v1_to_v2.md` | 移行手順はMigrations配下へ |
| `docs/07_NFR/NonFunctional_Spec.md` | `docs/02_Requirements/NonFunctional_Spec.md` | Requirementsに統合 |
| `docs/08_Test/Test_Strategy.md` | `docs/08_Test/Test_Strategy.md` | そのまま（章番号合わせ不要） |
| `docs/Versioning.md` | `docs/09_Release/Versioning_Rule.md` | Macro_vX.Y運用をここに集約 |
| `docs/images/*` | `docs/images/*` | そのまま（相対リンク維持） |
| `docs/images_v1.zip` | `docs/images/_archive/images_v1.zip` | 旧一式はarchiveへ（参照不要なら削除でも可） |

## 3. 新規に追加するファイル（まずは“章立てだけ”でOK）
| 追加パス | 目的 |
|---|---|
| `docs/01_Glossary.md` | 用語集（Macro/Step/Action/Label/GoTo/Playback等） |
| `docs/03_Architecture/Architecture_Overview.md` | レイヤ構成・依存方向・責務の全体像 |
| `docs/03_Architecture/Application_Service_Spec.md` | UseCase一覧 / UI→App→Domain 境界 / トランザクション境界 |
| `docs/03_Architecture/Aggregate_Boundary_Spec.md` | 集約境界・不変条件の保証範囲・ID生成責務 |
| `docs/03_Architecture/Threading_Model_Spec.md` | WinForms UIスレッド/Worker/Cancel/Invoke ルール |
| `docs/03_Architecture/Error_Handling_Policy.md` | 例外分類（Domain/App/UI）と表示・復旧・ログ方針 |
| `docs/03_Architecture/Logging_Spec.md` | ログレベル/保存先/ローテーション/フォーマット |
| `docs/05_UI/UI_Flow.md` | 画面遷移（起動→編集→再生→結果） |
| `docs/06_Playback/Playback_StateMachine.md` | 再生状態（Idle/Playing/Paused/...）の遷移図と許容操作 |
| `docs/06_Playback/InputSimulation_Spec.md` | SendInput/座標系/DPI/ClickType等の前提 |
| `docs/06_Playback/ScreenCapture_Spec.md` | Capture/OCR/色比較/検索領域の前提と制約 |
| `docs/08_Test/Test_Cases_Domain.md` | 不変条件→テスト観点の対応表 |
| `docs/08_Test/Test_Data.md` | テスト用 macro/json/csv の置き場と内容 |
| `docs/09_Release/Git_Branch_Tag_Rule.md` | feature→master、タグ、Release作成手順 |
| `docs/09_Release/Changelog_Format.md` | Release Notes書式（固定テンプレ） |

## 4. 最小スケルトン（コピペ用）
### 4.1 Application_Service_Spec.md（最優先）
```md
# Application Service 仕様

## 目的
- UIからDomainへの呼び出し境界を固定し、DDD/TDDを破綻させない。

## 用語
- （必要なら01_Glossaryへの参照）

## ユースケース一覧
- UC-01 新規マクロ作成
- UC-02 マクロを開く
- UC-03 マクロを保存
- UC-04 記録開始/停止
- UC-05 再生（通常/選択まで）
- UC-06 Import（CSV）
- UC-07 Export（CSV）
- （現行実装範囲のみ）

## 各ユースケース詳細（テンプレ）
### UC-XX: <名前>
- 入力:
- 前提条件:
- 事後条件:
- 例外/エラー:
- ドメイン呼び出し:
- 永続化:
- UI反映:
```
### 4.2 Threading_Model_Spec.md（最優先）
```md
# スレッドモデル仕様（WinForms）

## 基本方針
- UIスレッド: 画面更新のみ
- Worker: 再生/キャプチャ/OCR/入力送出など重い処理
- Cancel: CancellationTokenを全経路で伝播

## 禁止事項
- UIスレッドでの長時間処理（X ms以上）
- Workerから直接Control操作（必ずInvoke/BeginInvoke）

## 再生中のUI更新
- 実行中ステップの自動選択
- 進捗表示
- 停止/一時停止の受付
```
## 5. git mv 例（手順の雛形）
```bash
# 例：Requirements配下を作成
mkdir -p src/MacroTool.Docs/docs/02_Requirements
mkdir -p src/MacroTool.Docs/docs/03_Architecture
mkdir -p src/MacroTool.Docs/docs/05_UI/_sources
mkdir -p src/MacroTool.Docs/docs/06_Playback
mkdir -p src/MacroTool.Docs/docs/07_FileFormats/Migrations
mkdir -p src/MacroTool.Docs/docs/09_Release
mkdir -p src/MacroTool.Docs/docs/images/_archive

# 移動（例）
git mv src/MacroTool.Docs/docs/01_ProductSpec.md src/MacroTool.Docs/docs/02_Requirements/Product_Spec.md
git mv src/MacroTool.Docs/docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md src/MacroTool.Docs/docs/02_Requirements/Functional_Spec.md
git mv src/MacroTool.Docs/docs/03_UI/UI_Spec.md src/MacroTool.Docs/docs/05_UI/UI_Spec.md
git mv src/MacroTool.Docs/docs/06_Persistence/CSV_Import_Spec_v1.0.0.md src/MacroTool.Docs/docs/07_FileFormats/CSV_Import_Spec.md
# ...（移動表に従って続ける）
```

## 6. リンク修正の注意
- 画像は `docs/images/` を維持しているため、**多くは修正不要**です。
- ただし `UI_Spec.md` や `00_Index.md` 内で `../03_UI/` など章番号参照がある場合は、移動後に相対パスを更新してください。
