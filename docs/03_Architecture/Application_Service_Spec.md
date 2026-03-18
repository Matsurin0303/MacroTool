# Application Service 仕様（MacroTool）

## 0. 目的
本書は、WinForms UI から Domain を直接操作せず、**Application Service（UseCase）経由で一貫して操作する**ための仕様である。  
DDD の依存方向を固定し、TDD で検証可能な単位（UseCase）を明確化する。

### ゴール
- UIイベント（メニュー/ボタン） → UseCase → Domain → Persistence の経路を固定する
- Domain不変条件違反、I/O例外などの扱いを統一する
- “再生中のUI追従（実行中ステップの自動選択）”などの責務分離を明確にする

---

## 1. 適用範囲
- 対象：**現行版で実装・仕様対象となる機能**
- 対象外：将来実装予定機能は本文仕様に含めない（必要なら別途「対象外」注記のみ）

---

## 2. レイヤ責務（必須）
### 2.1 UI（WinForms）
- 役割：ユーザー入力の収集／表示更新／ダイアログ表示
- 禁止：Domain への直接操作（集約をUIで組み立てて勝手に保存しない）
- 原則：UIは **Application Service を呼ぶ**、戻り値（ViewModel/DTO）で描画する

### 2.2 Application（UseCase / Application Service）
- 役割：ユースケース実行の調停
  - 永続化（ロード/セーブ）
  - トランザクション境界（※ファイルI/Oは1UseCase内で完結）
  - Domain呼び出し順序制御
  - 例外分類（Domain例外はユーザー向けエラーへ変換）
  - 再生中の進捗通知（イベント/コールバック）
- 禁止：UI依存（MessageBox等は呼ばない）

### 2.3 Domain
- 役割：不変条件・状態遷移・業務ルール
- 禁止：I/O（ファイル・UI・OCR実体など）への依存

### 2.4 Infrastructure（Persistence / OS）
- 役割：ファイル読み書き、キャプチャ、SendInput 等の実体
- Applicationはインターフェース越しに利用する

---

## 3. 依存関係（固定）
- UI → Application → Domain
- Application → Infrastructure（interface）
- Domain はどこにも依存しない（例外：共通ライブラリ）

---

## 4. 用語（最低限）
- Macro：マクロ集約（Aggregate Root）
- Step：マクロの手順（Entity）
- Action：Stepが持つ具体的操作（値/派生型）
- Label：Stepに付与される識別名（**一意制約**あり）
- Playback：Macroを順に実行すること
- Selection：UI上で選択中のStep

※詳細は `docs/01_Glossary.md` に集約して参照する（推奨）

---

## 5. アプリケーション境界（I/F設計）
### 5.1 Application Service 一覧（推奨）
- `MacroEditorAppService`：編集系（New/Open/Save/Step操作/Import/Export）
- `PlaybackAppService`：再生系（Play/Stop/Pause/PlayUntilSelected 等、現行対象のみ）
- `SettingsAppService`：設定系（現行対象のみ）

※実装形は1クラスでも良いが、**UseCase単位が分かれる**よう命名する。

### 5.2 DTO / ViewModel 方針
- UIに返すのは `MacroDocumentDto`（Macro全体＋UI表示に必要な付加情報）
- DomainをそのままUIに渡さない（テスト容易性・依存抑止）

---

## 6. エラー分類（UseCase共通仕様）
UseCaseは例外をそのままUIに投げない。以下の分類で戻す（Resultパターン推奨）。

### 6.1 DomainError（ユーザー修正可能）
- Labelが一意でない
- SearchAreaが不正、など仕様上の入力不備

### 6.2 ApplicationError（再試行/運用で回避）
- ファイルが存在しない
- JSON/CSVの形式不正
- 互換変換に失敗、など

### 6.3 SystemError（致命）
- 想定外例外
- 依存コンポーネント障害

---

## 7. ユースケース一覧（現行対象）
> ※名称はUIのボタン/メニューに寄せる  
> ※「将来予定」はここに入れない

### 編集・ファイル
- UC-01 New（新規マクロ作成）
- UC-02 Open（マクロを開く）
- UC-03 Save（保存）
- UC-04 Save As（名前を付けて保存）
- UC-05 Import from CSV（CSV Import）
- UC-06 Export to CSV（CSV Export）

### 編集操作（Step）
- UC-11 Add Step（ステップ追加）
- UC-12 Delete Step（ステップ削除）
- UC-13 Move Step（上下移動）
- UC-14 Update Step（内容更新：Action/パラメータ）
- UC-15 Set Label（ラベル設定：一意保証）

### 再生
- UC-21 Play（再生）
- UC-22 Play until selected（選択ステップまで再生）※現行対象の場合のみ
- UC-23 Stop（停止）
- UC-24 Pause/Resume（一時停止/再開）※現行対象の場合のみ
- UC-25 Playback progress notify（実行中ステップ通知）

---

## 8. UseCase 詳細仕様（テンプレ + MacroTool向けに具体化）

以下のテンプレで全UseCaseを記述する。  
（最低限、**UC-01/02/03/05/06/21/25** は必ず詳細化する）

---

# UC-01 New（新規マクロ作成）

## 目的
空のMacroドキュメントを生成し、UI編集を開始可能にする。

## 入力
- なし（UI側で「未保存の変更がある場合の確認」はUI責務）

## 出力
- `MacroDocumentDto`（空Macro + 初期表示用情報）

## 前提条件
- なし

## 事後条件
- Macroが編集可能状態になる
- Dirtyフラグは false（作成直後）

## 処理手順
1. Domainで空Macro生成（初期Stepの有無は仕様に従う）
2. `MacroDocumentDto` を組み立てて返却

## エラー
- 原則なし（例外はSystemError）

---

# UC-02 Open（マクロを開く）

## 入力
- `path`

## 出力
- `MacroDocumentDto`

## 前提条件
- `path` が存在する

## 事後条件
- MacroDocumentがロードされ、編集可能状態になる
- Dirtyフラグは false

## 処理手順
1. Persistenceでファイル読み込み（Macro JSON）
2. バージョン判定 → 必要なら変換（互換ポリシーに従う）
3. Domainへ復元（不変条件チェック）
4. DTO返却

## エラー
- ファイル無し → ApplicationError
- 形式不正/パース失敗 → ApplicationError
- 不変条件違反 → DomainError

---

# UC-03 Save（保存）

## 入力
- `documentId` または `currentPath`
- `MacroDocumentDto`（または編集差分）

## 出力
- `SaveResultDto`（保存先、成功/失敗、Dirty=false など）

## 前提条件
- 保存先が決定済み（未決定なら UC-04 Save As を使う）

## 事後条件
- ファイルが更新される
- Dirty=false

## 処理手順
1. DTO→Domainへ反映（またはDomainを保持しているなら不要）
2. Domain不変条件検証
3. Persistenceへ書き込み（原子性：一時ファイル→置換推奨）
4. 結果返却

## エラー
- 不変条件違反 → DomainError
- I/O失敗 → ApplicationError

---

# UC-04 Save As（名前を付けて保存）
（UC-03 と同様。保存先指定が必須である点だけが差分）

---

# UC-05 Import from CSV（CSV Import）

## 入力
- `csvPath`
- `target`（新規Macroとして作る / 現Macroへ取り込む）※仕様に合わせる

## 出力
- `MacroDocumentDto`

## 前提条件
- CSV列（ヘッダ）仕様に一致（`CSV_Import_Spec.md`）

## 事後条件
- Import結果のMacroDocumentが編集可能状態になる

## 処理手順
1. CSV読み込み
2. バージョン判定（必要なら v1→v2 自動変換。互換ポリシーに従う）
3. 行→Step(Action)へ変換
4. Domain不変条件（Label一意等）を満たすよう補正（仕様に従う）
5. DTO返却

## エラー
- 列不足/型不正 → ApplicationError
- 補正不可能な不整合 → DomainError

---

# UC-06 Export to CSV（CSV Export）

## 入力
- `macroId` または `MacroDocument`
- `csvPath`

## 出力
- `ExportResultDto`

## 前提条件
- Macroが有効（Domain不変条件を満たす）

## 事後条件
- CSV出力が作成される

## 処理手順
1. Domain検証
2. CSV行へ変換（列仕様に従う）
3. 書き込み（原子性推奨）
4. 結果返却

---

# UC-15 Set Label（ラベル設定：一意保証）

## 入力
- `stepId`
- `requestedLabel`（ユーザー入力）

## 出力
- `MacroDocumentDto`（更新後）

## ルール（Domainと一致させる）
- LabelはMacro内で一意
- 重複した場合、末尾に数字を付与して一意化する
  - 例：`Jump先` → `Jump先1` → `Jump先2`
- 末尾に数字がある場合は、その次の番号へ

## 処理手順
1. Domainに「一意化を伴うラベル設定」を委譲（Domain責務）
2. 更新後DTO返却

---

# UC-21 Play（再生）

## 入力
- `macroId`
- `startPosition`（Start / Next / Label… のいずれか。現行仕様に従う）
- `options`（再生オプション：現行対象のみ）

## 出力
- `PlaybackSessionId`（または Result）

## 前提条件
- Macroが有効
- 既に再生中でない

## 事後条件
- Playbackセッション開始
- 実行中ステップ通知が発火する（UC-25）

## 処理手順
1. Domain検証
2. PlaybackEngine起動（バックグラウンド）
3. 進捗通知ハンドラ登録（UC-25）

## エラー
- 不変条件違反 → DomainError
- 実行環境起因（キャプチャ不可等） → ApplicationError

---

# UC-25 Playback progress notify（実行中ステップ通知）

## 目的
再生中、UIが「現在実行中のStep」を自動選択状態にするための通知仕様。

## 通知内容（例）
- stepId
- stepIndex
- stepSummary（UI表示用短文）
- timestamp
- status（Running / Succeeded / Failed）

## UI側の必須動作
- 通知を受けたら該当ステップを選択状態にする
- 失敗時は結果を表示（表示方法はUI仕様に従う）
- ただしUI更新は必ずUIスレッドで行う（Threading Model 参照）

---

## 9. トランザクション境界（ファイルI/O）
- Open / Save / Import / Export は **1UseCase内でI/O完結**
- 書き込みは原子性を推奨（tmp→replace）
- 再生はセッション単位（開始〜終了）をUseCase境界とする

---

## 10. 参照仕様
- 仕様書入口：`docs/00_Index.md`
- UI仕様：`docs/05_UI/UI_Spec.md`
- Domain：`docs/04_Domain/Domain_Model.md`
- Playback：`docs/06_Playback/Playback_Spec.md`
- FileFormats：`docs/07_FileFormats/*`
- 互換ポリシー：`docs/02_Requirements/Compatibility_Policy.md`
- スレッドモデル：`docs/03_Architecture/Threading_Model_Spec.md`