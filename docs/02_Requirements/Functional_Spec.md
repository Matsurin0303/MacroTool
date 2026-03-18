# MacroTool マクロ動作機能仕様書
- Version: Macro_v1.0.0  
- 作成日: 2026-02-12  
- 更新日: 2026-03-18  
- 開発環境: Visual Studio 2026  
- 開発言語: C#  
- 設計思想: ドメイン駆動設計（DDD） + テスト駆動開発（TDD）  

> 本書「3. 機能一覧」は、`Macro仕様書_v7.xlsx` の `v1.0` シートを基準に再整理した。  
> `Macro仕様書_v7.xlsx` に存在しない機能は、本版仕様には含めない。  
> UI画像との対応は `docs/05_UI/UI_Spec.md` を参照する。  

---

## 1. 目的・概要

本仕様書は、MacroTool における **マクロの作成・編集・再生** に関する機能一覧を定義する。  
本書は **Macro仕様書_v7.xlsx を文章仕様へ正規化した一覧** とし、個別の入力制約や永続化形式は関連仕様書へ委譲する。

---

## 2. 用語

- **マクロ**: 操作手順（Action）を並べた実行単位  
- **アクション**: マクロを構成する1ステップ  
- **Label**: GoTo / Repeat の参照先となる行識別子  
- **GoTo**: Start / End / Next / Label を遷移先として指定する制御  
- **Variable**: 実行中に利用される値保持領域  

---

## 2.2 名称統一ルール

- 開発IDと機能名は `Macro仕様書_v7.xlsx` の表記をそのまま使用する。
- UI表示名と永続化トークンは区別して扱う。
- CSV / JSON / DTO では `Action` / `WaitingMs` / `MousePosition` を正とし、同義語は使用しない。
- 代表対応は以下のとおり。
  - UI `Click` ↔ Action token `MouseClick`
  - UI `Move` ↔ Action token `MouseMove`
  - UI `Wheel` ↔ Action token `MouseWheel`
  - UI `Key press` ↔ Action token `KeyPress`
  - UI `Find text (OCR)` ↔ Action token `FindTextOcr`

## 2.3 変数仕様

- 変数の実行時型は **文字列** または **数値** とする。
- 未設定状態は **undefined** とする。空文字列や `0` を未設定値として扱わない。
- 変数スコープは **1回のPlayback実行単位** とする。再生終了後の値は保持しない。
- Playback開始時に、全変数は **undefined** へ初期化する。
- 設定 `Reset variables and list counter on each playback cycle` が有効な場合、**Playback Repeat の各 cycle 開始時** に全変数を **undefined** へ初期化する。Repeat Action の内部周回では初期化しない。
- 変数名は英数字と `_` のみを使用でき、先頭数字は禁止とする。正規表現は `^[A-Za-z_][A-Za-z0-9_]*$` とする。
- 変数参照は **大文字小文字を区別しない**。`Count` と `count` は同一変数として扱う。
- `Save Coordinate` で保存する X / Y は **数値** として変数へ格納する。

## 2.4 Recent Files 仕様

- Recent Files の保持件数は **最大10件** とする。
- 次の場合に対象ファイルを Recent Files の**先頭**へ追加または更新する。
  - `Open` により正常に読込完了した場合
  - `Recent Files` から選択したファイルが正常に読込完了した場合
  - `Save` が正常終了した場合
  - `Save As` が正常終了した場合
- 重複パスは **既存項目を削除して先頭へ再追加** する。重複のまま保持しない。
- `Open` / `Recent Files` で存在しないファイルを選択した場合は **エラー表示** し、該当項目を Recent Files から削除する。
- 読込失敗または保存失敗時は、成功していない操作結果で Recent Files を更新しない。

## 2.5 未保存変更確認ダイアログ仕様

- 未保存変更がある状態で `New` / `Open` / `Recent Files` / `New Record` / `Exit` を実行する場合、**共通の未保存変更確認ダイアログ** を表示する。
- ダイアログのタイトルは **`未保存の変更`** とする。
- ダイアログ本文は **`変更内容は保存されていません。保存しますか？`** とする。
- ボタン構成は **`保存` / `保存しない` / `キャンセル`** とする。
- `保存` は現在編集中のMacroを保存成功後、起動元操作を継続する。保存失敗時は起動元操作へ進まない。
- `保存しない` は現在編集中の未保存変更を破棄し、起動元操作を継続する。
- `キャンセル` は起動元操作を中止し、現在画面へ留まる。

## 2.6 Repeat 仕様

- Repeat の繰り返し範囲は **`startLabel` の行から Repeat 行の直前まで** とする。
- `startLabel` が存在しない Repeat を含むマクロは、**保存時および読込時にエラー** とする。
- Repeat の**ネストは本版では禁止** とする。
- `Infinite` は **Stop 操作またはエラー発生まで** 継続する。
- `finishGoTo` は **繰り返し完了後に1回だけ** 適用する。
- Repeat条件は **Seconds / repetitions / Until / Infinite** の4択で排他とし、選択中の条件に対応する入力欄のみ活性化する。
- Playback Repeat は **現在の再生対象全体** を1 cycleとして繰り返す。
- Playback Repeat が有効な場合でも、Macro 内の `Repeat` Action は内部制御としてそのまま実行する。
- Playback Repeat と `Repeat(Infinite)` が重なった場合、**内側の `Repeat(Infinite)` が優先** され、外側の Playback Repeat の次 cycle へは到達しない。

---

## 3. 機能一覧

- 列の意味は `Macro仕様書_v7.xlsx` に準拠する。  
- 本書では `開発ID / 機能名 / 概要 / UI部品 / 起動/操作 / 備考` をSSOTとする。  
- `WaitForScreenChange`、`After playback`、`Playback filter`、`RegEx` は **本版対象外** のため掲載しない。  


### 3.1 File

| 開発ID | 機能名 | 概要 | UI部品 | 起動/操作 | 備考 |
|---|---|---|---|---|---|
| 1 | File | Fileに関する操作欄 | TabPage | Fileに関する操作画面タブ |  |
| 1-1 | New | 新規マクロの作成 | Button | 新規にマクロを作成 |  |
| 1-1-1 | Confirmation | 未保存の変更 | MessageBox | 未保存変更がある状態で `New` を実行すると出現 | タイトル: `未保存の変更` / 本文: `変更内容は保存されていません。保存しますか？` / ボタン: `保存` `保存しない` `キャンセル` |
| 1-1-1-1 | Save | 保存 | Button | 現在編集中のマクロを保存し、新規マクロを作成する | 保存失敗時は新規作成へ進まない。 |
| 1-1-1-2 | Don't Save | 保存しない | Button | 現在編集中の変更を破棄し、新規マクロを作成する |  |
| 1-1-1-3 | Cancel | キャンセル | Button | キャンセル |  |
| 1-2 | Open | 既存マクロを開く | Button | 既存のマクロファイルを開く |  |
| 1-2-1 | Confirmation | 未保存の変更 | MessageBox | 未保存変更がある状態で `Open` を実行すると出現 | タイトル: `未保存の変更` / 本文: `変更内容は保存されていません。保存しますか？` / ボタン: `保存` `保存しない` `キャンセル` |
| 1-2-1-1 | Save | 保存 | Button | 現在編集中のマクロを保存し、既存マクロを開く | 保存失敗時はOpenへ進まない。 |
| 1-2-1-2 | Don't Save | 保存しない | Button | 現在編集中の変更を破棄し、既存マクロを開く |  |
| 1-2-1-3 | Cancel | キャンセル | Button | キャンセル |  |
| 1-3 | Recent Files | 最近開いたマクロを表示 | Button | カーソルを合わせると、最近開いたマクロが表示される。件数は10件まで。Open / Recent Files で正常に読込完了した場合、および Save / Save As 成功時に先頭へ更新する。 | 未保存変更がある場合は共通の未保存変更確認ダイアログを表示する。重複パスは既存項目を削除して先頭へ再追加する。存在しないファイルを選択した場合はエラー表示し、その項目をRecent Filesから削除する。 |
| 1-4 | Save | マクロを保存 | Button | マクロを保存、新規の場合は名前を付けて保存 |  |
| 1-4-1 | Confirmation | 未保存の変更 | MessageBox | 未保存変更に対する保存確認として出現 | タイトル: `未保存の変更` / 本文: `変更内容は保存されていません。保存しますか？` / ボタン: `保存` `保存しない` `キャンセル` |
| 1-4-1-1 | Save | 保存 | Button | 現在編集中のマクロを保存する | 保存失敗時は現在画面に留まる。 |
| 1-4-1-2 | Don't Save | 保存しない | Button | 現在編集中の変更を破棄して継続する |  |
| 1-4-1-3 | Cancel | キャンセル | Button | キャンセル |  |
| 1-5 | Save As… | マクロを名前を付けて保存 | Button | 名前を付けて保存 |  |
| 1-6 | Export to CSV | CSVとして出力 | Button | マクロ動作内容CSVとして出力する |  |
| 1-7 | Import from CSV | CSVから取り込み | Button | CSVの内容を読み取り、現在編集中のマクロへ選択された行位置から追加する |  |
| 1-8 | Schedule macro | マクロの起動をスケジュールする | Button | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-1 | Start date | 開始年月日(yyyy/MM/DD) | DateTimePicker | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-2 | Start time | 開始時刻(HH:mm:ss) | DateTimePicker | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3 | Recur every | 繰り返し間隔（数値＋単位） | NumericUpDown + ComboBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-1 | Execute one | 1回だけ実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-2 | by minutes | 分実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-3 | by hours | 時実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-4 | by days | 日実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-5 | by weeks | 週実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-3-6 | by months | 月実行 | TextBox | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-4 | OK | OKボタン | Button | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-8-5 | Cancel | Cancelボタン | Button | 参考機能。**将来予定 / 本版対象外**。 | 実装しない。 |
| 1-9 | Settings | 設定画面を開く | Button |  | 設定はアプリ全体で1つのローカル設定として扱い、Macroファイルには保存しない。 |
| 1-9-1 | Recording | 記録に関する設定画面 | ListBoxItem |  |  |
| 1-9-1-1 | Record mouse paths | マウスの軌道を記録 | CheckBox |  |  |
| 1-9-1-2 | Minimum mouse movement | マウスの軌道を記録する間隔 | NumericUpDown |  | 最小は10, 最大は9999999、初期値は100, 不正値時は入力キャンセル、単位はms |
| 1-9-1-4 | Ignore multiple modifier key down events | 複数のキーダウンイベントを無効する | CheckBox |  |  |
| 1-9-1-6 | minimum wait time | 最小待機時間 | NumericUpDown | 最小待機時間を超えたものは別イベントとして扱う | 最小は10, 最大は9999999、初期値は100, 不正値時は入力キャンセル、単位はms |
| 1-9-2 | Playback | 再生に関する設定画面 | ListBoxItem |  |  |
| 1-9-2-1 | Block key presses during playback | 再生中のキー入力を無効 | CheckBox |  |  |
| 1-9-2-2 | Abort playback on key press | キー入力で再生を止める | CheckBox |  |  |
| 1-9-2-3 | Abort playback on mouse move | マウス移動で再生を止める | CheckBox |  |  |
| 1-9-2-4 | Restore mouse position after playback | 再生後マウス位置を再生開始時の位置に戻す | CheckBox |  |  |
| 1-9-2-5 | Restore window sizes | 再生後にウィンドウサイズを元に戻す | CheckBox |  |  |
| 1-9-2-6 | Use relative mouse positions | マウス位置を相対位置として扱う | CheckBox |  | 相対位置の厳密な基準座標ルールは本版未確定。 |
| 1-9-2-7 | Reset variables and list counter on each playback cycle | Playback Repeat の各 cycle 開始時に variables / list counter をリセット | CheckBox | Playback Repeat の各 cycle 開始時に variables / list counter をリセットする | Repeat Action の内部周回には適用しない。 |
| 1-9-3 | Hotkeys | ショートカットキー | ListBoxItem |  |  |
| 1-9-3-1 | Start/append recording | 記録を開始、または現在のMacroへ追記記録する | HotkeyTextBox |  |  |
| 1-9-3-2 | Start new recording | 新規にマクロを作成し録画 | HotkeyTextBox |  |  |
| 1-9-3-3 | Stop | 記録または再生を停止 | HotkeyTextBox |  |  |
| 1-9-3-4 | Playback | 現在の再生対象を再生 | HotkeyTextBox |  |  |
| 1-9-3-5 | Play selection | 選択中のマクロ動作を再生 | HotkeyTextBox |  |  |
| 1-9-3-6 | Capture mouse position | マウス位置をキャプチャ | HotkeyTextBox | マクロ動作編集中、マウス位置をキャプチャし、マクロ動作に反映 |  |
| 1-9-4 | User interface | ユーザーインターフェース関連設定 | ListBoxItem |  |  |
| 1-9-4-1 | Show delete confirmation | 削除前に確認ダイアログを表示 | CheckBox |  |  |
| 1-9-4-2 | Hide Macro Recorder when recording | 記録中マクロ記録ツールを隠す | CheckBox |  |  |
| 1-9-4-3 | Hide Macro Recorder on playback | 再生中マクロ記録ツールを隠す | CheckBox |  |  |
| 1-9-7 | Reset settings | デフォルト設定 | Button | 設定画面の全項目をデフォルト値へ戻す | 保存は OK 押下時のみ反映する。 |
| 1-9-8 | OK | OKボタン | Button | 設定の保存 | 設定画面の全項目をユーザープロファイル配下のローカル設定ファイルへ保存する。 |
| 1-9-9 | Cancel | Cancelボタン | Button | 設定のキャンセル | ダイアログを開いてからの未保存変更を破棄する。 |

> 補足: `Settings` 配下の説明文は、項目名と一致する意味に統一した。v7 由来のコピペ疑いがある文言は、本版では項目名優先で修正する。
| 1-10 | Exit | ツールを終了する | Button |  | 未保存変更がある場合は共通の未保存変更確認ダイアログを表示する。 |


### 3.2 Record and Edit

| 開発ID | 機能名 | 概要 | UI部品 | 起動/操作 | 備考 |
|---|---|---|---|---|---|
| 2 | Record and Edit | マクロの記録、編集に関する操作欄 | TabPage |  |  |
| 2-1 | Play | マクロを再生 | ToolStripSplitButton |  |  |
| 2-1-1 | Play until selected | 選択行までマクロを再生 | ToolStripMenuItem |  |  |
| 2-1-2 | Play from selected | 選択行からマクロを再生 | ToolStripMenuItem |  |  |
| 2-1-3 | Play selected | 選択行を再生 | ToolStripMenuItem |  |  |
| 2-2 | Record | マクロを選択行から記録する | ToolStripSplitButton |  |  |
| 2-2-1 | New Record | マクロを新規に記録する | ToolStripMenuItem |  |  |
| 2-2-1-1 | Confirmation | 未保存の変更 | MessageBox | 未保存変更がある状態で `New Record` を実行すると出現 | タイトル: `未保存の変更` / 本文: `変更内容は保存されていません。保存しますか？` / ボタン: `保存` `保存しない` `キャンセル` |
| 2-2-1-1-1 | Save | 保存 | Button | 現在編集中のマクロを保存し、新規記録を開始する | 保存失敗時は新規記録へ進まない。 |
| 2-2-1-1-2 | Don't Save | 保存しない | Button | 現在編集中の変更を破棄し、新規記録を開始する |  |
| 2-2-1-1-3 | Cancel | キャンセル | Button | `New Record` を中止し、現在画面に留まる |  |
| 2-3 | Stop | マクロの再生を停止 | ToolStripButton |  |  |
| 2-4 | Mouse | マウスに関わる操作 | ToolStripDropDownButton |  |  |
| 2-4-1 | Click | クリック動作画面 | Form | マウスのクリックに関わる画面 |  |
| 2-4-1-1 | Mouse button | マウスのボタン | ComboBox | 右、左、真ん中、サイドボタン1、サイドボタン2 |  |
| 2-4-1-2 | Action | 動作 | ComboBox | クリック、ダブルクリック、押す、離す |  |
| 2-4-1-3 | Relative | 相対座標OnOffスイッチ | CheckBox | Onなら現在のマウス座標からの移動、Offなら絶対座標 |  |
| 2-4-1-4 | X | X座標 | ComboBox | マウス位置のX座標 |  |
| 2-4-1-5 | Y | Y座標 | ComboBox | マウス位置のY座標 |  |
| 2-4-1-6 | OK | OKボタン | Button | 動作の追加 |  |
| 2-4-1-7 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-4-2 | Move | 移動動作画面 | Form | マウスの移動に関わる画面 |  |
| 2-4-2-1 | Relative | 相対座標OnOffスイッチ | CheckBox | Onなら現在のマウス座標からの移動、Offなら絶対座標 |  |
| 2-4-2-2 | Start X | 開始X座標 | ComboBox | マウス位置の開始X座標 |  |
| 2-4-2-3 | Start Y | 開始Y座標 | ComboBox | マウス位置の開始Y座標 |  |
| 2-4-2-4 | End X | 終了X座標 | ComboBox | マウス位置の終了X座標 |  |
| 2-4-2-5 | End Y | 終了Y座標 | ComboBox | マウス位置の終了Y座標 |  |
| 2-4-2-6 | Duration | 間隔時間 | ComboBox | マウス位置の開始から終了までにかかる時間の指定 |  |
| 2-4-2-7 | OK | OKボタン | Button | 動作の追加 |  |
| 2-4-2-8 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-4-3 | Wheel | ホイール動作画面 | Form | マウスのホイールに関わる画面 |  |
| 2-4-3-1 | Wheel orientation | 水平、垂直 | ComboBox | 水平、垂直 |  |
| 2-4-3-2 | Value | 値 | TextBox | ホイール時の値 |  |
| 2-4-3-3 | OK | OKボタン | Button | 動作の追加 |  |
| 2-4-3-4 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-5 | Text/Key | テキスト、キーボードに関わる動作 | ToolStripDropDownButton |  |  |
| 2-5-1 | Key press | キーボードに関わる動作画面 | Form |  |  |
| 2-5-1-1 | Key press option | キーボードの動作 | ComboBox | Press、Down、Up |  |
| 2-5-1-2 | Key | キー | ComboBox | キーボードのキー全て |  |
| 2-5-1-3 | Count | 回数 | TextBox | 回数 |  |
| 2-5-1-4 | OK | OKボタン | Button | 動作の追加 |  |
| 2-5-1-5 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-5-2 | Hotkey | ホットキー | Form | ホットキーの設定 |  |
| 2-5-2-1 | Press hotkey now | ホットキーの表示 | HotkeyTextBox | 入力されているホットキーを表示 | 例：Ctrl+Z |
| 2-5-2-2 | OK | OKボタン | Button | 動作の追加、入力されたホットキーに相当する複数のKey press項目を追加 | Ctrl+Zの場合、「Ctrl Down」, 「Z press」, 「Ctrl Up」の3つのKey press動作を追加する |
| 2-5-2-3 | Cancel | Cancelボタン | Button | Cancel |  |

> 補足: 独立した `Text` Action は本版に存在しない。`2-5` カテゴリの本版対象は `Key press` と `Hotkey` のみとし、テキスト入力は `Wait for text input` / `Find text (OCR)` 側で扱う。

| 2-6 | Wait | 待機に関わる動作 | ToolStripDropDownButton |  |  |
| 2-6-1 | Wait | 時間待機に関わる動作画面 | Form |  |  |
| 2-6-1-1 | Value | 待機時間(ms) | TextBox |  |  |
| 2-6-1-2 | OK | OKボタン | Button | 動作の追加 |  |
| 2-6-1-3 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-6-2 | Wait for pixel color | 色待機に関わる動作画面 | Form | 成功時の動作はIf true Go to、失敗時の動作はIf false Go to |  |
| 2-6-2-1 | X | X座標 | TextBox | 色を監視するX座標 |  |
| 2-6-2-2 | Y | Y座標 | TextBox | 色を監視するY座標 |  |
| 2-6-2-3 | Color | カラーコード | TextBox | #FFFFFF |  |
| 2-6-2-4 | Color表示 | カラーコードの色 | Panel | カラーコードの色を可視化 |  |
| 2-6-2-5 | Color tolerance | 色許容度 | TextBox | 0-100% | RGB距離、監視頻度は20ms(設定画面で設定可能) |
| 2-6-2-6 | If true Go to | 検出成功時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-6-2-7 | Waiting ms | 検出までの待機時間(ms) | TextBox | 単位はms |  |
| 2-6-2-8 | If false Go to | 検出失敗時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-6-2-9 | OK | OKボタン | Button | 動作の追加 |  |
| 2-6-2-10 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-6-5 | Wait for text input | テキスト入力に関わる動作画面 | Form | 指定された文字列がキーボード入力されるのを待つ、部分一致 |  |
| 2-6-5-1 | Text to wait for | 検出対象のテキスト | TextBox |  |  |
| 2-6-5-2 | If true Go to | 検出成功時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-6-5-3 | Waiting ms | 検出までの待機時間(ms) | TextBox | 単位はms |  |
| 2-6-5-4 | If false Go to | 検出失敗時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-6-5-5 | OK | OKボタン | Button | 動作の追加 |  |
| 2-6-5-6 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-7 | Image/OCR | 画像検索、OCRに関わる動作 | ToolStripDropDownButton |  |  |
| 2-7-1 | Find image | 画像検出動作画面 | Form | Capture bitmap と Load bitmap の優先順位は最新のもの、保存先はなし。 |  |
| 2-7-1-1 | Search area | 監視対象 | ComboBox | Entire Desktop, Area of Desktop, Focused window, Area of Focused window | 監視対象がArea of Desktop, Area of Focused windowの場合、2-7-1-1-1から2-7-1-1-5までが表示される |
| 2-7-1-1-1 | Define | 監視対象エリアの定義 | Button | Defineボタンクリック後、ドラッグアンドドロップで監視対象範囲を指定 | ドラッグアンドドロップ後、2-7-1-1-2から2-7-1-1-5までの値が自動入力される |
| 2-7-1-1-2 | X1 | 監視対象エリアの左上X座標 | TextBox | 監視対象エリアの左上X絶対座標値 |  |
| 2-7-1-1-3 | Y1 | 監視対象エリアの左上Y座標 | TextBox | 監視対象エリアの左上Y絶対座標値 |  |
| 2-7-1-1-4 | X2 | 監視対象エリアの右下X座標 | TextBox | 監視対象エリアの右下X絶対座標値 |  |
| 2-7-1-1-5 | Y2 | 監視対象エリアの右下Y座標 | TextBox | 監視対象エリアの右下Y絶対座標値 |  |
| 2-7-1-2 | Color tolerance | 色許容度 | TextBox | 0-100% | 0で完全一致 |
| 2-7-1-3 | Test | 検出テスト | Button | 検出した場合は緑文字でDetectedと表示され、検出した対象へマウスカーソルを移動する。未検出の場合は赤文字でNot Detectedと表示する。検出時間は固定で1秒とする。2-7-1-7から2-7-1-11までの設定は反映しない。 |  |
| 2-7-1-4 | Capture bitmap | 検出対象の画像切り取り | Button | 検出対象の画像を切り取り | すでに画像がある場合は上書き |
| 2-7-1-5 | Load bitmap from file | 検出対象の画像読み取り | Button | ファイルから検出対象の画像を開く | すでに画像がある場合は上書き |
| 2-7-1-7 | Mouse action | 検出成功時のマウス動作のOn, OFF | CheckBox | チェックボックス |  |
| 2-7-1-7-1 | Mouse action behavior | 検出成功時のマウス動作 | ComboBox | Positioning, Left-Click, Right-Click, Middle-Click, Double-Click |  |
| 2-7-1-7-2 | Mouse position | 検出成功時のマウス位置 | ComboBox | Center, Top-Left, Top-Right, Bottom-Left, Bottom-Right | Centerの場合、検出矩形の中心 |
| 2-7-1-8 | Save Coordinate behavior | 検出成功時の座標保存のOn, OFF | CheckBox | チェックボックス |  |
| 2-7-1-8-1 | Save Coordinate X | 検出成功時のX座標の保存指定 | TextBox | Variableに保存 |  |
| 2-7-1-8-2 | Save Coordinate Y | 検出成功時のY座標の保存指定 | TextBox | Variableに保存 |  |
| 2-7-1-9 | If true Go to | 検出成功時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-7-1-10 | Waiting ms | 検出までの待機時間(ms) | TextBox | 単位はms |  |
| 2-7-1-11 | If false Go to | 検出失敗時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-7-1-12 | OK | OKボタン | Button | 動作の追加 |  |
| 2-7-1-13 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-7-2 | Find text (OCR) | テキスト検出動作画面 | Form | OCRはWinOCRを使用 |  |
| 2-7-2-1 | Text to search for | 検出対象テキスト | TextBox | 完全一致 |  |
| 2-7-2-3 | Language | 検出対象言語 | ComboBox | English, Japanese |  |
| 2-7-2-4 | Search area | 監視対象 | ComboBox | Entire Desktop, Area of Desktop, Focused window, Area of Focused window | 監視対象がArea of Desktop, Area of Focused windowの場合、2-7-2-4-1から2-7-2-4-5までが表示される |
| 2-7-2-4-1 | Define | 監視対象エリアの定義 | Button | Defineボタンクリック後、ドラッグアンドドロップで監視対象範囲を指定 | ドラッグアンドドロップ後、2-7-2-4-2から2-7-2-4-5までの値が自動入力される |
| 2-7-2-4-2 | X1 | 監視対象エリアの左上X座標 | TextBox | 監視対象エリアの左上X絶対座標値 |  |
| 2-7-2-4-3 | Y1 | 監視対象エリアの左上Y座標 | TextBox | 監視対象エリアの左上Y絶対座標値 |  |
| 2-7-2-4-4 | X2 | 監視対象エリアの右下X座標 | TextBox | 監視対象エリアの右下X絶対座標値 |  |
| 2-7-2-4-5 | Y2 | 監視対象エリアの右下Y座標 | TextBox | 監視対象エリアの右下Y絶対座標値 |  |
| 2-7-2-5 | Test | 検出テスト | Button | 検出した場合は緑文字でDetectedと表示され、検出した対象へマウスカーソルを移動する。未検出の場合は赤文字でNot Detectedと表示する。検出時間は固定で1秒とする。2-7-2-8から2-7-2-12までの設定は反映しない。 |  |
| 2-7-2-8 | Mouse action | 検出成功時のマウス動作のOn, OFF | CheckBox | チェックボックス |  |
| 2-7-2-8-1 | Mouse action behavior | 検出成功時のマウス動作 | ComboBox | Positioning, Left-Click, Right-Click, Middle-Click, Double-Click |  |
| 2-7-2-8-2 | Mouse position | 検出成功時のマウス位置 | ComboBox | Center, Top-Left, Top-Right, Bottom-Left, Bottom-Right |  |
| 2-7-2-9 | Save Coordinate behavior | 検出成功時の座標保存のOn, OFF | CheckBox | チェックボックス |  |
| 2-7-2-9-1 | Save Coordinate X | 検出成功時のX座標の保存指定 | TextBox | Variableに保存 |  |
| 2-7-2-9-2 | Save Coordinate Y | 検出成功時のY座標の保存指定 | TextBox | Variableに保存 |  |
| 2-7-2-10 | If true Go to | 検出成功時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-7-2-11 | Waiting ms | 検出までの待機時間(ms) | TextBox | 単位はms |  |
| 2-7-2-12 | If false Go to | 検出失敗時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-7-2-13 | OK | OKボタン | Button | 動作の追加 |  |
| 2-7-2-14 | Cancel | Cancelボタン | Button | Cancel |  |
| 2-8 | Misc | その他動作 | ToolStripDropDownButton |  |  |
| 2-8-1 | Repeat | 繰り返し実行動作画面 | Form | `Start Label` の行から Repeat 行の直前までを繰り返す | Repeat のネストは禁止。`finishGoTo` は完了後に1回だけ適用する。 |
| 2-8-1-1 | Start Label | 繰り返し開始Labelを指定 | ComboBox |  | 指定したLabelが存在しない場合は保存時 / 読込時エラー |
| 2-8-1-1-1 | Seconds RadioButton | Repeat条件、秒数のラジオボタン | RadioButton |  | Repeat条件は4つのRadioButtonで排他運用。選択時は `Seconds` 入力欄のみ活性化する。 |
| 2-8-1-1-1-1 | Seconds | Repeat条件、秒数 | TextBox | seconds | `Seconds RadioButton` 選択時のみ活性。未選択時は非活性。 |
| 2-8-1-1-2 | repetitions | Repeat条件、回数のラジオボタン | RadioButton |  | Repeat条件は4つのRadioButtonで排他運用。選択時は `repetitions` 入力欄のみ活性化する。 |
| 2-8-1-1-2-1 | repetitions | Repeat条件、回数 | TextBox | repetitions | `repetitions` RadioButton 選択時のみ活性。未選択時は非活性。 |
| 2-8-1-1-3 | Until | Repeat条件、終了時間のラジオボタン | RadioButton |  | Repeat条件は4つのRadioButtonで排他運用。選択時は `Until` 入力欄のみ活性化する。 |
| 2-8-1-1-3-1 | Until | Repeat条件、終了時間 | DateTimePicker | HH:mm:ss | `Until` RadioButton 選択時のみ活性。未選択時は非活性。 |
| 2-8-1-1-4 | Infinite | Repeat条件、無限 | RadioButton |  | Repeat条件は4つのRadioButtonで排他運用。選択時は `Seconds` / `repetitions` / `Until` の各入力欄を非活性にする。Stop操作またはエラー発生まで継続。 |
| 2-8-1-2 | Finish Label | Repeat終了後のLabelを指定 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行。繰り返し完了後に1回だけ適用する。 |
| 2-8-2 | Go to | Go to動作画面 | Form | 指定されたLabelの行に移動 |  |
| 2-8-2-1 | Go to Label | Go to Label指定 | ComboBox | Start, End, Next, Label | Labelはユーザーで定義されたLabelを全列挙 |
| 2-8-3 | If | If条件動作画面 | Form |  |  |
| 2-8-3-1 | Variable name | Variable name選択欄 | ComboBox | If条件の対象 | Variableは文字列もしくは数字 |
| 2-8-3-2 | If | 条件欄 | ComboBox | Text equals, Text begins with, Text ends with, Text includes, Text doesn't equal, Text doesn't begin with, Text doesn't end with, Text doesn't includes, Text is longer than, Text is shorter than, Value is higher than, Value is lower than, Value is higher-or-equal than, Value is lower-or-equal than, Value is defined |  |
| 2-8-3-3 | Value | 条件の値欄 | TextBox | 条件の値欄 |  |
| 2-8-3-4 | If true Go to | 検出成功時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-8-3-5 | If false Go to | 検出失敗時の動作 | ComboBox | Start, End, Next, Label | Labelはユーザーが定義したラベルの値すべてを表示、Startはマクロ先頭行、Endはマクロ最終行、Nextはマクロの次の行、Labelは一致するLabelの行 |
| 2-8-4 | Embed macro file | マクロファイルの埋め込み画面 | Form |  |  |
| 2-8-4-1 | Embed macro file path | マクロファイルの指定パス | TextBox | 指定したマクロファイルを起動する |  |
| 2-8-5 | Execute program | プログラムの実行画面 | Form |  |  |
| 2-8-5-1 | Execute program path | プログラムの指定パス | TextBox | 指定したプログラムを起動する |  |


### 3.3 Playback

| 開発ID | 機能名 | 概要 | UI部品 | 起動/操作 | 備考 |
|---|---|---|---|---|---|
| 3 | Playback | 再生欄 | TabPage |  |  |
| 3-1 | Play | マクロを再生 | ToolStripSplitButton |  |  |
| 3-1-1 | Play until selected | 選択行までマクロを再生 | ToolStripMenuItem |  |  |
| 3-1-2 | Play from selected | 選択行からマクロを再生 | ToolStripMenuItem |  |  |
| 3-1-3 | Play selected | 選択行を再生 | ToolStripMenuItem |  |  |
| 3-2 | Record | マクロを選択行から記録する | ToolStripSplitButton |  |  |
| 3-2-1 | New Record | マクロを新規に記録する | ToolStripMenuItem |  |  |
| 3-2-1-1 | Confirmation | 未保存の変更 | MessageBox | 未保存変更がある状態で `New Record` を実行すると出現 | タイトル: `未保存の変更` / 本文: `変更内容は保存されていません。保存しますか？` / ボタン: `保存` `保存しない` `キャンセル` |
| 3-2-1-1-1 | Save | 保存 | Button | 現在編集中のマクロを保存し、新規記録を開始する | 保存失敗時は新規記録へ進まない。 |
| 3-2-1-1-2 | Don't Save | 保存しない | Button | 現在編集中の変更を破棄し、新規記録を開始する |  |
| 3-2-1-1-3 | Cancel | キャンセル | Button | `New Record` を中止し、現在画面に留まる |  |
| 3-3 | Stop | マクロの再生を停止 | ToolStripButton |  |  |
| 3-4 | Playback Properties | 再生設定欄 | Area |  |  |
| 3-4-1 | Playback Speed | 再生速度（％） | TextBox |  | 最小は1, 最大は1000、初期値は100, 不正値時は入力キャンセル、単位は%。`Wait` Action の待機時間にのみ適用し、検出系待機 (`FindImage` / `FindText` / `WaitForTextInput`) のタイムアウト・ポーリング間隔、および入力系Action (`MouseClick` / `KeyPress`) には適用しない。 |
| 3-4-3 | Repeat | マクロファイル繰り返し実行指定 | TextBox |  | 最小は1, 最大は9999999、初期値は1, 不正値時は入力キャンセル、単位は回数。Playback Repeat は現在の再生対象全体を1 cycleとして繰り返す。Macro 内の `Repeat` Action は内部制御としてそのまま動作し、`Repeat(Infinite)` がある場合は内側が優先される。 |

---
## 4. 本版対象外として扱う項目

以下は画像や旧文書に記載が残っていても、`Macro仕様書_v7.xlsx` に存在しないため本版仕様には含めない。

- Wait for screen change
- After playback
- Playback filter
- If 条件の `RegEx`

---
以上
