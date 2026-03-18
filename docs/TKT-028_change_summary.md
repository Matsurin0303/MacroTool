# TKT-028 修正サマリ

## 対応概要
設定項目の説明文を `Macro仕様書_v7.xlsx` と照合し、**項目名と意味が一致していない文言** を修正した。

## 主な修正点
- `Abort playback on mouse move` の説明を `マウス移動で再生を止める` に修正
- `Restore window sizes` の説明を `再生後にウィンドウサイズを元に戻す` に修正
- `Use relative mouse positions` の説明を `マウス位置を相対位置として扱う` に修正
- `Reset variables and list counter on each playback cycle` の説明を TKT-026 の確定内容に合わせて修正
- `Start/append recording` の説明を `記録を開始、または現在のMacroへ追記記録する` に修正
- `Stop` の説明を `記録または再生を停止` に修正
- `Playback` の説明を `現在の再生対象を再生` に修正
- `Show delete confirmation` の説明を `削除前に確認ダイアログを表示` に修正
- `UI_Spec.md` に、Settings の文言は項目名優先で扱う旨の共通ルールを追加

## 主な反映先
- `02_Requirements/Functional_Spec.md`
- `05_UI/UI_Spec.md`

## 補足
- `Minimum mouse movement` と `minimum wait time` は、v7 自体の表現に不自然さがあるが、単位・補足説明付きで一応意味が通るため、本チケットでは名称変更までは行っていない。
- `Use relative mouse positions` の厳密な基準座標ルールは、本版では未確定のままとした。
