# スレッドモデル仕様（WinForms）

## 基本方針
- UIスレッド: 画面更新のみ
- Worker: 再生/キャプチャ/OCR/入力送出など重い処理
- Cancel: CancellationTokenを全経路で伝播

## 禁止事項
- UIスレッドでの長時間処理（X ms以上）
- Workerから直接Control操作（必ずInvoke/BeginInvoke）

## 再生中のUI更新
- 実行中ステップの自動選択
- 進捗表示
- 停止/一時停止の受付