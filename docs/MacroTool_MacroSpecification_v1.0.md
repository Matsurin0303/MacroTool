# MacroTool マクロ動作機能仕様書

- Version: Macro_v1.0  
- 作成日: 2026-02-12  
- 開発環境: Visual Studio 2026  
- 開発言語: C#  
- 設計思想: ドメイン駆動設計（DDD） + テスト駆動開発（TDD）  

---

## 1. 目的・概要

本仕様書は、MacroTool における **マクロの作成・編集・実行**、および関連する **マウス／キーボード操作、待機、検出、制御構文** 機能の仕様を定義する。

---

## 2. 用語

- **マクロ**: 操作手順（アクション）を並べた実行単位  
- **アクション**: マクロを構成する 1 ステップ（例: Click / Wait / Find image 等）  
- **Variable**: 実行中に値（文字列・数値・座標など）を保持する領域  
- **(ユーザー定義Label一覧…)**: ジャンプ先として参照可能な行識別子  
- **GoTo**: 指定行（Start/End/Next/(ユーザー定義Label一覧…)）へ実行位置を移動する制御  

---

## 3. 機能一覧

---

# 3.1 File（ファイル操作）

| 開発ID | 機能名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 1 | File | Fileに関する操作欄 |  |  |
| 1-1 | New | 新規マクロの作成 |  |  |
| 1-2 | Open | 既存マクロを開く |  |  |
| 1-3 | Recent files | 最近開いたマクロを表示しクリックで開く |  |  |
| 1-4 | Save | マクロを保存（上書き） |  |  |
| 1-5 | Save As… | マクロを名前を付けて保存 |  |  |
| 1-6 | Export to CSV | CSVとして出力 |  |  |
| 1-7 | Schedule macro | マクロの起動をスケジュールする |  |  |
| 1-8 | Settings | 設定画面を開く |  |  |
| 1-9 | Exit | ツールを終了する |  |  |

---

# 3.2 Record and Edit（記録・編集）

| 開発ID | 機能名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2 | Record and Edit | マクロの記録、編集に関する操作欄 |  |  |
| 2-1 | Play | マクロを再生 |  |  |
| 2-1-1 | Play until selected | 選択行までマクロを再生 |  |  |
| 2-1-2 | Play from selected | 選択行からマクロを再生 |  |  |
| 2-1-3 | Play selected | 選択行を再生 |  |  |
| 2-2 | Record | マクロを記録する |  |  |
| 2-3 | Stop | マクロの再生を停止 |  |  |

---

# 3.3 Mouse（マウス操作）

## 3.3.1 Click（クリック動作）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-4 | Mouse | マウスに関わる操作欄 |  |  |
| 2-4-1 | Click | クリック動作画面 |  | マウスのクリックに関わる画面 |
| 2-4-1-1 | Mouse button | マウスのボタン | 選択 | 右、左、真ん中、サイドボタン1、サイドボタン2 |
| 2-4-1-2 | Action | 動作 | 選択 | クリック、ダブルクリック、押す、離す |
| 2-4-1-3 | Relative | 相対座標ON/OFF | 切替 | ON:現在座標から相対移動／OFF:絶対座標 |
| 2-4-1-4 | X | X座標 | 入力 | マウス位置のX座標 |
| 2-4-1-5 | Y | Y座標 | 入力 | マウス位置のY座標 |
| 2-4-1-6 | OK | OKボタン | クリック | 動作の追加 |
| 2-4-1-7 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.3.2 Move（移動動作）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-4-2 | Move | 移動動作画面 |  | マウスの移動に関わる画面 |
| 2-4-2-1 | Relative | 相対座標ON/OFF | 切替 | ON:現在座標から相対移動／OFF:絶対座標 |
| 2-4-2-2 | Start X | 開始X座標 | 入力 |  |
| 2-4-2-3 | Start Y | 開始Y座標 | 入力 |  |
| 2-4-2-4 | End X | 終了X座標 | 入力 |  |
| 2-4-2-5 | End Y | 終了Y座標 | 入力 |  |
| 2-4-2-6 | Duration | 間隔時間 | 入力 | 開始→終了までの所要時間 |
| 2-4-2-7 | OK | OKボタン | クリック | 動作の追加 |
| 2-4-2-8 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.3.3 Wheel（ホイール動作）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-4-3 | Wheel | ホイール動作画面 |  | マウスのホイールに関わる画面 |
| 2-4-3-1 | Wheel orientation | 水平/垂直 | 選択 | 水平、垂直 |
| 2-4-3-2 | Value | 値 | 入力 | ホイール時の値 |
| 2-4-3-3 | OK | OKボタン | クリック | 動作の追加 |
| 2-4-3-4 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

# 3.4 Text/Key（テキスト・キーボード）

## 3.4.1 Key press

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-5 | Text/Key | テキスト、キーボードに関わる動作画面 |  |  |
| 2-5-1 | Key press | キーボードに関わる動作画面 |  |  |
| 2-5-1-1 | Key press option | キーボードの動作 | 選択 | Press / Down / Up |
| 2-5-1-2 | Key | キー | 選択 | キーボードのキー全て |
| 2-5-1-3 | Count | 回数 | 入力 |  |
| 2-5-1-4 | OK | OKボタン | クリック | 動作の追加 |
| 2-5-1-5 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.4.2 Hotkey（ホットキー）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-5-2 | Hotkey | ホットキー設定画面 |  |  |
| 2-5-2-1 | Press hotkey now | 入力中ホットキー表示 | 入力 | 例：Ctrl+Z |
| 2-5-2-2 | OK | OKボタン | クリック | 入力ホットキーに相当する複数のKey pressを追加（例: Ctrl Down → Z Press → Ctrl Up） |
| 2-5-2-3 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.4.3 Text
将来実装予定。

---

# 3.5 Wait（待機）

## 3.5.1 Wait（時間待機）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-6 | Wait | 待機に関わる動作 |  |  |
| 2-6-1 | Wait | 時間待機に関わる動作画面 |  |  |
| 2-6-1-1 | Value | 待機時間(ms) | 入力 |  |
| 2-6-1-2 | OK | OKボタン | クリック | 動作の追加 |
| 2-6-1-3 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.5.2 Wait for pixel color（色待機）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-6-2 | Wait for pixel color | 指定座標の色一致待機 |  |  |
| 2-6-2-1 | X | X座標 | 入力 | 色監視X座標 |
| 2-6-2-2 | Y | Y座標 | 入力 | 色監視Y座標 |
| 2-6-2-3 | Color | カラーコード | 入力 | `#FFFFFF` |
| 2-6-2-4 | Color表示 | 色の可視化 | 表示 | カラーコードの色 |
| 2-6-2-5 | Color tolerance | 色許容度 | 入力 | 0〜100% |
| 2-6-2-6 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-2-7 | Waiting ms | 検出までの待機(ms) | 入力 | 単位ms |
| 2-6-2-8 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-2-9 | OK | OKボタン | クリック | 動作の追加 |
| 2-6-2-10 | Cancel | Cancelボタン | クリック | 変更破棄 |

備考（(ユーザー定義Label一覧…)の意味）  
- (ユーザー定義Label一覧…) はユーザーが各マクロに定義付けた一意の値
- Labelは一意：既存Labelと重複したら末尾に数字付与、数字付きならインクリメント（例：Jump先 → Jump先1 → Jump先2…）
- GoToで選択できるもの：Start / Next / End / (ユーザー定義Label一覧…)
- Start: マクロ先頭行 / End: マクロ最終行 / Next: 次行 / (ユーザー定義Label一覧…): 該当ラベル行

---

## 3.5.3 Wait for screen changes（画面変化待機）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-6-3 | Wait for screen changes | 画面変化を検出するまで待機 |  |  |
| 2-6-3-1 | Search area | 監視対象 | 選択 | Entire Desktop / Area of Desktop / Focused window / Area of Focused window |
| 2-6-3-1-1 | Define | 監視範囲定義 | クリック→D&D | D&D後、座標値自動入力 |
| 2-6-3-1-2 | X1 | 左上X | 表示/入力 |  |
| 2-6-3-1-3 | Y1 | 左上Y | 表示/入力 |  |
| 2-6-3-1-4 | X2 | 右下X | 表示/入力 |  |
| 2-6-3-1-5 | Y2 | 右下Y | 表示/入力 |  |
| 2-6-3-2 | Mouse action | 成功時マウス動作ON/OFF | チェック |  |
| 2-6-3-2-1 | Mouse action behavior | 成功時マウス動作 | 選択 | Positioning / Left-Click / Right-Click / Middle-Click / Double-Click |
| 2-6-3-3 | Save Coordinate | 成功時座標保存ON/OFF | チェック |  |
| 2-6-3-3-1 | Save Coordinate behavior(X) | X座標保存先 | 選択 | Variableに保存 |
| 2-6-3-3-2 | Save Coordinate behavior(Y) | Y座標保存先 | 選択 | Variableに保存 |
| 2-6-3-4 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-3-5 | Waiting ms | 検出までの待機(ms) | 入力 |  |
| 2-6-3-6 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-3-7 | OK | OKボタン | クリック | 動作の追加 |
| 2-6-3-8 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.5.4 Wait for hotkey press
将来実装予定。

## 3.5.5 Wait for text input

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-6-5 | Wait for text input | テキスト入力待機 |  |  |
| 2-6-5-1 | Text to wait for | 検出対象テキスト | 入力 |  |
| 2-6-5-2 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-5-3 | Waiting ms | 検出までの待機(ms) | 入力 |  |
| 2-6-5-4 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-6-5-5 | OK | OKボタン | クリック | 動作の追加 |
| 2-6-5-6 | Cancel | Cancelボタン | クリック | 変更破棄 |

## 3.5.6 Wait for file
将来実装予定。

---

# 3.6 検出（Detection）

## 3.6.1 Find image

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-7-1 | Find image | 画像検出 |  |  |
| 2-7-1-1 | Search area | 監視対象 | 選択 | Entire Desktop / Area of Desktop / Focused window / Area of Focused window |
| 2-7-1-1-1 | Define | 監視範囲定義 | クリック→D&D | D&D後、座標値自動入力 |
| 2-7-1-1-2 | X1 | 左上X | 表示/入力 |  |
| 2-7-1-1-3 | Y1 | 左上Y | 表示/入力 |  |
| 2-7-1-1-4 | X2 | 右下X | 表示/入力 |  |
| 2-7-1-1-5 | Y2 | 右下Y | 表示/入力 |  |
| 2-7-1-2 | Color tolerance | 色許容度 | 入力 | 0〜100% |
| 2-7-1-3 | Test | 検出テスト | 実行 |  |
| 2-7-1-4 | Capture bitmap | 画像切り取り | 実行 | 検出対象画像を切り取り |
| 2-7-1-5 | Load bitmap from file | 画像読み取り | 実行 | ファイルから検出対象画像を開く |
| 2-7-1-6 | Variable | 画像の参照先 | 選択 | Variableから選択 |
| 2-7-1-7 | Mouse action | 成功時マウス動作ON/OFF | チェック |  |
| 2-7-1-7-1 | Mouse action behavior | 成功時マウス動作 | 選択 | Positioning / Left-Click / Right-Click / Middle-Click / Double-Click |
| 2-7-1-7-2 | Mouse position | マウス位置 | 選択 | Center / Top-Left / Top-Right / Bottom-Left / Bottom-Right |
| 2-7-1-8 | Save Coordinate | 成功時座標保存ON/OFF | チェック |  |
| 2-7-1-8-1 | Save Coordinate behavior(X) | X座標保存先 | 選択 | Variableに保存 |
| 2-7-1-8-2 | Save Coordinate behavior(Y) | Y座標保存先 | 選択 | Variableに保存 |
| 2-7-1-9 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-7-1-10 | Waiting ms | 検出までの待機(ms) | 入力 |  |
| 2-7-1-11 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-7-1-12 | OK | OKボタン | クリック | 動作の追加 |
| 2-7-1-13 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.6.2 Find text（OCR）

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-7-2 | Find text (OCR) | OCRによる文字検出 |  |  |
| 2-7-2-1 | Text to search for | 検出対象テキスト | 入力 |  |
| 2-7-2-2 | RegEx term | 正規表現ON/OFF | 切替 | 将来実装予定 |
| 2-7-2-3 | Language | 言語 | 選択 | English / Japanese |
| 2-7-2-4 | Search area | 監視対象 | 選択 | Entire Desktop / Area of Desktop / Focused window / Area of Focused window |
| 2-7-2-4-1 | Define | 監視範囲定義 | クリック→D&D | D&D後、座標値自動入力 |
| 2-7-2-4-2 | X1 | 左上X | 表示/入力 |  |
| 2-7-2-4-3 | Y1 | 左上Y | 表示/入力 |  |
| 2-7-2-4-4 | X2 | 右下X | 表示/入力 |  |
| 2-7-2-4-5 | Y2 | 右下Y | 表示/入力 |  |
| 2-7-2-5 | Test | 検出テスト | 実行 |  |
| 2-7-2-6 | Optimize contrast and sharpness | 最適化 | 切替 | 将来実装予定 |
| 2-7-2-7 | Optimize for single characters or short text | 最適化 | 切替 | 将来実装予定 |
| 2-7-2-8 | Mouse action | 成功時マウス動作ON/OFF | チェック |  |
| 2-7-2-8-1 | Mouse action behavior | 成功時マウス動作 | 選択 | Positioning / Left-Click / Right-Click / Middle-Click / Double-Click |
| 2-7-2-8-2 | Mouse position | マウス位置 | 選択 | Center / Top-Left / Top-Right / Bottom-Left / Bottom-Right |
| 2-7-2-9 | Save Coordinate | 成功時座標保存ON/OFF | チェック |  |
| 2-7-2-9-1 | Save Coordinate behavior(X) | X座標保存先 | 選択 | Variableに保存 |
| 2-7-2-9-2 | Save Coordinate behavior(Y) | Y座標保存先 | 選択 | Variableに保存 |
| 2-7-2-10 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-7-2-11 | Waiting ms | 検出までの待機(ms) | 入力 |  |
| 2-7-2-12 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-7-2-13 | OK | OKボタン | クリック | 動作の追加 |
| 2-7-2-14 | Cancel | Cancelボタン | クリック | 変更破棄 |

---

## 3.6.3 Capture text (OCR)
将来実装予定。

## 3.6.4 Capture image (Screenshot)
将来実装予定。

## 3.6.5 Get barcode / QR code
将来実装予定。

---

# 3.7 制御（Control Flow）

## 3.7.1 Repeat

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-8-1 | Repeat | 繰り返し実行 |  |  |
| 2-8-1-1 | (ユーザー定義Label一覧…) | 指定(ユーザー定義Label一覧…)範囲を繰り返す | 選択 |  |
| 2-8-1-1-1 | Seconds | 条件：秒数 | 入力 | seconds |
| 2-8-1-1-2 | Repetitions | 条件：回数 | 入力 | repetitions |
| 2-8-1-1-3 | Until | 条件：終了時刻 | 入力 | HH:mm:ss |
| 2-8-1-1-4 | Infinite | 条件：無限 | 選択 |  |
| 2-8-1-2 | Go to | Repeat終了後の遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |

---

## 3.7.2 Go to

| 開発ID | 機能名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-8-2 | Go to | 指定(ユーザー定義Label一覧…)の行に移動 |  |  |

---

## 3.7.3 If

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-8-3 | If | 条件分岐 |  |  |
| 2-8-3-1 | Variable name | 条件対象のVariable選択 | 選択 |  |
| 2-8-3-2 | If | 条件種別 | 選択 | Text/Value/RegEx/Defined |
| 2-8-3-3 | Value | 条件値 | 入力 |  |
| 2-8-3-4 | If true Go to | 成功時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |
| 2-8-3-5 | If false Go to | 失敗時遷移 | 選択 | Start / End / Next / (ユーザー定義Label一覧…) |

条件種別一覧：
- Text equals
- Text begins with
- Text ends with
- Text include
- Text doesn't equal
- Text doesn't begin with
- Text doesn't end on
- Text doesn't include
- Text is longer than
- Text is shorter than
- Value is higher than
- Value is lower than
- Value is higher-or-equal than
- Value is lower-or-equal than
- RegEx
- Value is defined

---

## 3.7.4 Embed macro file

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-8-4 | Embed macro file | マクロファイルを埋め込み実行 |  |  |
| 2-8-4-1 | Embed macro file path | 実行対象マクロファイルパス | 入力 | 指定したマクロファイルを起動する |

---

## 3.7.5 Execute program

| 開発ID | 項目名 | 概要 | 起動/操作 | 備考 |
|---|---|---|---|---|
| 2-8-5 | Execute program | 外部プログラムを実行 |  |  |
| 2-8-5-1 | Execute program path | 実行対象プログラムパス | 入力 | 指定したプログラムを起動する |

---

## 3.7.6 将来実装予定（2-8-6 ～ 2-8-14）

- 2-8-6 Window focus
- 2-8-7 Show notification（Windows通知）
- 2-8-8 Show message box
- 2-8-9 Beep
- 2-8-10 Set variable
- 2-8-11 Set variable from data list
- 2-8-12 Save variable
- 2-8-13 Calculate expression
- 2-8-14 Extract from Web site

---

## 4. 共通GoTo仕様

GoTo 指定可能値は以下とする：

- **Start**: マクロ先頭行へ遷移
- **End**: マクロ最終行へ遷移
- **Next**: 次行へ遷移
- **(ユーザー定義Label一覧…)**: 指定ラベル行へ遷移（ユーザー定義ラベル一覧から選択）

---

## 5. DDD設計の指針（参考）

想定される主要ドメイン要素：

- Macro（Aggregate Root）
- MacroAction（抽象）
  - MouseAction
  - KeyAction
  - WaitAction
  - DetectionAction
  - ControlFlowAction
- Variable
- (ユーザー定義Label一覧…)

---

以上
