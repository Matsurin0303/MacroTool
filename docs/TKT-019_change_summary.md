# TKT-019 修正サマリ

- 対応日: 2026-03-18
- 対象チケット: TKT-019
- 内容: Recent Files の更新ルールを確定し、関連仕様書へ反映

## 今回確定したルール

1. `Open` / `Recent Files` で **正常に読込完了した場合のみ** Recent Files を更新する。
2. `Save` / `Save As` の**成功時**も保存先ファイルを Recent Files の先頭へ更新する。
3. 重複パスは **既存項目を削除して先頭へ再追加** する。
4. `Recent Files` から存在しないファイルを選択した場合は **エラー表示し、該当項目を一覧から削除** する。
5. 読込失敗・保存失敗時は、失敗した操作結果で Recent Files を更新しない。
6. 保持件数は **最大10件** とする。

## 主な修正ファイル

- `02_Requirements/Functional_Spec.md`
- `03_Architecture/Application_Service_Spec.md`
- `03_Architecture/Architecture_Overview.md`
- `03_Architecture/Error_Handling_Policy.md`
- `05_UI/UI_Spec.md`

## 反映ポイント

### 1. Functional_Spec
- `1-3 Recent Files` の動作説明と備考へ更新規則を追記
- `2.4 Recent Files 仕様` を追加

### 2. Application_Service_Spec
- `UC-02 Open` 成功時に Recent Files を先頭更新することを追加
- `UC-03 Save` / `UC-04 Save As` 成功時に Recent Files を先頭更新することを追加
- `Recent Files` から存在しないファイル選択時は ApplicationError とし、該当項目を削除することを追加

### 3. Architecture_Overview
- Open / Save 代表フローに Recent Files 更新を追加

### 4. Error_Handling_Policy
- 読込失敗時は Recent Files を更新しないことを追加
- 保存成功時のみ Recent Files を更新することを追加

### 5. UI_Spec
- `3.4 Recent Files 共通ルール` を追加
- 未確定事項から `Recent Files の更新規則` を削除

## 未確定のまま残した事項

- `Schedule macro` の詳細挙動
