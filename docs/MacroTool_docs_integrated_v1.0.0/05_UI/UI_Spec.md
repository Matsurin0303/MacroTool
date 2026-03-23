# UI仕様書（WinForms）

- プロダクト：MacroTool
- 仕様書バージョン：Macro_v1.0.0
- 最終更新日：2026-03-18
- 対象：WinForms UI（見た目 + 操作 + 入力制約）
- 基準資料：`Macro仕様書_v7.xlsx` / `docs/02_Requirements/Functional_Spec.md`

---

## 0. 目的

本書は、`docs/images` 配下のUI画像と、`Macro仕様書_v7.xlsx` に基づく本版対象機能の対応を定義する。  
本書の役割は **画像と本版機能スコープの対応付け** であり、動作詳細のSSOTは `Functional_Spec.md` とする。

---

## 1. 参照

- 画像フォルダ：`docs/images`
- 機能仕様：`docs/02_Requirements/Functional_Spec.md`
- ドメイン仕様：`docs/04_Domain/Domain_Model.md`
- 再生仕様：`docs/06_Playback/Playback_Spec.md`

---

## 2. 画面一覧と本版対象

> 「本版対象」は `Macro仕様書_v7.xlsx` を基準に判定する。  
> 画像が存在しても、v7 に存在しない項目は **参考画像 / 本版対象外** とする。

### 2.1 全体

| 画面/領域 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| 全体イメージ | `docs/images/0_All.png` | 画面全体の構成 | 対象 |

### 2.2 File

| 画面/ダイアログ | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| File メニュー全体 | `docs/images/1_File.png` | File配下の項目一覧 | 対象 |
| New 確認 | `docs/images/1-1-1_Confirmation.png` | 新規作成確認 | 対象 |
| Open | `docs/images/1-2_Open.png` | 既存マクロを開く | 対象 |
| Open 確認 | `docs/images/1-2-1_Confirmation.png` | Open前確認 | 対象 |
| Recent Files 一覧 | `docs/images/1-3_RecentFileList.png` | 最近開いたマクロ一覧 | 対象 |
| Recent Files 確認 | `docs/images/1-3-1_Confirmation.png` | Recent選択時確認 | 対象 |
| Save確認 | `docs/images/1-4-1_Confirmation.png` | Save時確認 | 対象 |
| Schedule macro | `docs/images/1-8_ScheduleMacro.png` | スケジュール設定画面 | 参考画像 / 将来予定 / 本版対象外 |
| Settings - Recording | `docs/images/1-9-1_Recording.png` | 設定（記録） | 対象 |
| Settings - Playback | `docs/images/1-9-2_Playback.png` | 設定（再生） | 対象 |
| Settings - Hotkeys | `docs/images/1-9-3_Hotkeys.png` | 設定（ホットキー） | 対象 |
| Settings - User Interface | `docs/images/1-9-4_UserInterface.png` | 設定（UI） | 対象 |
| Settings - Network | `docs/images/1-9-5_Network.png` | 設定（Network） | 参考画像 / 本版対象外 |
| Settings - AI | `docs/images/1-9-6_AI.png` | 設定（AI） | 参考画像 / 本版対象外 |

### 2.3 Record & Edit

| 画面/カテゴリ | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Record & Edit メニュー全体 | `docs/images/2_RecordAndEdit.png` | Record/Edit配下の項目一覧 | 対象 |
| Play | `docs/images/2-1Play.png` | 再生系操作 | 対象 |

#### 2.3.1 Mouse

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Mouseカテゴリ | `docs/images/2-4_Mouse.png` | Mouse系Actionのカテゴリ画面 | 対象 |
| Click | `docs/images/2-4-1_Click.png` | Mouse Click入力UI | 対象 |
| Move | `docs/images/2-4-2_Move.png` | Mouse Move入力UI | 対象 |
| Wheel | `docs/images/2-4-3_Wheel.png` | Mouse Wheel入力UI | 対象 |

#### 2.3.2 Text/Key

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Text/Keyカテゴリ | `docs/images/2-5_TextKey.png` | キーボード系カテゴリ | 対象 |
| Key press | `docs/images/2-5-1_KeyPress.png` | Key press入力UI | 対象 |
| Hotkey | `docs/images/2-5-2_Hotkey.png` | Hotkey入力UI | 対象 |
| Text | `docs/images/2-5-3_Text.png` | 旧画像。v7 に該当機能なし | 参考画像 / 本版対象外 |

> v7 の `2-5` カテゴリは `Key press` と `Hotkey` を対象とする。独立した `Text` Action は本版に含めない。

#### 2.3.3 Wait

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Waitカテゴリ | `docs/images/2-6_Wait.png` | Wait系カテゴリ | 対象 |
| Wait | `docs/images/2-6-1_Wait.png` | 固定時間待機 | 対象 |
| Wait for pixel color | `docs/images/2-6-2_WaitForPixelColor.png` | ピクセル色待ち | 対象 |
| Wait for screen change | `docs/images/2-6-3_WaitForScreenChange.png` | 画面変化待ち | 参考画像 / 本版対象外 |
| Wait for hotkey | `docs/images/2-6-4_WaitForHotkey.png` | ホットキー待ち | 参考画像 / 本版対象外 |
| Wait for text input | `docs/images/2-6-5_WaitForTextInput.png` | テキスト入力待ち | 対象 |
| Wait for file change | `docs/images/2-6-6_WaitForFileChange.png` | ファイル変更待ち | 参考画像 / 本版対象外 |

#### 2.3.4 Image / OCR

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Image/OCRカテゴリ | `docs/images/2-7_ImageOCR.png` | 画像検出・OCR系カテゴリ | 対象 |
| Find image | `docs/images/2-7-1_FindImage.png` | 画像検出 | 対象 |
| Find text (OCR) | `docs/images/2-7-2_FindText.png` | OCR検索 | 対象 |
| Capture text | `docs/images/2-7-3_CaptureText.png` | OCR抽出 | 参考画像 / 本版対象外 |
| Capture image | `docs/images/2-7-4_CaptureImage.png` | 画像キャプチャ | 参考画像 / 本版対象外 |

#### 2.3.5 Misc

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Miscカテゴリ | `docs/images/2-8_Misc.png` | 制御系カテゴリ | 対象 |
| Repeat | `docs/images/2-8-1_Repeat.png` | 繰り返し | 対象 |
| Go to | `docs/images/2-8-2_GoTo.png` | GoTo | 対象 |
| If | `docs/images/2-8-3_Condition.png` | 条件分岐 | 対象 |
| Embed macro file | `docs/images/2-8-4_EnableMacroFile.png` | マクロファイル埋め込み | 対象 |
| Execute program | `docs/images/2-8-5_ExecuteProgram.png` | 外部プログラム実行 | 対象 |
| Window focus | `docs/images/2-8-6_WindowFocus.png` | ウィンドウフォーカス | 参考画像 / 本版対象外 |
| Show notification | `docs/images/2-8-7_ShowNotification.png` | 通知表示 | 参考画像 / 本版対象外 |
| Show message box | `docs/images/2-8-8_ShowMessageBox.png` | メッセージ表示 | 参考画像 / 本版対象外 |
| Set variable | `docs/images/2-8-10_SetVariavle.png` | 変数設定 | 参考画像 / 本版対象外 |
| Set variable from data list | `docs/images/2-8-11_SetVariableFromDataList.png` | データリストから変数設定 | 参考画像 / 本版対象外 |
| Save variable | `docs/images/2-8-12_SaveVariable.png` | 変数保存 | 参考画像 / 本版対象外 |
| Calculate | `docs/images/2-8-13_Calculate.png` | 計算 | 参考画像 / 本版対象外 |
| Extract from website | `docs/images/2-8-14_ExtractFromWebSite.png` | Web抽出 | 参考画像 / 本版対象外 |

### 2.4 Playback / View

| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Playback | `docs/images/3_PlayBack.png` | 再生画面 / 再生領域 | 対象 |
| View | `docs/images/4_View.png` | 表示関連 | 参考画像（本版範囲は別途定義） |

---

## 3. UI共通ルール

### 3.1 状態
- 公開状態は `Idle` / `Playing` を基本とする。
- `Playing` 中は編集系UIを無効化する。
- `Playing` 中は実行中ステップを一覧上で追従表示する。

### 3.2 GoTo候補
- `Start / End / Next / Label`
- Labelはユーザー定義ラベル一覧から選択する。

### 3.3 Settings 共通ルール
- Settings は **アプリ全体で1つ** の設定として扱い、Macroファイルには保存しない。
- 設定の保存先は **ユーザープロファイル配下のローカル設定ファイル** とする。
- `OK` は設定画面の全項目を保存する。
- `Cancel` はダイアログを開いてからの未保存変更を破棄する。
- `Reset settings` は設定画面の全項目をデフォルト値へ戻す。**保存は `OK` 押下時のみ** 行う。
- Settings 各項目の表示名・説明は、項目名と一致する意味で扱う。コピペ由来の誤記は採用しない。

### 3.4 Recent Files 共通ルール
- 一覧の保持件数は **最大10件** とする。
- `Open` / `Recent Files` の正常読込完了時、および `Save` / `Save As` の成功時に、対象ファイルを一覧の**先頭**へ移動または追加する。
- 重複パスは一覧内に複数残さず、既存項目を削除して先頭へ再追加する。
- `Recent Files` から存在しないファイルを選択した場合はエラーを表示し、その項目を一覧から削除する。

### 3.5 未保存変更確認ダイアログ
- `New` / `Open` / `Recent Files` / `New Record` / `Exit` で未保存変更がある場合、共通の確認ダイアログを表示する。
- タイトルは **`未保存の変更`** とする。
- 本文は **`変更内容は保存されていません。保存しますか？`** とする。
- ボタン構成は **`保存` / `保存しない` / `キャンセル`** とする。
- `保存` は保存成功後に起動元操作を継続する。保存失敗時は起動元操作へ進まない。
- `保存しない` は未保存変更を破棄して起動元操作を継続する。
- `キャンセル` は起動元操作を中止し、現在画面へ留まる。

### 3.6 将来予定として扱う事項
以下は本版では実装しない。参考情報としてのみ扱う。
- Schedule macro

---
以上
