# TKT-008 修正サマリ

## 対応概要
`02_Requirements/Functional_Spec.md` の Repeat 画面仕様を、`Macro仕様書_v7` の UI 粒度に合わせて補強した。

## 反映内容
- Repeat条件を `Seconds / repetitions / Until / Infinite` の4つの RadioButton で排他運用と明記
- 各条件に対応する入力欄を UI 部品単位で分離したまま、活性条件を追記
- `Infinite` 選択時は `Seconds / repetitions / Until` 入力欄を非活性と明記
- Repeat概要にも「選択中の条件に対応する入力欄のみ活性化する」ルールを追記

## 完了条件への対応
- v7 の UI 粒度で記載されている
- RadioButton と入力欄が部品単位で分離されている
- 入力条件・活性条件が明記されている

## 主な更新ファイル
- `02_Requirements/Functional_Spec.md`
