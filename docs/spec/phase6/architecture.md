# Architecture

Phase 6 では、既存の `pack` 系実装に大きな責務追加は行わず、quality 運用層を拡張する。

## 責務分割

### 1. Pack / Scan 実行層
- workspace を scan する
- entry を resolve する
- pack を生成する
- diagnostics / unknown guidance / representative chain を返す

この層の責務は Phase 5 と同じで、Phase 6 では原則として再利用する。

### 2. Quality Artifact 層
- case 単位 artifact を生成する
- run 単位 artifact を集約する
- benchmark と handoff observation を加算する
- CI / ローカルの両方で同じ schema を使う

### 3. Gate Evaluation 層
- required corpus に対して success / crash-free / deterministic / false-confidence を判定する
- handoff quality を warning または advisory として扱う
- release gate の pass/fail を JSON と markdown の両方で出す

### 4. Benchmark / Telemetry 層
- `first useful map time` を run ごとに記録する
- representative chain 数、unknown guidance 数、degraded reason 数を集計する
- 退行検知に使える比較単位を作る

### 5. CI / Release Integration 層
- local runner と同じ quality 実行を CI から呼ぶ
- artifact を保存する
- gate に失敗した run を release 不可として扱う

## 依存方向

- `pack` 実行層は quality / CI を知らない
- quality artifact 層は `pack` 結果を読むが、逆依存は作らない
- CI / release 層は quality artifact を読むが、`pack` 本体のロジックを持たない

この分離により、pack の品質向上と release 運用を独立に進められるようにする。
