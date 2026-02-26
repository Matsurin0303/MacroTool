# UI仕様書（WinForms）

- プロダクト：MacroTool
- 仕様書バージョン：Macro_vX.Y.Z（リリースタグ `Macro_vX.Y.Z` と一致させる）
- 最終更新日：YYYY-MM-DD
- 対象：WinForms UI（見た目＋操作＋入力制約）
- 前提：ドメイン（DDD）で不変条件を担保し、UIは入力支援とバリデーションを担う

---

## 0. 重要注意（番号の扱い）
`docs/images` のファイル名先頭の番号（例：`1-2_Open.png`）は **UI画像側の整理番号**であり、  
機能仕様書（開発ID）と **一致しない可能性**がある。

運用上は、必要に応じて機能仕様書側に「UI画像ID（ファイル名）」列を追加して紐づける。

---

## 1. 参照（正：画像 / 補助：論理仕様）

### 1.1 UI画像（見た目の正）
- 画像フォルダ（正）：`docs/images`
  - **運用方針**：画像は原則移動しない。整理する場合は `docs/images/**` のサブフォルダで分割する。

### 1.2 論理仕様（入力項目・動作の根拠）
- 機能仕様（状態付き）：`docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.md`
- ドメイン仕様：`docs/04_Domain/Domain_Model.md`
- 再生仕様：`docs/05_Playback/Playback_Spec.md`

---

## 2. 画面一覧と画像対応表（ファイル名確定版）

> 「対象」= 本版で実装対象  
> 「対象外（将来予定）」= UIにあっても本版では処理しない（クリック等で未実装動作にしない方針）

### 2.1 全体
| 画面/領域 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| 全体イメージ | docs/images/0_All.png | 画面全体の構成・メニュー体系 | 対象 |

### 2.2 File メニュー
| 画面/ダイアログ | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| File メニュー全体 | docs/images/1_File.png | File配下の項目一覧 | 対象 |
| New 確認 | docs/images/1-1-1_Confirmation.png | 新規作成の確認ダイアログ | 対象 |
| Open | docs/images/1-2_Open.png | ファイル選択/オープン | 対象 |
| Open 確認 | docs/images/1-2-1_Confirmation.png | オープン前確認 | 対象 |
| Recent（※画像名Resent）一覧 | docs/images/1-3_ResentFileList.png | 最近使ったファイル一覧 | 対象 |
| Recent 確認 | docs/images/1-3-1_Confirmation.png | Recent選択時の確認 | 対象 |
| Schedule macro | docs/images/1-8_ScheduleMacro.png | スケジュール設定画面 | 対象外（将来予定）※運用方針に従う |
| Settings - Recording | docs/images/1-9-1_Recording.png | 設定（記録） | 対象/対象外は機能仕様の状態で決定 |
| Settings - Playback | docs/images/1-9-2_Playback.png | 設定（再生） | 対象/対象外は機能仕様の状態で決定 |
| Settings - Hotkeys | docs/images/1-9-3_Hotkeys.png | 設定（ホットキー） | 対象/対象外は機能仕様の状態で決定 |
| Settings - User Interface | docs/images/1-9-4_UserInterface.png | 設定（UI） | 対象/対象外は機能仕様の状態で決定 |
| Settings - Network | docs/images/1-9-5_Network.png | 設定（ネットワーク） | 対象外（将来予定）※一般に影響が大きい |
| Settings - AI | docs/images/1-9-6_AI.png | 設定（AI） | 対象外（将来予定） |

※Settingsは「UIだけ先に存在している」可能性があるため、最終的な対象/対象外は機能仕様（状態列）で確定する。

### 2.3 Record & Edit メニュー（Action編集）
| 画面/カテゴリ | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Record & Edit メニュー全体 | docs/images/2_RecordAndEdit.png | Record/Edit配下の項目一覧 | 対象 |
| Play | docs/images/2-1Play.png | 再生系操作（Play等） | 対象 |

#### 2.3.1 Mouse
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Mouseカテゴリ | docs/images/2-4_Mouse.png | Mouse系Actionのカテゴリ画面 | 対象 |
| Click | docs/images/2-4-1_Click.png | Mouse Click の入力UI | 対象 |
| Move | docs/images/2-4-2_Move.png | Mouse Move の入力UI | 対象 |
| Wheel | docs/images/2-4-3_Wheel.png | Mouse Wheel の入力UI | 対象 |

#### 2.3.2 Text / Key
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Text/Keyカテゴリ | docs/images/2-5_TextKey.png | キーボード・テキスト系 | 対象 |
| KeyPress | docs/images/2-5-1_KeyPress.png | KeyPress の入力UI | 対象 |
| Text | docs/images/2-5-2_Text.png | テキスト入力（タイプ）UI | 対象 |

#### 2.3.3 Wait
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Waitカテゴリ | docs/images/2-6_Wait.png | Wait系カテゴリ | 対象 |
| Wait | docs/images/2-6-1_Wait.png | 固定時間待機 | 対象 |
| WaitForPixelColor | docs/images/2-6-2_WaitForPixelColor.png | ピクセル色待ち | 対象 |
| WaitForScreenChange | docs/images/2-6-3_WaitForScreenChange.png | 画面変化待ち | 対象 |
| WaitForHotkey | docs/images/2-6-4_WaitForHotkey.png | ホットキー待ち | 対象外（将来予定） |
| WaitForTextInput | docs/images/2-6-5_WaitForTextInput.png | テキスト入力待ち | 対象外（将来予定） |
| WaitForFileChange | docs/images/2-6-6_WaitForFileChange.png | ファイル変更待ち | 対象外（将来予定） |

#### 2.3.4 Image / OCR
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Image/OCRカテゴリ | docs/images/2-7_ImageOCR.png | 画像検出・OCR系カテゴリ | 対象 |
| FindImage | docs/images/2-7-1_FindImage.png | 画像検索（テンプレマッチ等） | 対象 |
| FindText | docs/images/2-7-2_FindText.png | OCR検索 | 対象 |
| CaptureText | docs/images/2-7-3_CaptureText.png | OCR抽出（保存） | 対象外（将来予定） |
| CaptureImage | docs/images/2-7-4_CaptureImage.png | 画像キャプチャ（保存） | 対象外（将来予定）※スクショ系 |

#### 2.3.5 Misc
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Miscカテゴリ | docs/images/2-8_Misc.png | そのほか（制御・通知等） | 対象 |
| Repeat | docs/images/2-8-1_Repeat.png | 繰り返し | 対象 |
| GoTo | docs/images/2-8-2_GoTo.png | GoTo（Start/Next/End/Label…） | 対象 |
| Condition | docs/images/2-8-3_Condition.png | 条件分岐（If等） | 対象 |
| EnableMacroFile | docs/images/2-8-4_EnableMacroFile.png | マクロファイル有効化 | 対象外（将来予定） |
| ExecuteProgram | docs/images/2-8-5_ExecuteProgram.png | 外部プログラム実行 | 対象外（将来予定） |
| WindowFocus | docs/images/2-8-6_WindowFocus.png | ウィンドウフォーカス | 対象外（将来予定） |
| ShowNotification | docs/images/2-8-7_ShowNotification.png | 通知表示 | 対象外（将来予定） |
| ShowMessageBox | docs/images/2-8-8_ShowMessageBox.png | メッセージボックス | 対象外（将来予定） |
| SetVariavle（※画像名の綴り） | docs/images/2-8-10_SetVariavle.png | 変数設定 | 対象外（将来予定） |
| SetVariableFromDataList | docs/images/2-8-11_SetVariableFromDataList.png | データリストから変数設定 | 対象外（将来予定） |
| SaveVariable | docs/images/2-8-12_SaveVariable.png | 変数保存 | 対象外（将来予定） |
| Calculate | docs/images/2-8-13_Calculate.png | 計算 | 対象外（将来予定） |
| ExtractFromWebSite | docs/images/2-8-14_ExtractFromWebSite.png | Web抽出 | 対象外（将来予定） |

### 2.4 Playback / View
| 画面 | 画像ファイル | 内容 | 本版対象 |
|---|---|---|---|
| Playback（再生画面/領域） | docs/images/3_PlayBack.png | 再生中の表示・操作 | 対象 |
| View（表示） | docs/images/4_View.png | 表示メニュー/表示切替 | 対象/対象外は機能仕様の状態で決定 |

---

## 3. 共通UIルール（抜粋：最低限）

### 3.1 UI状態（Enabled/Disabled）
- Idle / Recording / Playing の3状態を前提に、編集UIの有効/無効を制御する
- Playing中：編集（追加/削除/並べ替え/プロパティ変更）は無効
- Playing中：実行中ステップは一覧で自動選択状態に追従（ユーザー要望）
- Recording中：再生操作は無効、Stopのみ有効

### 3.2 入力バリデーション（共通）
- 数値は整数（範囲は各Actionで規定）
- `#RRGGBB` 形式の色入力（PixelColor等）
- 文字列は前後空白除去（Label/Variable名など）

### 3.3 GoTo（ターゲット選択）
- Target候補は「Start / Next / End / Label(ユーザー定義の全件)」
- Labelは一意
  - 重複時は末尾に連番付与（例：Jump先, Jump先1, Jump先2）
  - 末尾が数字の場合はさらにインクリメント（Jump先2 が存在→Jump先3）

---

## 4. 対象外（将来実装予定：本版では処理しない）
以下は UI画像が存在しても **本版では処理しない**（必要なら「対象外（将来予定）」表示を行う）。
- WaitForHotkey / WaitForTextInput / WaitForFileChange
- CaptureText / CaptureImage
- WindowFocus / ShowNotification / ShowMessageBox
- 変数操作群（SetVariable / SaveVariable / Calculate / DataList 等）
- Web抽出（ExtractFromWebSite）
- Network / AI 系の設定（導入範囲が大きいため）

---

## 5. 変更履歴（このファイル）
| 日付 | 版 | 変更内容 | 変更者 |
|---|---|---|---|
| YYYY-MM-DD | Macro_vX.Y.Z | 画像一覧の実ファイル名で対応表を確定 |  |