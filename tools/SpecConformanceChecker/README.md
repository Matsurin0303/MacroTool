# SpecConformanceChecker

`master` ブランチのソースコード（`src/` 配下）が、`docs/` に格納された仕様書通りに実装されているかを検証する C# コンソールアプリです。

## ビルドと実行

```bash
# ビルド
dotnet build tools/SpecConformanceChecker/

# 実行（リポジトリルートから）
dotnet run --project tools/SpecConformanceChecker/ -- /path/to/MacroTool

# リポジトリルートで実行（自動検索）
cd /path/to/MacroTool
dotnet run --project tools/SpecConformanceChecker/
```

## 検証内容

| チェック | 内容 |
|---------|------|
| CHECK 1 | レイヤー構成と依存方向（`.csproj` の `ProjectReference` を解析） |
| CHECK 2 | ドメインモデルの存在確認（`Macro`, `MacroStep`, 値オブジェクト群） |
| CHECK 3 | Action 体系の網羅性（14 種類の `MacroAction` 派生クラス） |
| CHECK 4 | Application Service の存在と UseCase メソッドの確認 |
| CHECK 5 | 不変条件バリデーションの実装有無 |
| CHECK 6 | Playback 状態管理の実装有無 |

## 出力形式

```
=== MacroTool 仕様書 vs ソースコード 整合性チェック ===

[CHECK 1: レイヤー構成と依存方向]
  ✅ PASS: ...
  ❌ FAIL: ...
  ⚠️  WARN: ...

[SUMMARY]
Total  : XX checks
Passed : XX
Failed : XX
Warnings: XX
```

## 終了コード

| コード | 意味 |
|--------|------|
| `0` | FAIL が 0 件（WARN のみ可） |
| `1` | FAIL が 1 件以上 |
