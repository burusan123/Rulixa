# Rulixa Product Readiness

## 現在地

`Rulixa` は、WPF / .NET ワークスペース理解の入口を短時間で作るプロダクトとして段階的に仕上がっています。  
主軸は `pack`、quality artifact、release review、human outputs、visual outputs の 5 系統です。

## 現在の到達点

- `pack` / `scan` / `resolve-entry` / `compare-evidence` がある
- local quality gate と GitHub Actions の required gate がある
- handoff / corpus / performance の advisory 指標がある
- `render-human` で `review` / `audit` / `knowledge` の Markdown を出せる
- `render-visual` で `Overview` / `Workflow` / `Evidence` / `Unknowns` / `Architecture` を持つ visual artifact を出せる
- local quality gate で `release-review.md`、`human-outputs/`、`visual-outputs/` を補助資料として扱える
- release review の一次資料は `summary.md` に固定されている

## KPI

| 指標 | 意味 | 現在の扱い | 確認方法 |
|---|---|---|---|
| `pack success rate` | pack が返る割合 | required corpus で 100% を維持 | regression / smoke |
| `partial pack rate` | degraded ながら pack が返る割合 | advisory | diagnostics / artifact |
| `crash-free rate` | 実行中に未処理例外が出ない割合 | required gate で 100% | CI / local gate |
| `first useful map time` | 最初の有用な map が返るまでの時間 | advisory | baseline 比較 |
| `unknown guidance hit rate` | handoff 候補が次の探索に効いた割合 | advisory | case review |
| `false confidence rate` | 根拠の薄い断定をした割合 | required corpus で 0% | regression |
| `deterministic rate` | 同一入力で同一結果が返る割合 | required corpus で 100% | regression |

## チェックリスト

### 1. 互換性

- [ ] modern WPF + DI で system pack が安定して返る
- [ ] legacy WPF + code-behind で crash せず返る
- [ ] root entry を `App.xaml` / `DataContext` / `new Window()` から解決できる
- [ ] unsupported construct は degraded pack + diagnostics に落とせる
- [ ] false confidence を抑制できる
- [ ] `render-human` の 3 mode を安定して出せる
- [ ] `render-visual` の 5 view を安定して出せる

### 2. 計測と観測

- [ ] synthetic corpus の modern / legacy / dialog-heavy / weak-signal を維持する
- [ ] observed corpus を observation-only で扱える
- [ ] required gate が CI で動く
- [ ] advisory 指標が artifact に残る
- [ ] compare-evidence の regression が維持される

### 3. UX

- [ ] `entry=file` / `entry=symbol` の選び方が docs で理解できる
- [ ] `pack -> 人間が読む文章` の流れが docs で理解できる
- [ ] `pack -> 探索型 UI` の流れが docs で理解できる
- [ ] `render-human` の 3 mode が README と skill で分かる
- [ ] `render-visual` の 5 view と artifact 構成が README と skill で分かる
- [ ] unknown guidance の読み方が public docs にある
- [ ] GitHub 上で読めるローカル非依存 docs になっている

### 4. サポート資料運用

- [ ] evidence bundle を比較資料として使える
- [ ] `summary.md` / `gate.json` / `kpi.json` を release review に使える
- [ ] `release-review.md`、`human-outputs/`、`visual-outputs/` を補助資料として使える
- [ ] `render-visual` を探索補助資料としてローカルで開ける
- [ ] optional smoke を observation-only として扱える
- [ ] `render-human --mode audit` を根拠ドラフトとして使える
- [ ] `render-human --mode knowledge` を設計知の下書きとして使える

## フェーズとの関係

- Phase 1: scan / resolve-entry / pack の基盤
- Phase 2: goal 駆動 section
- Phase 3: system pack
- Phase 4: legacy WPF compatibility
- Phase 5: quality artifact と local quality gate
- Phase 6: GitHub Actions と release gate
- Phase 7: handoff scoring と corpus / performance 比較
- Phase 8: human output (`render-human`)
- Phase 9: visual output (`render-visual`)
