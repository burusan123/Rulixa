# Phase 1 仕様

このフォルダは、`Rulixa` の Phase 1 実装仕様を分割して管理するための場所です。

現時点の Phase 1 は、**Windows 上の `WPF + .NET 8` アプリケーションに対して、AI 用の最小 Context Pack を決定的に生成すること**を主目的とします。

このフォルダ配下の文書数は、**多くても 10 本以下**に抑える方針とします。  
理由は、仕様が分散し過ぎると誰も全体像を読まなくなるためです。

## 位置づけ

- 正本: [`docs/project_full_spec.md`](D:/C#/Rulixa/docs/project_full_spec.md)
- 北極星: [`docs/polaris.md`](D:/C#/Rulixa/docs/polaris.md)
- このフォルダ: Phase 1 の具体仕様

差分が出た場合の優先順位は次です。

1. `project_full_spec.md`
2. `polaris.md`
3. `docs/spec/phase1/*`

## 文書一覧

- [`scope.md`](D:/C#/Rulixa/docs/spec/phase1/scope.md)
  Phase 1 の目標、対象範囲、非目標、成功条件
- [`architecture.md`](D:/C#/Rulixa/docs/spec/phase1/architecture.md)
  Frontend / Core 分離、依存方向、推奨プロジェクト分割
- [`ir.md`](D:/C#/Rulixa/docs/spec/phase1/ir.md)
  Phase 1 の共通 IR
- [`entry_resolution.md`](D:/C#/Rulixa/docs/spec/phase1/entry_resolution.md)
  `entry=file/symbol/auto` の解決仕様
- [`wpf_net8_extraction_targets.md`](D:/C#/Rulixa/docs/spec/phase1/wpf_net8_extraction_targets.md)
  `WPF + .NET 8` 向けに最初に抽出すべき対象と契約
- [`context_pack_rules.md`](D:/C#/Rulixa/docs/spec/phase1/context_pack_rules.md)
  Phase 1 の Context Pack 組み立てルール
- [`implementation_plan.md`](D:/C#/Rulixa/docs/spec/phase1/implementation_plan.md)
  プロジェクト作成順、scan pipeline、最初のユースケース
- [`examples/assessmeister_shell_pack_example.md`](D:/C#/Rulixa/docs/spec/phase1/examples/assessmeister_shell_pack_example.md)
  `AssessMeister` を題材にした Pack 例

## 現時点の前提

- 主戦場は `WPF + .NET 8`
- 入口はまず `VS Code / Cursor` を想定するが、Core の入出力仕様はフロント非依存にする
- 対象コードベースの代表観察例として `AssessMeister` を参照した
- フロントエンドとコアは分離し、対象技術固有解析はプラグインとして外側に置く
