Domain Specification
MacroTool – ドメイン仕様書
1. 目的

本ドキュメントは、MacroTool のドメインモデルにおける
業務ルール・不変条件・集約責務 を定義する。

UI / インフラ / 永続化の都合ではなく、
業務として正しい状態とは何か を明文化することを目的とする。

2. 用語定義
用語	意味
Macro	マクロ全体（集約ルート）
Step	マクロの1行
Action	Stepが保持する実行動作
Label	Stepを識別する文字列
GoTo	指定Stepへ遷移する制御
実行可能状態	実行前検証を通過した状態
3. 集約構造
Macro（Aggregate Root）
 └─ MacroStep（Entity）
      └─ MacroAction（抽象）
           └─ 各種具体Action

4. Macro 集約の不変条件
4.1 Stepの存在

Macroは0件以上のStepを保持可能

ただし実行可能状態では1件以上必要

IsExecutable == true → Steps.Count >= 1

4.2 Step順序の整合性

Stepは順序付きコレクション

Indexは0から連番

StepIndex は 0 から Count-1 の連続値

4.3 StepIdの一意性
Macro内で StepId は一意

4.4 Label制約（重要）
ルール

Labelは任意（null可）

nullでない場合、空文字・空白は禁止

LabelはMacro内で一意

不変条件
Label != null のStepにおいて、Labelはすべてユニーク

4.5 Label自動採番（コピペ時）
目的

コピペ操作時にLabel衝突による不変条件違反を防ぐ

規則

貼り付け対象Labelが未使用 → そのまま使用

使用済み → 末尾に整数を付与し未使用になるまでインクリメント

例：

Jump先
Jump先1
Jump先2


既存：Jump先, Jump先1

貼り付け：Jump先 → Jump先2

分解規則

末尾が数値の場合は数値部分をインクリメント

数値なしは「1」から開始

手入力変更時

重複は許可しない（例外とする）

自動採番は行わない

4.6 GoTo整合性

GoToSpecがLabel指定の場合、Macro内に必ず存在する必要がある

GoToSpec.Kind == Label → 一致Labelが存在


Labelは一意のため、解決先は常に1件。

5. MacroStep不変条件
5.1 Action必須
MacroStep.Action != null

5.2 Label正規化
Label != null → Trim(Label) != ""

6. MacroAction共通不変条件
6.1 ActionId一意（推奨）
Macro内で ActionId は一意

6.2 Type整合性
Action.Type は実体クラスと一致

7. 各Actionの不変条件（概要）
MouseClick
Point != null
Button != null
ClickType != null
Absoluteの場合 → X,Y >= 0

MouseMove
Start != null
End != null
Duration > 0

KeyPress
Key != null
Count >= 1

Wait
Duration > 0

WaitForPixelColor
0 <= Tolerance <= 100
Timeout > 0
OnTrue != null
OnFalse != null

FindImage / FindText
Timeout > 0
OnTrue != null
OnFalse != null

Repeat
TargetLabel != null
Condition != null
AfterRepeat != null

If
Variable != null
Operator != null
OnTrue != null
OnFalse != null

8. 実行前検証

Macroは実行前に以下を検証する：

Stepが存在する

Labelが重複していない

GoTo参照整合性

Repeat参照整合性

Timeout値が正

Tolerance値が範囲内

ValidationResult ValidateExecutable()

9. 設計原則

不変条件はUIではなくDomainで保証する

コンストラクタ／Factoryで必ず検証する

例外はDomainExceptionとする

10. 今後拡張時の原則

新Action追加時は必ず不変条件を本仕様に追記する

ValidateExecutableに影響があれば明記する