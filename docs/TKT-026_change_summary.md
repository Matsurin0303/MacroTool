# TKT-026 修正サマリ

- 対応日: 2026-03-18
- 対象: Playback Repeat と Repeat Action の関係整理

## 反映内容
- Playback Repeat は **現在の再生対象全体** を 1 cycle として繰り返すよう定義した
- 通常再生時はマクロ全体、`Play until selected` / `Play from selected` / `Play selected` ではその対象範囲全体を 1 cycle とするよう明記した
- Macro 内の `Repeat` Action は、Playback Repeat が有効でも **内部制御としてそのまま有効** とした
- `Repeat(Infinite)` がある場合は **内側の Repeat が優先** され、外側の Playback Repeat の次 cycle へ到達しないと明記した
- `Reset variables and list counter on each playback cycle` は、**Playback Repeat の各 cycle** に対して適用し、Repeat Action の内部周回には適用しないよう修正した

## 主な更新ファイル
- `02_Requirements/Functional_Spec.md`
- `04_Domain/Domain_Model.md`
- `06_Playback/Playback_Spec.md`

## 完了条件
- Playback Repeat の適用単位が文書上で一意になっている
- Repeat Action との優先関係が文書上で明確になっている
- Playback cycle 初期化の対象が、Repeat Action 内部周回ではなく Playback Repeat cycle であることが明記されている
