# Rulixa Product Readiness

## 目的

この文書は、`Rulixa` を「試作」から「市販できるレベル」へ引き上げるための出荷判定チェックリストである。  
Phase 1〜4 の仕様書は機能進化を扱うが、本書は **製品として何が揃っていればよいか** を確認するために使う。

## 前提

- `Rulixa` の主価値は「全文検索の代替」ではない
- 主価値は「LLM が短いコンテキストで理解を始められる高密度な地図を返すこと」
- そのため、出荷判定では賢さだけでなく
  - 互換性
  - 安定性
  - 診断性
  - 再現性
  - サポート可能性
  を同等に扱う

## 出荷判定の原則

- `modern WPF で強い` だけでは不可
- `legacy WPF でも落ちない` ことが必要
- 未対応構成でも crash せず、partial pack と diagnostics を返せること
- 同じ入力で同じ pack と diagnostics が出ること
- 「分からないのに分かったふり」をしないこと

## KPI

| 指標 | 目的 | 最低条件 | 証跡 |
|---|---|---|---|
| `pack success rate` | pack が返る率 | modern / legacy corpus の両方で継続計測されている | smoke / regression レポート |
| `partial pack rate` | degraded でも有用な出力が返る率 | failure と区別して計測されている | diagnostics / evidence |
| `crash-free rate` | 例外で落ちない率 | release gate に含まれている | CI / optional smoke |
| `first useful map time` | 初動理解の速さ | 大規模 workspace でも許容時間内 | benchmark |
| `unknown guidance hit rate` | handoff 品質 | unknown から全文検索で正答に届くかを検証 | acceptance |
| `false confidence rate` | 分かったふりの抑制 | 低いことを確認する | golden review |
| `deterministic rate` | 再現性 | 同じ入力で同じ結果が出る | regression |

## チェックリスト

### 1. 技術

- [ ] modern WPF + DI 構成で system pack が安定して返る
- [ ] legacy WPF + code-behind 構成で crash しない
- [ ] `App.xaml StartupUri` を root 解決に使える
- [ ] `DataContext = new XxxViewModel()` を root / binding 解決に使える
- [ ] service locator / static resolver を limited support できる
- [ ] `new Window()` / `ShowDialog()` を route 候補として拾える
- [ ] ResourceDictionary / merged dictionaries で parse failure になりにくい
- [ ] partial class / partial record の統合が安定している
- [ ] unsupported construct で top-level failure ではなく degraded pack を返せる
- [ ] diagnostics が failure reason と next candidates を返せる
- [ ] false confidence を抑制できている
- [ ] compare-evidence で改善内容を追える

### 2. 品質保証

- [ ] modern corpus がある
- [ ] legacy corpus がある
- [ ] synthetic fixture が root / dialog / service locator / code-behind を含む
- [ ] `AssessMeister` の acceptance がある
- [ ] `AssessMeister_20260204` の acceptance がある
- [ ] pack 本文の golden test がある
- [ ] diagnostics の golden test がある
- [ ] decision trace の regression test がある
- [ ] deterministic regression がある
- [ ] failure taxonomy が固定されている
- [ ] release ごとに KPI を記録できる

### 3. UX

- [ ] plugin 説明が日本語で一貫している
- [ ] `pack -> 必要時のみ全文検索` の使い分けが明確
- [ ] `entry=file` / `entry=symbol` の選び方が説明されている
- [ ] root entry では system map が先に読める
- [ ] unknowns を「次の探索ガイド」として読める
- [ ] diagnostics が exception text ではなく説明文になっている
- [ ] 初回ユーザーが `ShellViewModel` や main screen を起点に選びやすい
- [ ] 成功時の期待値が正しく伝わる
  - 「全実装を説明する」ではなく「高密度な地図を返す」

### 4. サポート運用

- [ ] issue template がある
- [ ] 再現に必要な入力が定義されている
  - workspace
  - entry
  - goal
  - diagnostics
  - evidence bundle
- [ ] diagnostics から再現調査を始められる
- [ ] degraded pack をサポート時に説明できる
- [ ] evidence bundle の保全方針がある
- [ ] 対応済み / 部分対応 / 未対応の互換性表がある
- [ ] release note で互換性改善を追える

## フェーズとの関係

- Phase 1
  基盤と evidence
- Phase 2
  高シグナル抽出
- Phase 3
  system pack
- Phase 4
  legacy WPF compatibility

本書は Phase 4 の次に参照するものではなく、Phase 4 を実装しながら継続的に更新する。

## 優先順位

1. crash-free
2. legacy compatibility
3. diagnostics quality
4. acceptance corpus
5. UX / supportability
6. deeper drilldown

## メモ

- `mode 分離` や `deep drilldown` は重要だが、product readiness の観点では crash-free と互換性の後に来る
- 市販レベルを目指すなら、まず「使える workspace の範囲」と「壊れたときの説明可能性」を完成させる
