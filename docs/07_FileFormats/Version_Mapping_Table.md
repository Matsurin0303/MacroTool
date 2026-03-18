---
date: 2026-03-18
title: Macro Version and CSV Schema Version Mapping
version: Macro_v1.0.0
---

# CSVバージョンとMacroバージョンの正式対応表（Macro_v1.0.0 本版）

## 1. 目的

本ドキュメントは、Macro のバージョンと、CSV スキーマの正式対応関係を
**本版の実装範囲に限定して**定義する。

## 2. 本版の正式対応

### 2.1 対応表

| Macroバージョン | 対応CSVスキーマ | Import | Export | 備考 |
|---|---|---|---|---|
| `Macro_v1.0.0` | `CSV_v1.0` | 対応 | 対応 | 本版の正式対象 |

### 2.2 本版の結論

- `Macro_v1.0.0` は **`CSV_v1.0` のみ** を正式サポートする
- Import / Export ともに `CSV_v1.0` 固定とする
- `CSV_v2.0` 以降との互換変換は **本版対象外** とする

## 3. スキーマ識別方法（本版）

本版では、以下のみを採用する。

- **ヘッダ完全一致方式**

以下は本版では採用しない。

- ファイル名による識別
- メタ行による識別
- 複数スキーマ候補からの推定

## 4. 互換性ポリシー（本版）

### 4.1 Import

- `CSV_v1.0` は受け付ける
- `CSV_v1.0` 以外は受け付けない
- 自動マイグレーションは行わない

### 4.2 Export

- 常に `CSV_v1.0` で出力する
- 旧版 / 新版向けの切替出力は行わない

## 5. 将来予定

以下は将来、必要になった時点で別途仕様化する。

- `CSV_v2.0` 以降の正式対応表
- `CSV_v1.0 -> CSV_v2.0` 自動変換
- メタ行方式の採用
- スキーマバージョン間の互換保証ルール

現時点では、上記はいずれも **参考検討事項** に留める。

## 6. 関連仕様

- `docs/07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `docs/07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `docs/02_Requirements/Compatibility_Policy.md`
- `docs/07_FileFormats/Migrations/CSV_v1_to_v2.md`（将来予定）

以上
