# 画面キャプチャ仕様（Playback / Detection）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 参照: `docs/04_Domain/Domain_Model.md` / `docs/05_UI/UI_Spec.md` / `docs/06_Playback/Playback_Spec.md`

---

## 1. 目的

本書は、Playback 中に `FindImage` / `FindTextOcr` / `WaitForPixelColor` が利用する**画面取得責務**と、`Define` による検索領域指定方法を定義する。

---

## 2. 利用箇所

画面キャプチャは少なくとも以下のActionで使用する。
- `WaitForPixelColor`
- `FindImage`
- `FindTextOcr`

本版対象外の `CaptureText` / `CaptureImage` には適用しない。

---

## 3. SearchArea

### 3.1 列挙値
- `EntireDesktop`
- `AreaOfDesktop`
- `FocusedWindow`
- `AreaOfFocusedWindow`

### 3.2 Rect
- `AreaOfDesktop` / `AreaOfFocusedWindow` の場合は `Rect` 必須
- `Rect` は `X1, Y1, X2, Y2` で定義する
- 保存単位は**物理ピクセル**とする
- `X1, Y1` は左上、`X2, Y2` は右下を表す
- 保存時は `X1=min(startX,endX)`、`Y1=min(startY,endY)`、`X2=max(startX,endX)`、`Y2=max(startY,endY)` に**正規化**する
- `X2 > X1` かつ `Y2 > Y1` を満たすこと
- 幅0または高さ0の矩形は無効とし、確定しない

### 3.3 座標原点
- `EntireDesktop` / `AreaOfDesktop` は**仮想デスクトップ基準**の座標を使用する
- `FocusedWindow` / `AreaOfFocusedWindow` は**実行時点でフォーカス中のウィンドウ**を対象とする
- `AreaOfFocusedWindow` の `Rect` は**フォーカス中ウィンドウの外枠左上**を `(0,0)` とする
- 仮想デスクトップ基準では、マルチモニタ配置により負座標を取り得る

---

## 4. Define 操作仕様

### 4.1 開始条件
- `SearchArea` が `AreaOfDesktop` または `AreaOfFocusedWindow` の場合に `Define` を使用できる
- `Define` 開始時は、現在選択中の `SearchArea` に応じた座標系で矩形を取得する

### 4.2 操作手順
1. ユーザーが `Define` を押下する
2. 画面上でドラッグ開始点を取得する
3. ドラッグ終了点を取得する
4. 開始点と終了点から矩形を生成する
5. 正規化後の `X1, Y1, X2, Y2` をUIへ反映する

### 4.3 表示ルール
- `Define` 中は**背景を見えるまま**にする
- 選択矩形は**赤枠のみ表示**し、**枠内は透過**とする
- これにより背景位置を視認できること

### 4.4 キャンセル
- `Esc` でキャンセルできる
- 右クリックでもキャンセルできる
- キャンセル時は `X1, Y1, X2, Y2` を更新しない

### 4.5 完了条件
- ドラッグで有効矩形が作成された場合のみ確定する
- 幅0または高さ0の場合はキャンセル扱いとし、値を更新しない

---

## 5. マルチモニタ / DPI

### 5.1 マルチモニタ
- `AreaOfDesktop` の `Define` は**モニタをまたぐドラッグを許可**する
- 取得結果は仮想デスクトップ基準の物理ピクセル座標として保持する
- モニタまたぎにより負座標や大きな座標値を取り得る

### 5.2 DPI
- Domain / FileFormat に保存する座標は**物理ピクセル**とする
- DIP / 論理座標への正規化は行わない
- OS API やUI入力が論理座標を返す場合、Infrastructure 層で物理ピクセルへ変換してから Domain へ渡す
- 再生時のキャプチャも同じ物理ピクセル基準で評価する

---

## 6. キャプチャ責務

### 6.1 ScreenCaptureService
- `SearchArea` から取得対象を解決する
- 必要な領域のビットマップを返す
- 必要に応じて座標系の変換情報を返す

### 6.2 検出系サービスとの分担
- OCR 実行自体は `OcrService` の責務
- 画像一致判定自体は `ImageFinderService` の責務
- `ScreenCaptureService` は入力画像の取得責務に限定する

---

## 7. 処理フロー

### 7.1 WaitForPixelColor
1. 対象 `SearchArea` を解決する
2. 指定座標の色を取得する
3. 条件一致まで待機またはタイムアウトする

### 7.2 FindImage
1. 対象 `SearchArea` を解決する
2. 領域画像を取得する
3. `ImageFinderService` へ入力する
4. 成功時は検出矩形と代表点を返す
- `Tolerance` は許容差とし、`0` が最も厳密、`100` が最も緩い
- 複数候補が検出された場合は、一致度が最も高い候補を採用し、同点時は左上の候補を採用する
- 代表点は `MousePosition` (`Center / TopLeft / TopRight / BottomLeft / BottomRight`) により検出矩形から算出する
- 代表点は、後続のマウス操作と `Save Coordinate` 保存の双方で共通利用する

### 7.3 FindTextOcr
1. 対象 `SearchArea` を解決する
2. 領域画像を取得する
3. `OcrService` へ入力する
4. テキスト一致結果を返す
- OCR で得た候補文字列は、前後空白を除去したうえで完全一致判定する
- 複数候補が一致した場合は、最も左上の候補を採用する
- 代表点は `MousePosition` により検出矩形から算出する
- 代表点は、後続のマウス操作と `Save Coordinate` 保存の双方で共通利用する

---

## 8. 座標変換

- Domain上の `Rect` は検索対象の矩形領域を表す
- `AreaOfDesktop` の `Rect` は仮想デスクトップ基準の物理ピクセルとして扱う
- `AreaOfFocusedWindow` の `Rect` は外枠左上基準の物理ピクセルとして扱う
- Detection 成功時は、検索領域内座標ではなく**再生側で利用可能な画面座標**へ変換できること
- `AreaOfFocusedWindow` で得た相対座標を画面座標へ変換する責務は Infrastructure 実装が持つ

---

## 9. FocusedWindow の扱い

- `FocusedWindow` は実行時点でフォーカス中のウィンドウを対象とする
- `AreaOfFocusedWindow` は、FocusedWindow の外枠左上基準で部分矩形を取得する
- フォーカス取得不能時の扱いは `ApplicationError` 候補とする

---

## 10. タイミング

- 画面取得は Playback 開始時ではなく **各Step評価時** に行う
- 再利用キャッシュを持つかどうかは本版未確定とする
- 同一Step内での再試行は Action仕様に従う
- `FindImage` / `FindTextOcr` の再試行は、各ポーリング時点の**最新画面**を用いて行う
- `WaitingMs` 経過まで未検出の場合は、タイムアウトをエラーにせず `FalseGoTo` 側へ分岐する

---

## 11. エラー方針

- 取得不能、対象外座標、OCR入力不能は `ApplicationError` 候補とする
- `FindImage` の画像読込失敗、`ImageFinderService` 実行失敗、`OcrService` 実行失敗は `ApplicationError` 候補とする
- `WaitingMs` 経過まで未検出だった場合はエラーにせず、Action仕様どおり `FalseGoTo` へ分岐する
- `Define` のキャンセル、0サイズ矩形、値未更新はユーザーキャンセル扱いとしエラーにしない
- 予期しないOS API失敗は `SystemError` 候補とする
- 停止要求を受けた場合は、次のキャンセル可能境界で中断する

---

## 12. テスト観点

- `SearchArea` 列挙値ごとの対象領域解決
- `Rect` 正規化 (`min/max`) の検証
- `AreaOfFocusedWindow` の外枠左上原点の検証
- `Define` の `Esc` / 右クリックキャンセル
- モニタまたぎドラッグで矩形が保持されること
- 負座標を含む仮想デスクトップ矩形の保持
- DPI差異環境でも物理ピクセル基準で一貫すること
- FocusedWindow 未取得時の失敗
- 検出座標をマウス操作へ引き渡せること
- Step評価時に最新画面を取得すること
