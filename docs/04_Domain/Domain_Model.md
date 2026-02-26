# ドメイン仕様（Domain Model）

- Version: **Macro_v1.0.0**
- 更新日: 2026-02-26
- 対象: **MacroTool**（WinForms / C# / DDD + TDD）

本書は、`docs/02_FunctionSpec/MacroTool_MacroSpecification_v1.0.0.md`（機能仕様）に基づき、実装のブレを防ぐために **ドメイン用語・集約・不変条件**を定義する。

---

## 1. スコープ

### 1.1 本版で扱うもの（Macro_v1.0.0）
- Macro（マクロ）と Step（ステップ）と Action（アクション）
- 編集（追加/削除/並べ替え/プロパティ変更）に必要な不変条件
- 再生（実行）に必要な **参照整合性**（GoToターゲット、Label一意、検索領域の妥当性など）

### 1.2 本版で扱わないもの（将来実装予定）
機能仕様内で「将来実装予定」と明記されるものは **ドメインモデルに含めない**。  
（例：Wait for hotkey press、Wait for file change、Capture text/image、Barcode/QR、2-8-6〜2-8-14 の変数/通知/WEB抽出等）

---

## 2. ユビキタス言語（用語集）

| 用語 | 意味 |
|---|---|
| Macro | アクション列を持つ実行単位（集約ルート） |
| Step | Macro内の1行（順序を持つ） |
| Action | Stepが実行する具体的な操作（Mouse/Key/Wait/Detection/ControlFlow など） |
| Label | GoTo/Repeat 等の参照先として使う **一意な行識別子** |
| GoTo | 実行位置を Start/Next/End/Label に移動する制御 |
| Variable | 実行中に値を保持する領域（本版は “参照/保存先” を値オブジェクトとして扱う） |
| SearchArea | 検索・監視対象（Entire Desktop / Area of Desktop / Focused window / Area of Focused window） |
| Rect | 監視/検索領域の矩形（X1,Y1,X2,Y2） |
| Playback | Macroを上から順に評価し、必要に応じて分岐・待機する実行処理 |

---

## 3. 集約設計（DDD）

### 3.1 集約ルート: Macro
**責務**
- Stepの順序と整合性を守る
- Labelの一意性を守る（追加/変更/複製時）
- GoTo/Repeat など “参照を持つ Action” の参照整合性を守る（少なくとも検証可能にする）

**保持するもの（例）**
- `MacroId`
- `IReadOnlyList<MacroStep> Steps`
- `MacroVersion SpecVersion`（例：Macro_v1.0.0）

### 3.2 エンティティ: MacroStep
**責務**
- 1行を表し、順序（Index/Order）を持つ
- 任意で `StepLabel?` を持つ（Labelなしの行もある）
- 1つの `MacroAction` を持つ

**推奨プロパティ（例）**
- `StepId`
- `int Order`（0..n-1 の連番）
- `StepLabel? Label`
- `MacroAction Action`

---

## 4. 値オブジェクト（Value Objects）

### 4.1 StepLabel
- `string Value`（トリム後の値を保持）

**正規化（推奨）**
- 前後空白は除去して扱う
- 文字種の制限は最小限（日本語可）

### 4.2 GoToTarget
- 種別: `Start | Next | End | Label`
- `Label` の場合は `StepLabel` を保持（参照）

### 4.3 SearchArea / Rect
- `SearchAreaKind`（4種類）
- Kindが Area を含む場合は `Rect` 必須

Rectは `X1,Y1,X2,Y2` の4点で保持し、**幅・高さが正**であることが必要。

### 4.4 ColorCode / Tolerance / Duration
- `ColorCode`：RGB（#RRGGBB）を内部表現に変換
- `Percentage`：0〜100
- `Milliseconds`：0以上（※Timeoutをどう扱うかは Action ごとに定義）

### 4.5 VariableName / VariableRef
- UIで入力・選択される “変数名” を値オブジェクト化
- 本版は「保存先として指定できる」ことが前提（型は string/int/point 等が入る想定）

---

## 5. アクション体系（ドメイン型）

> 実装では `MacroAction` 抽象クラス + 派生型（レコードでも可）を推奨。  
> UIの各ダイアログは **Actionの編集UI** として位置づける。

### 5.1 Mouse
- `MouseClickAction`（button, clickType, relative, x, y）
- `MouseMoveAction`（relative, startX/Y, endX/Y, durationMs）
- `MouseWheelAction`（orientation, value）

### 5.2 Key
- `KeyPressAction`（option: Press/Down/Up, key, count）
- `HotkeyAction`（入力されたChordを KeyPress列へ展開する “編集操作” として扱う）
  - ドメイン的には **Hotkey自体を保存しない**でOK（保存するなら “元入力” と “展開結果” の整合ルールが必要）

### 5.3 Wait
- `WaitAction`（valueMs）
- `WaitForPixelColorAction`（x,y,color,tolerance,waitingMs,trueGoTo,falseGoTo）
- `WaitForScreenChangeAction`（searchArea(+rect), mouseAction?, saveCoordinate?, waitingMs, trueGoTo,falseGoTo）

### 5.4 Detection
- `FindImageAction`（searchArea(+rect), bitmapSource, tolerance, mouseAction?, saveCoordinate?, trueGoTo,falseGoTo, waitingMs）
- `FindTextOcrAction`（text, language, searchArea(+rect), mouseAction?, saveCoordinate?, trueGoTo,falseGoTo, waitingMs）

### 5.5 ControlFlow
- `RepeatAction`（startLabel, condition(Seconds/Repetitions/Until/Infinite), finishGoTo）
- `GoToAction`（target）
- `IfAction`（variableName, conditionType, value?, trueGoTo, falseGoTo）
- `EmbedMacroFileAction`（path）
- `ExecuteProgramAction`（path）

---

## 6. ドメイン不変条件（最重要）

### 6.1 Macro / Step の不変条件
1. **Stepの順序は 0..n-1 の連番**（重複なし）
2. Stepを削除/挿入したら、Orderは再採番される（UI表示順＝Order）
3. `StepLabel` は Macro内で **一意**

### 6.2 Label一意性ルール（生成規約）
- 入力されたLabel（トリム後）が既存と重複する場合、末尾に数字を付与して一意化する  
  - 例：`Jump先` が存在 → 新規は `Jump先1`  
  - `Jump先1` も存在 → `Jump先2` …
- すでに末尾が数字のLabelを複製/追加する場合は、末尾数字をインクリメントして一意化する  
  - 例：`Jump先2` が存在 → 新規は `Jump先3`

**推奨アルゴリズム**
- `basePart`（末尾連番を除いた部分）と `n`（末尾数値、なければ0）を抽出  
- `candidate = basePart + (n==0 ? "" : n)` を基準にし、存在する限り `n++` で探索

### 6.3 GoToTarget の不変条件
- `Start/Next/End` は常に有効
- `Label` は、Macro内に存在するLabelのみ指定可能
  - Label削除時は参照を壊さないように、編集操作で検知できること（例：検証エラー、または自動置換は別仕様）

### 6.4 SearchArea / Rect の不変条件
- SearchArea が Area 系の場合、Rect必須
- Rectは `width > 0 && height > 0`
  - `width = X2 - X1`、`height = Y2 - Y1`
- 座標はマルチモニタを考慮して負数も許容しうる（禁止しない）。ただし width/height は必須。

### 6.5 WaitForPixelColor の不変条件
- `Tolerance` は 0〜100（整数）
- `ColorCode` は #RRGGBB として解釈可能
- `waitingMs` は 0以上（0を許すかは仕様で固定。許す場合は “即時判定”）

### 6.6 FindImage / FindTextOcr の不変条件（要点）
- FindImage は bitmapSource が必須（Capture/File/Variable のいずれか）
- FindTextOcr は `text` が空でない
- `Test` ボタンは **UI操作**（ドメイン永続値ではない）として扱う

### 6.7 Repeat の不変条件
- Repeat条件（Seconds/Repetitions/Until/Infinite）は **排他**
- startLabel は Macro内に存在するLabel（または仕様で Start を許すなら別途定義）
- finishGoTo は Start/Next/End/Label のいずれかで有効

### 6.8 If の不変条件
- variableName は空でない
- conditionType により `value` の必須/不要が変わる
  - 例：Defined は value 不要
  - RegEx を採用する場合は value 必須（ただし FindText の RegEx は将来予定）

---

## 7. ドメインサービス（推奨）

### 7.1 LabelUniquenessService
- 入力Labelを受け取り、Macro内で一意なLabelを返す

### 7.2 MacroValidator
- Macro全体の参照整合性（Label参照、Rect妥当性等）を検証し、エラー一覧を返す  
  - UIは保存時/再生開始時に呼び出せる

### 7.3 GoToResolver（再生側）
- GoToTarget を “次の Step index” に解決する  
  - Next/End は境界処理が必要（Nextが最終行の次にならない等）

---

## 8. 状態遷移（アプリ層の責務）

ドメインは原則として「編集可能なデータ構造」を提供し、以下の状態制御はアプリ層（WinForms / Presenter / Application Service）で行う。

- Idle：編集可、再生可
- Recording：編集不可（記録以外）、Stop可
- Playing：編集不可、Stop/Cancel可、実行中ステップの自動選択追従（UI要件）

---

## 9. 受け入れ基準（TDD向けチェック観点）

- Labelを複製/追加しても Macro内で必ず一意になる
- GoTo の候補（Start/Next/End/Label一覧）が Macroの状態と常に一致する
- Area選択が必要な SearchArea で Rectが未設定の Action は検証で弾ける
- tolerance / color / waitingMs 等の範囲外入力はドメインで弾ける（例外 or Result）

---
以上
