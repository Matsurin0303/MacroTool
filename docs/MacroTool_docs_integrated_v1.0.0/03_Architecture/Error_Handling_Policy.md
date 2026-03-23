# エラーハンドリング方針（MacroTool）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 参照: `docs/03_Architecture/Application_Service_Spec.md` / `docs/06_Playback/Playback_Spec.md`

---

## 1. 目的

本書は、MacroTool における例外・失敗・検証エラーの扱いを統一するための方針を定義する。

---

## 2. 基本原則

- 例外は握り潰さない。
- UI は原則として Application の結果を表示し、例外の詳細を直接解釈しない。
- 失敗はユーザーが修正可能かどうかで分類する。
- ログには原因調査に必要な情報を残す。
- ユーザー向け文言と内部例外情報は分離する。

---

## 3. エラー分類

### 3.1 DomainError
仕様上の不正入力、整合性違反、事前条件違反。

例:
- Label 重複
- GoTo先 Label 不存在
- SearchArea の矩形不正
- WaitingMs が範囲外
- Action 必須項目の不足

### 3.2 ApplicationError
入出力、形式変換、運用上の失敗。

例:
- ファイルが存在しない
- JSON / CSV 形式不正
- バージョン不整合
- Import データがDTOへ変換できない
- OCR / 画像検出の実行失敗

### 3.3 SystemError
想定外例外、実行基盤障害、回復不能に近い失敗。

例:
- 予期しない NullReference
- 外部ライブラリの異常終了
- OS API 呼び出し失敗

---

## 4. レイヤ別方針

### 4.1 UI層
- MessageBox 等の表示責務のみ持つ。
- 例外クラスの詳細分岐をUI側で持たない。
- エラー表示後の再操作可能性を損なわない。

### 4.2 Application層
- Domain例外を受けて `DomainError` として返す。
- I/O失敗や形式不正を `ApplicationError` として返す。
- 想定外例外は `SystemError` として扱い、ログ出力対象にする。

### 4.3 Domain層
- 不変条件違反を明示的に通知する。
- UI文言を持たない。
- ファイルI/Oや画面表示は行わない。

### 4.4 Infrastructure層
- OS / ライブラリ例外を必要に応じてラップして Application に返す。
- 元例外を失わないよう内部情報を保持する。

---

## 5. 代表ユースケースごとの扱い

### 5.1 New / Open / Recent Files
- 未保存確認はエラーではなく分岐として扱う。
- 未保存確認ダイアログのタイトルは **`未保存の変更`**、本文は **`変更内容は保存されていません。保存しますか？`**、ボタンは **`保存` / `保存しない` / `キャンセル`** とする。
- 読込失敗、存在しないファイル、形式不正は ApplicationError。
- `Recent Files` から存在しないファイルを選択した場合は、ApplicationError をUIへ通知し、同一トランザクション内で該当MRU項目を削除する。
- 読込に失敗した場合は Recent Files の並び順を更新しない。

### 5.2 Save / Save As
- 保存先不正、書込失敗、権限不足は ApplicationError。
- 永続化前の整合性違反は DomainError。
- 保存成功時のみ保存先ファイルを Recent Files の先頭へ反映する。保存失敗時は Recent Files を更新しない。

### 5.3 Import / Export CSV
- ヘッダ不足、列挙値不正、型変換失敗は ApplicationError。
- DTO変換後に不変条件へ違反した場合は DomainError。

### 5.4 Playback
- 再生開始前の検証失敗は `ValidationRejected` として扱う。
- Step単位の制御失敗は `StepFailed`、継続不能な異常は `StepErrored` として扱う。
- 停止要求はエラーではなく `Cancelled` / `Aborted` の終了理由とする。
- `FindImage` / `FindTextOcr` / `WaitForTextInput` の `WaitingMs` 経過による未検出 / 未成立は、エラーではなく `FalseGoTo` 側への通常分岐として扱う。
- OCR 実行失敗、画像読込失敗、画像検出サービス失敗など、検出処理そのものが実行できない場合は `ApplicationError` もしくは `StepErrored` として扱う。

---

## 6. UI表示方針

### 6.1 表示内容
ユーザー表示は最低限、次を含む。
- 何に失敗したか
- 何を見直すべきか
- 続行可能か

### 6.2 表示しない内容
- スタックトレース全文
- 内部クラス名や実装依存の詳細
- 解析にしか使わない内部識別子

---

## 7. ログ方針

ログには必要に応じて次を記録できること。
- 発生時刻
- UseCase名
- 対象ファイル
- 対象Step番号
- Action種別
- 終了理由
- 内部例外情報

ログ出力先、フォーマット、保存期間は本版未確定とする。

---

## 8. 再スロー方針

- UI境界を超えて素の例外を上げない。
- ただしアプリ終了判断が必要な致命障害は最上位で捕捉する。
- `OperationCanceledException` 相当は停止系として正規処理に変換する。

---

## 9. 未確定事項

- ユーザー向けエラーコード採番の有無
- ログレベル定義（Info / Warn / Error など）の最終形
- 例外通知ダイアログの統一UIテンプレート

