マクロ記録ツール 動作仕様書（論理仕様）
1. システム概要
1.1 目的

本ツールは、ユーザー操作（マウス・キーボード・画面検出等）を記録・編集・再生できるマクロ実行アプリケーションである。

2. ドメインモデル概要（DDD視点）
2.1 集約
■ Macro（集約ルート）

複数の MacroAction を順序付きで保持する

実行制御（再生開始位置、分岐、繰り返し）を管理する

■ MacroAction（抽象）

すべての動作は以下を持つ：

項目	内容
Id	一意識別子
Label	任意ラベル
ExecutionResult	成功 / 失敗
NextAction	次の遷移先
3. 共通仕様
3.1 GoTo指定ルール

すべての条件付き動作は以下を持つ：

指定値	意味
Start	マクロ先頭
End	マクロ最終行
Next	次の行
Label	指定ラベル行
3.2 座標仕様

Absolute（絶対座標）

Relative（現在座標基準）

3.3 待機仕様

待機単位：ms

タイムアウト発生時は「失敗扱い」

4. 機能仕様
4.1 File操作
機能	内容
New	新規マクロ作成
Open	マクロ読み込み
Recent Files	最近使用したファイル
Save	上書き保存
Save As	名前を付けて保存
Export to CSV	CSV形式出力
Schedule macro	スケジュール実行
Settings	設定画面
Exit	終了
4.2 Record & Edit
機能	内容
Play	全体再生
Play until selected	選択行まで再生
Play from selected	選択行から再生
Play selected	選択行のみ
Record	記録開始
Stop	停止
4.3 Mouse系
4.3.1 Click
入力項目

MouseButton（Left/Right/Middle/Side1/Side2）

Action（Click/DoubleClick/Down/Up）

X,Y

Relative ON/OFF

動作

指定座標へ移動 → 指定アクション実行

4.3.2 Move
入力

StartX/Y

EndX/Y

Duration(ms)

Relative

動作

線形補間移動

4.3.3 Wheel

Orientation（Horizontal/Vertical）

Value（スクロール量）

4.4 Keyboard系
4.4.1 Key Press

Option（Press / Down / Up）

Key

Count

4.4.2 Hotkey

入力されたショートカットを分解し以下を自動追加：

例：
Ctrl+Z
→ Ctrl Down
→ Z Press
→ Ctrl Up

4.5 Wait系
4.5.1 Wait（時間）

Value(ms)

4.5.2 Wait for Pixel Color
入力

X,Y

Color（#RRGGBB）

Tolerance（0-100%）

Timeout(ms)

成功時GoTo

失敗時GoTo

判定式

色差計算：

RGB差分 <= 許容範囲

4.5.3 Wait for Screen Changes
検出対象

Entire Desktop

Area of Desktop

Focused Window

Area of Focused Window

オプション

Mouse Action

Save Coordinate（変数保存）

成功/失敗GoTo

4.5.4 Wait for Hotkey Press（将来実装）
4.5.5 Wait for Text Input

指定テキスト検出待機

4.6 Image / OCR系
4.6.1 Find Image
入力

Search Area

Bitmap（Capture / File / Variable）

Color tolerance

Testボタン

Mouse Action

Save Coordinate

成功/失敗GoTo

4.6.2 Find Text (OCR)
入力

Text

Language（EN/JP）

Search Area

Mouse Action

Save Coordinate

成功/失敗GoTo

4.7 制御構造
4.7.1 Repeat
条件

秒数

回数

Until（時刻）

Infinite

終了後GoTo指定あり
4.7.2 GoTo

指定Labelへジャンプ

4.7.3 If
入力

VariableName

条件式

Value

条件種別
文字列系

equals

begins with

ends with

include

正規表現

数値系

<

=

<=

defined

4.8 外部実行
機能	内容
Embed macro file	別マクロ起動
Execute program	外部プログラム起動
Show notification	Windows通知
Beep	ビープ音
5. 将来実装予定一覧

Wait for hotkey press

Capture text (OCR)

Screenshot

Barcode/QR

Window focus

Message box

Variable操作群

Web抽出

6. 実行フロー仕様
Macro開始
 ↓
Action実行
 ↓
成功 or 失敗
 ↓
GoTo解決
 ↓
次Actionへ
 ↓
End到達で終了

7. テスト観点（TDD視点）
単体テスト観点例

GoTo解決が正しい行へ遷移するか

Relative座標が正しく計算されるか

色許容度判定が正しいか

Repeat条件終了判定が正しいか

Hotkey分解が正しくKeyPress列へ展開されるか

8. 今後の推奨

次の段階として：

✅ ドメインクラス設計図作成

✅ MacroActionクラス構造設計

✅ 状態遷移図作成

✅ JSON保存フォーマット設計

✅ テストケース一覧作成