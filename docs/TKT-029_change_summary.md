---
date: 2026-03-18
title: TKT-029 Change Summary
version: Macro_v1.0.0
---

# TKT-029 修正サマリ

## 対応概要

互換ポリシーと Migration 仕様のうち、**Macro_v1.0.0 本版で実装対象とする範囲**だけが分かるように整理した。

## 反映内容

- `Compatibility_Policy.md` を **本版互換ポリシー** として全面見直し
- 本版の CSV 対応範囲を **Import / Export とも `CSV_v1.0` のみ** に固定
- 本版では **自動マイグレーションを行わない** と明記
- `CSV_v1_to_v2.md` を **将来予定 / 参考資料** へ降格
- `Version_Mapping_Table.md` を **Macro_v1.0.0 <-> CSV_v1.0 の単純対応表** に整理
- メタ行方式、段階的変換、複数スキーマ対応は **本版対象外** と明記

## 完了条件に対する結果

- 現在版で実装すべき互換範囲が明確になった
- 将来仕様と本版仕様が分離された

## 対象文書

- `02_Requirements/Compatibility_Policy.md`
- `07_FileFormats/Migrations/CSV_v1_to_v2.md`
- `07_FileFormats/Version_Mapping_Table.md`

## 補足

`CSV_v1_to_v2.md` は削除せず残しているが、**Macro_v1.0.0 本版では実装義務を持たない参考資料**として扱う。

以上
