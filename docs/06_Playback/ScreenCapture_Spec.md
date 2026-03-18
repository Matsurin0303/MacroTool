# 画面キャプチャ仕様（Playback / Detection）

- Version: **Macro_v1.0.0**
- 更新日: 2026-03-18
- 参照: `docs/04_Domain/Domain_Model.md` / `docs/05_UI/UI_Spec.md` / `docs/06_Playback/Playback_Spec.md`

---

## 1. 目的

本書は、Playback 中に `FindImage` / `FindTextOcr` / `WaitForPixelColor` が利用する**画面取得責務**を定義する。

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
- `X2 > X1` かつ `Y2 > Y1` を満たすこと

---

## 4. キャプチャ責務

### 4.1 ScreenCaptureService
- SearchArea から取得対象を解決する
- 必要な領域のビットマップを返す
- 必要に応じて座標系の変換情報を返す

### 4.2 検出系サービスとの分担
- OCR 実行自体は `OcrService` の責務
- 画像一致判定自体は `ImageFinderService` の責務
- ScreenCaptureService は入力画像の取得責務に限定する

---

## 5. 処理フロー

### 5.1 WaitForPixelColor
1. 対象 SearchArea を解決する
2. 指定座標の色を取得する
3. 条件一致まで待機またはタイムアウトする

### 5.2 FindImage
1. 対象 SearchArea を解決する
2. 領域画像を取得する
3. ImageFinderService へ入力する
4. 成功時は検出座標を返す

### 5.3 FindTextOcr
1. 対象 SearchArea を解決する
2. 領域画像を取得する
3. OcrService へ入力する
4. テキスト一致結果を返す

---

## 6. 座標系

- Domain上の `Rect` は画面上の矩形領域を表す
- Detection 成功時は、検索領域内座標ではなく**再生側で利用可能な画面座標**へ変換できることが望ましい
- 変換責務の最終配置は Infrastructure 実装で吸収する

---

## 7. FocusedWindow の扱い

- `FocusedWindow` は実行時点でフォーカス中のウィンドウを対象とする
- `AreaOfFocusedWindow` は、FocusedWindow 基準で部分矩形を取得する
- フォーカス取得不能時の扱いは ApplicationError 候補とする

---

## 8. タイミング

- 画面取得は Playback 開始時ではなく **各Step評価時** に行う
- 再利用キャッシュを持つかどうかは本版未確定とする
- 同一Step内での再試行は Action仕様に従う

---

## 9. エラー方針

- 取得不能、対象外座標、OCR入力不能は ApplicationError 候補とする
- 予期しないOS API失敗は SystemError 候補とする
- 停止要求を受けた場合は、次のキャンセル可能境界で中断する

---

## 10. テスト観点

- SearchArea 列挙値ごとの対象領域解決
- Rect 不正値の検証
- FocusedWindow 未取得時の失敗
- 検出座標をマウス操作へ引き渡せること
- Step評価時に最新画面を取得すること

---

## 11. 未確定事項

- マルチモニタ時の仮想スクリーン座標の扱い
- DPI差異吸収方法
- 最小キャプチャ単位と性能最適化
- 画像フォーマット統一方針

