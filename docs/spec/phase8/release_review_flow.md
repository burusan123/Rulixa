# Release Review Flow

## 基本方針

Phase 8 では、release review を artifact 中心で回します。  
コード差分だけではなく、quality artifact と人間向け資料を review の正本として扱います。

## Review 順序

1. `summary.md`
2. `gate.json`
3. `release-review.md`
4. `human-outputs/review-brief-<synthetic-root-case>.md`
5. `human-outputs/audit-snapshot-<synthetic-root-case>.md`
6. `human-outputs/design-knowledge-snapshot-<synthetic-root-case>.md`
7. 必要時のみ `kpi.json` / `compare-evidence`

## 何を見るか

### gate

- required gate が pass しているか
- failed checks がないか

### handoff

- corpus ごとの `hit / miss / unknown`
- miss case の first candidate
- handoff warning

### observed corpus

- pass / fail / skipped
- 構造カテゴリ
- skip reason
- human outputs の有無

### performance

- current / baseline / delta
- regression warning
- case comparison

## Phase 8 の完了状態

- release review の資料構成が文書として固定されている
- `summary.md` を一次資料として読む
- `release-review.md` と `human-outputs/` を補助資料として辿る
- optional smoke は gate 判定に影響しない
- GitHub Actions の step summary から `summary.md` と `release-review.md` を読める
- synthetic root cases の human outputs が自動生成される
- observed corpus の human outputs は optional smoke 実行時のみ追加される
