# TKT-025 修正サマリ

- 対象チケット: **TKT-025 Playback Speed の適用範囲**
- 反映日: 2026-03-18
- 決定内容:
  - 1: A `Playback Speed` を適用する対象は **待機系のみ**
  - 2: A `Wait` は **指定時間を Playback Speed で倍率変換**する
  - 3: C 検出系待機 (`FindImage` / `FindText` / `WaitForTextInput`) のタイムアウト・ポーリング間隔には **適用しない**
  - 4: A 入力系Action (`MouseClick` / `MouseMove` / `MouseWheel` / `KeyPress`) には **適用しない**

## 修正対象
- `02_Requirements/Functional_Spec.md`
- `06_Playback/InputSimulation_Spec.md`
- `06_Playback/Playback_Spec.md`

## 変更概要
1. `Functional_Spec.md`
   - `3-4-1 Playback Speed` の備考へ適用範囲を追記
   - `Wait` Action にのみ適用し、検出系待機・入力系Actionには適用しないことを明文化

2. `InputSimulation_Spec.md`
   - これまで未確定だった `Playback Speed` の適用ルールを確定
   - `Wait` の実効待機時間の決定方法を追記
   - 検出系待機と入力系Actionへ適用しないことを明記
   - テスト観点へ `Playback Speed` 反映確認を追加

3. `Playback_Spec.md`
   - Playback 全体ルールとして `Playback Speed` 適用範囲を追加

## 完了条件への対応
- `Playback Speed` の適用範囲が仕様書上で一意に読める
- `Wait` のみへ適用されることが明文化されている
- 検出系待機と入力系Actionへ適用しないことが明文化されている
- 主要文書間で矛盾がない
