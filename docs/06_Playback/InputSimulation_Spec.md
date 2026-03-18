# 入力シミュレーション仕様（Playback）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 参照: `docs/06_Playback/Playback_Spec.md` / `docs/04_Domain/Domain_Model.md` / `docs/05_UI/UI_Spec.md`

---

## 1. 目的

本書は、Playback 中に実行される **マウス / キーボード入力送信** の責務と最低限の動作ルールを定義する。

---

## 2. 対象

### 2.1 本版対象Action
- `MouseClick`
- `MouseMove`
- `MouseWheel`
- `KeyPress`

### 2.2 本版対象外
- 録画時の入力フック詳細
- 将来機能向けの特殊入力
- UIホットキー設定画面の保存形式詳細

---

## 3. 役割分担

### 3.1 PlaybackEngine
- Stepの評価順を制御する
- 停止要求の監視を行う
- InputSimulation の呼び出しタイミングを決める

### 3.2 InputSimulationService
- OSへ入力を送信する
- Mouse / Keyboard 送信方式を隠蔽する
- 必要に応じてキャンセル可能境界を提供する

---

## 4. 基本ルール

- 入力送信は Playback 中のみ実行する。
- `StopRequested` 受領後は、次のキャンセル可能境界以降の入力送信を開始しない。
- 送信失敗は Application / Playback 側へ異常として返す。
- 公開状態は `Idle / Playing` のみであり、入力送信層は Pause 状態を持たない。

---

## 5. マウス入力

### 5.1 MouseClick
- `MouseButton` は `Left / Right / Middle / SideButton1 / SideButton2` を受け付ける。
- `ClickType` は `Click / DoubleClick / Down / Up` を受け付ける。
- クリック位置は対象Actionの座標解決後に決定する。

### 5.2 MouseMove
- 指定座標へカーソルを移動する。
- `Duration` を持つ場合は、その時間を使って移動演出を行う。
- Duration の補間方式詳細は本版未確定とする。

### 5.3 MouseWheel
- 指定方向と回転量に応じてホイール入力を送信する。
- OS依存のデルタ換算は Infrastructure が吸収する。

---

## 6. キーボード入力

### 6.1 KeyPress
- 単一キーまたは修飾キーを含む組合せを送信する。
- 押下順 / 解放順は OS 送信規則に従う。
- 不正なキー組合せは Domain / Application で事前検証する。

### 6.2 Hotkey
- UI入力としては存在するが、保存時および再生時は `KeyPress` 群へ展開して扱う。
- ファイル形式に `Hotkey` Action は保存しない。

---

## 7. 座標解決

- `MouseClick` / `MouseMove` の座標は Playback 開始時ではなく **Step実行時** に解決する。
- `FindImage` / `FindTextOcr` と連動する場合は、検出結果座標を先に確定させる。
- `MousePosition` は `Center / TopLeft / TopRight / BottomLeft / BottomRight` を使用する。

---

## 8. Playback 設定との関係

### 8.1 反映対象
以下の設定は Playback 実行制御に影響する。
- Block key presses during playback
- Abort playback on key press
- Abort playback on mouse move
- Restore mouse position after playback

### 8.2 未確定
以下は本版で存在するが、入力送信との厳密な適用ルールは未確定とする。
- Use relative mouse positions
- Playback Speed の適用範囲

---

## 9. エラーと停止

- 入力送信不能は `StepErrored` の候補とする。
- ユーザー入力検知による停止は `Aborted` 終了理由で扱う。
- Stop 操作は `Cancelled` 終了理由で扱う。

---

## 10. テスト観点

- Button / ClickType の列挙値検証
- 修飾キーを含む `KeyPress` 送信順
- 停止要求後に新規入力送信が始まらないこと
- Restore mouse position 設定の反映
- 検出結果座標を使ったマウス位置解決

---

## 11. 未確定事項

- 送信APIの最終選定（SendInput 等）
- ダブルクリックの間隔値
- MouseMove の補間方式
- マルチモニタ / DPI差異への補正方式

