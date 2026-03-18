# TKT-005 / TKT-016 修正サマリ

## 対応チケット
- TKT-005: Pause/Resume を本版対象外として確定
- TKT-016: Import from CSV の取り込み先を v7 の意図に合わせて固定

## 修正対象
- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Application_Service_Spec.md`
- `03_Architecture/Threading_Model_Spec.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`

## 主な修正内容
### TKT-005
- `Application_Service_Spec.md` から UC-24 Pause/Resume を削除
- `PlaybackAppService` の説明から Pause を削除
- `Threading_Model_Spec.md` の「停止/一時停止の受付」を「停止の受付」に修正
- 既存の `Playback_Spec.md` / `Playback_StateMachine.md` / `InputSimulation_Spec.md` と整合させた

### TKT-016
- `Functional_Spec.md` の Import from CSV の説明を、現在編集中のMacroへ選択された行位置から追加する表現に修正
- `Application_Service_Spec.md` の UC-05 を、現在編集中のMacroに対する取り込みとして明文化
- `CSV_Import_Spec_v1.0.0.md` に、取り込み先は現在編集中のMacroであり、新規Macro自動作成モードは本版対象外であることを明記
- `Macro_FileFormat_Spec.md` から「Importの追加位置詳細」を未確定事項から削除

## 完了確認
- Pause/Resume を本版対象と読む記述が、主要仕様書から除去されている
- Import from CSV の取り込み先が「現在編集中のMacro」に統一されている
- v7 の説明「選択された行から追加する」と矛盾しない
