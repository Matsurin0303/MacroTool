# TKT-013 修正サマリ

- 更新日: 2026-03-18
- 対象: 空の重要仕様補完

## 1. 目的

TKT-013 に対応し、空ファイルだった重要仕様書を、既存仕様との整合を保ちながら最小限の実装可能な内容で補完した。

## 2. 補完した文書

- `02_Requirements/NonFunctional_Spec.md`
- `03_Architecture/Architecture_Overview.md`
- `03_Architecture/Error_Handling_Policy.md`
- `06_Playback/InputSimulation_Spec.md`
- `06_Playback/Playback_StateMachine.md`
- `06_Playback/ScreenCapture_Spec.md`

## 3. 補完方針

- `Macro仕様書_v7.xlsx` と既存 md 仕様で確定している内容のみ記載
- 将来機能や未確定事項は決め打ちせず、`未確定事項` として明示
- 既存の `Functional_Spec.md` / `Application_Service_Spec.md` / `Domain_Model.md` / `Playback_Spec.md` と矛盾しないよう整理

## 4. 主な反映内容

### 4.1 NonFunctional_Spec
- DDD + TDD、WinForms、JSON/CSV、予測可能な再生、診断容易性を明文化
- 数値SLAや性能閾値は未確定として分離

### 4.2 Architecture_Overview
- UI / Application / Domain / Infrastructure の4層構成を明記
- Open/Save、Import/Export、Playback の代表フローを追加

### 4.3 Error_Handling_Policy
- DomainError / ApplicationError / SystemError を定義
- Save、Import、Playback などユースケース別の扱いを整理

### 4.4 InputSimulation_Spec
- MouseClick / MouseMove / MouseWheel / KeyPress の責務分離を明記
- Hotkey は保存時に KeyPress 群へ展開する方針を再確認

### 4.5 Playback_StateMachine
- `Idle / Playing` のみを公開状態とする詳細ルールを補強
- イベント、ガード条件、終了理由、部分再生ルールを整理

### 4.6 ScreenCapture_Spec
- SearchArea、Rect、FindImage / FindTextOcr / WaitForPixelColor との関係を整理
- 画面取得と OCR / 画像一致判定の責務分離を明記

## 5. 今回あえて確定していない項目

- 性能目標値
- ログ出力先・保持期間
- Scheduler の実装方式
- 送信APIの最終選定
- マルチモニタ / DPI差異の厳密ルール

