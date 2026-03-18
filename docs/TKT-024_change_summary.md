# TKT-024 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-024
- 概要: `FindImage` / `FindTextOcr` の詳細挙動を追加し、Tolerance の意味、複数候補時の採用規則、OCR一致規則、成功座標の扱い、タイムアウトとエラーの境界を明文化した。

---

## 1. 主な反映内容

### 1.1 Detection 仕様の明文化
- `FindImage` の `Tolerance` を **許容差** とし、`0` が最も厳密、`100` が最も緩いと定義
- `FindImage` は複数候補時に **一致度最大** を採用し、同点時は **左上優先** と定義
- `FindTextOcr` は **前後空白を除去して完全一致** と定義
- `FindTextOcr` は複数候補時に **最も左上** の候補を採用すると定義

### 1.2 成功座標の共通化
- 検出成功時の代表点は `MousePosition` で算出すると定義
- 上記代表点を **マウス操作** と **Save Coordinate** の双方で共通利用すると定義

### 1.3 タイムアウトと異常の切り分け
- `WaitingMs` 経過による未検出は **エラーではなく `FalseGoTo` 側への通常分岐** と定義
- OCR実行失敗、画像読込失敗、画像検出サービス失敗は **ApplicationError / StepErrored** 候補と整理
- 再試行時は **各ポーリング時点の最新画面** を使うと定義

### 1.4 関連文書の整合
- `Functional_Spec.md` の Find image / Find text (OCR) に詳細挙動を反映
- `Domain_Model.md` に Detection の意味論を反映
- `Playback_Spec.md` と `ScreenCapture_Spec.md` に実行時ルールを反映
- `Macro_FileFormat_Spec.md` と `CSV_Schema_v1.0.md` に保存形式側の意味を反映
- `Error_Handling_Policy.md` にタイムアウトと処理異常の扱いを反映

---

## 2. 更新ファイル

- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Error_Handling_Policy.md`
- `04_Domain/Domain_Model.md`
- `06_Playback/Playback_Spec.md`
- `06_Playback/ScreenCapture_Spec.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`
- `07_FileFormats/CSV_Schema_v1.0.md`

---

## 3. 補足

- 本対応では OCR エンジン自体の精度改善、画像検出アルゴリズムの内部実装、キャッシュ最適化方針までは定義していない
- `BitmapKind=CapturedBitmap` の CSV 上の具体表現は引き続き未確定とする
