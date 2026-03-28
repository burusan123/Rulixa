## Rulixa

`Rulixa` は、AI駆動開発における **トークン不足／暗黙仕様／巨大コードベース**の問題を、**契約（Contracts）＋索引（Index）＋Context Pack** に変換して解決するためのローカルツールです。

### ドキュメント（まずここ）

- `docs/project_full_spec.md`（完全版の正本）
- `docs/polaris.md`（北極星）
- `docs/spec/phase1/README.md`（Phase 1 仕様の入口）
- `docs/spec/phase1/scope.md`（Phase 1 スコープ）
- `docs/spec/phase1/architecture.md`（Frontend / Core 分離とプロジェクト分割）
- `docs/spec/phase1/ir.md`（Phase 1 IR）
- `docs/spec/phase1/entry_resolution.md`（entry 解決仕様）
- `docs/spec/phase1/wpf_net8_extraction_targets.md`（WPF + .NET 8 抽出対象）
- `docs/spec/phase1/context_pack_rules.md`（Context Pack ルール）
- `docs/spec/phase1/implementation_plan.md`（実装順と scan pipeline）
- `docs/spec/phase1/examples/assessmeister_shell_pack_example.md`（Pack 具体例）

### ソース構成（概要）

- `src/Rulixa.Core`：解析・契約抽出・索引・pack生成・セキュリティ・ライセンスチェック
- `src/Rulixa.Cli`：CLI入口（デバッグ/手動運用）
- `src/Rulixa.Mcp`：MCP入口（将来、Cursor/VS Codeから利用）

### 生成物

生成物は `artifacts/` 配下を既定とし、Git管理外です（`.gitignore`）。

Doc Pack（`docs/_generated/`）も生成物としてGit管理しません（CI artifact＋PRコメント運用）。  
CI定義は `/.github/workflows/docpack-ci.yml` を参照してください。

### Windowsでの文字化けについて（CLI）

Windowsのターミナル設定によっては、`--goal` に日本語を直接渡すと文字化けする可能性があります。  
その場合は **`--goalStdin`（推奨）** または **`--goalFile`** を使用してください。


