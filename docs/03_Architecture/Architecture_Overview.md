# アーキテクチャ概要（MacroTool）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 対象: MacroTool（WinForms / C# / DDD + TDD）
- 参照: `docs/03_Architecture/Application_Service_Spec.md` / `docs/04_Domain/Domain_Model.md` / `docs/06_Playback/Playback_Spec.md`

---

## 1. 目的

本書は、MacroTool の全体構成、責務分離、主要データフローを俯瞰で定義する。  
個別のユースケース仕様、ドメイン不変条件、再生状態遷移の詳細は関連仕様書へ委譲する。

---

## 2. 全体構成

MacroTool は以下の4層で構成する。

- **UI層**: WinForms 画面、ダイアログ、一覧表示、入力収集
- **Application層**: UseCase 実行調停、DTO変換、永続化呼び出し、再生開始/停止制御
- **Domain層**: Macro / Step / Action と不変条件、状態遷移ルール
- **Infrastructure層**: JSON/CSV入出力、SendInput、画面取得、OCR/画像検出実装、時刻・タイマー

---

## 3. 依存方向

依存方向は次に固定する。

- `UI -> Application -> Domain`
- `Application -> Infrastructure(interface)`
- `Infrastructure -> OS / Library`
- `Domain` は UI / Infrastructure に依存しない

### 3.1 禁止事項
- UI が Domain オブジェクトを直接永続化しない
- Domain がファイルI/OやWinForms APIを呼ばない
- Infrastructure から UI を更新しない

---

## 4. 主要コンポーネント

### 4.1 UI
- MainForm
- Action編集ダイアログ群
- Settingsダイアログ
- Schedule macro ダイアログ

責務:
- ユーザー入力の取得
- DTOの表示
- 確認ダイアログの表示
- UseCase呼び出し

### 4.2 Application
- `MacroEditorAppService`
- `PlaybackAppService`
- `SettingsAppService`

責務:
- UseCase単位の処理順制御
- 例外分類
- DTO組み立て
- リポジトリ/サービス呼び出し

### 4.3 Domain
- `Macro`
- `MacroStep`
- `MacroAction` 派生群
- 値オブジェクト（`StepLabel`, `GoToTarget`, `SearchArea`, `Rect`, `Milliseconds` など）

責務:
- 整合性検証
- ラベル一意制約
- Action間参照の妥当性

### 4.4 Infrastructure
- `MacroFileRepository`（JSON）
- `MacroCsvRepository` / CSV mapper
- `InputSimulator`
- `ScreenCaptureService`
- `OcrService`
- `ImageFinderService`
- `SchedulerAdapter`
- `Clock` / `Timer` アダプタ

---

## 5. 代表フロー

### 5.1 Open / Save
1. UI が Application Service を呼ぶ
2. Application が Repository を利用する
3. Repository が JSON を読み書きする
4. 成功時のみ Application が Recent Files を更新する
5. Application が DTO を返す
6. UI が画面更新する

### 5.2 Export / Import CSV
1. UI が Import / Export を要求する
2. Application が CSV mapper を利用する
3. Domain 整合性を検証する
4. 成功時に DTO へ反映する

### 5.3 Playback
1. UI が再生開始を要求する
2. Application が事前バリデーションを行う
3. PlaybackEngine が Step を順次評価する
4. 入力送信 / 画面取得 / OCR / 画像検索は Infrastructure を経由する
5. 進捗は Application 経由で UI へ通知する
6. 終了理由を確定して Idle に戻る

---

## 6. 再生関連の分離

### 6.1 PlaybackEngine
責務:
- Step反復
- GoTo / Repeat / If の制御
- 停止要求の監視
- 実行中Step通知

### 6.2 InputSimulation
責務:
- MouseClick / MouseMove / MouseWheel / KeyPress の送信
- OS差異の吸収

### 6.3 ScreenCapture
責務:
- SearchArea に応じた対象領域取得
- OCR / 画像検出へ渡す画像生成

---

## 7. 永続化方針

- 内部保存形式は JSON
- 交換形式は CSV
- Hotkey は UI入力機能であり、保存時は `KeyPress` 群へ展開する
- 本版対象外Actionは永続化形式に含めない
- アプリ設定は **ユーザープロファイル配下のローカル設定ファイル** に保存する
- アプリ設定の保存単位は **アプリ全体で1つ** とし、Macro JSON / CSV には含めない
- Settings ダイアログの `OK` で設定ファイルへ保存し、`Cancel` では未保存変更を破棄する
- `Reset settings` はダイアログ上の全項目をデフォルト値へ戻し、保存反映は `OK` 押下時のみ行う

---

## 8. 設計原則

- ドメインルールは Domain に閉じ込める
- 画面都合の情報は DTO / ViewModel 側で扱う
- 時刻、タイマー、入力送信、画面取得は抽象化する
- 例外は分類し、ユーザー表示と内部原因を分離する

---

## 9. 未確定事項

- Scheduler 実装方式（常駐監視 / OSスケジューラ補助）
- OCR / 画像検出ライブラリの最終選定
- マルチモニタ・DPI差異吸収の実装詳細

