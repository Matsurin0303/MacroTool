# TKT-018 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-018
- 反映方針:
  - 設定の保存先は **ユーザープロファイル配下のローカル設定ファイル**
  - 保存単位は **アプリ全体で1つ**
  - 保存タイミングは **Settings ダイアログの OK 押下時**
  - `Reset settings` は **設定画面の全項目** をデフォルトへ戻し、保存は OK 押下時のみ反映

## 修正ファイル
- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Application_Service_Spec.md`
- `03_Architecture/Architecture_Overview.md`
- `05_UI/UI_Spec.md`

## 主な反映内容
1. `Functional_Spec.md`
   - `Settings` を Macro ファイルとは独立したアプリ全体設定として明記
   - `Reset settings` / `OK` / `Cancel` の挙動を明記
2. `UI_Spec.md`
   - Settings 共通ルールを追加
   - Settings 保存先 / 保存単位を未確定事項から削除
3. `Architecture_Overview.md`
   - 永続化方針にアプリ設定の保存ルールを追加
   - 未確定事項から Settings 永続化先を削除
4. `Application_Service_Spec.md`
   - `SettingsAppService` の責務を具体化
   - `Open Settings` / `Save Settings` / `Reset Settings` / `Cancel Settings` の UseCase を追加

## 完了条件に対する結果
- 設定保存の単位が仕様上で一意に読める: 完了
- 保存先が Macro ファイルではなくローカル設定であることが読める: 完了
- `OK` / `Cancel` / `Reset settings` の責務が明文化されている: 完了
- 文書間で Settings の扱いが矛盾しない: 完了
