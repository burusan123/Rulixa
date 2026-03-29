# Release Review Flow

## 基本方針

Phase 8 では、release review を artifact 中心で閉じることを目指します。  
コード差分だけではなく、quality artifact と人間向け出力を review の正本として扱います。

## Review 順序

1. `gate.json`
2. `summary.md`
3. `Review Brief`
4. `Audit Snapshot`
5. `kpi.json`
6. compare-evidence が必要な場合のみ追加確認

## 何を見るか

### gate

- required gate が pass か
- failed checks がないか

### handoff

- corpus ごとの `hit / miss / unknown`
- miss case の first candidate
- handoff warning

### observed corpus

- pass / fail / skipped
- 実行カテゴリ
- skip reason

### performance

- current / baseline / delta
- regression warning
- case comparison

## Phase 8 の完了条件

- release review の観点が文書として固定されている
- summary だけで一次判断できる
- 詳細確認が必要な場合の参照先が明確
- 人間向け出力が gate 判定と矛盾しない
