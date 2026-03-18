# TKT-023 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-023
- 概要: `ScreenCapture` の操作仕様を追加し、`Define` 操作・座標系・DPI・マルチモニタ・キャンセル方法を明文化した。

---

## 1. 主な反映内容

### 1.1 ScreenCapture_Spec の拡充
- `Define` の開始条件、操作手順、完了条件を追加
- `Esc` または右クリックでキャンセルできることを追加
- `Define` 中は背景を見えるままにし、赤枠のみ表示、枠内は透過と定義
- ドラッグ開始点 / 終了点から矩形を生成し、`X1/Y1/X2/Y2` を min/max へ正規化するルールを追加
- 幅0または高さ0の矩形は確定しないことを追加

### 1.2 座標系の確定
- 保存座標は **物理ピクセル** と確定
- `AreaOfDesktop` は **仮想デスクトップ基準** と確定
- `AreaOfFocusedWindow` は **フォーカス中ウィンドウ外枠左上基準** と確定
- 仮想デスクトップでは負座標を取り得ることを追加

### 1.3 マルチモニタ / DPI
- `AreaOfDesktop` の Define は **モニタをまたぐドラッグを許可** と確定
- Domain / FileFormat は DIP ではなく **物理ピクセル保持** と明記
- 論理座標から物理ピクセルへの変換責務を Infrastructure 側へ明記

### 1.4 関連文書の整合
- `Functional_Spec.md` の `Find image` / `Find text (OCR)` に Define 操作・座標基準・キャンセル方法を反映
- `Domain_Model.md` の `SearchArea` / `Rect` 説明へ座標基準と正規化を反映
- `Macro_FileFormat_Spec.md` と `CSV_Schema_v1.0.md` に物理ピクセル基準と `AreaOfFocusedWindow` 原点を反映

---

## 2. 更新ファイル

- `06_Playback/ScreenCapture_Spec.md`
- `02_Requirements/Functional_Spec.md`
- `04_Domain/Domain_Model.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`
- `07_FileFormats/CSV_Schema_v1.0.md`

---

## 3. 補足

- `Define` の見た目は、既存の議論どおり **背景を見えるままにし、赤枠のみ表示、枠内は透過** とした
- 本対応では OCR 精度、画像一致アルゴリズム、最適化キャッシュ方針までは変更していない
