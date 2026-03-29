# Implementation Plan

## 実装順

1. handoff hit/miss/unknown の artifact schema 追加
2. quality evaluator に case 単位の handoff 判定を追加
3. `summary.md` に handoff 比率と first candidate 要約を追加
4. corpus を synthetic / real workspace の両面で拡張
5. benchmark 比較ロジックを artifact 集約へ追加
6. CI artifact の表示順を handoff / performance 重視へ調整
7. 公開 docs / examples からローカル絶対パスと空例を除去する

## テスト方針

### quality

- handoff outcome が `hit/miss/unknown` で記録される
- Drafting 系で期待 family が無い場合に miss になる
- UI ノイズのみの candidate が miss になる
- degraded case が `unknown` または `hit` 判定されても raw exception 単独ではない

### corpus

- 追加 synthetic fixture で root / non-root / weak-signal が維持される
- real workspace corpus が observation-only として artifact に残る

### performance

- `first_useful_map_time_ms` が run artifact に載る
- baseline 比較結果が summary に出る
- advisory 指標が gate fail と混同されない

### docs

- GitHub 上で壊れるローカル絶対パスが公開 docs に残らない
- 例示ファイルが空例や placeholder だけで終わらず、最低限の使い方を伝える
- example 内のパスは相対化、プレースホルダ化、または GitHub で読める説明に置き換える

## 検証コマンド

```powershell
dotnet build .\Rulixa.sln
dotnet test .\Rulixa.sln --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1
```

必要時:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1 -IncludeOptionalSmoke
```

## リスク

- corpus を増やしすぎると CI 時間が重くなる
- handoff 判定を厳しくしすぎると false miss が増える
- performance 比較を hosted runner と real workspace で混ぜるとノイズが増える
- docs hardening を後回しにすると、public repo 上の信頼性が下がる

## フェーズ完了条件

- handoff hit/miss/unknown が artifact と summary に出る
- corpus 拡張が acceptance に組み込まれる
- benchmark 比較が advisory 指標として機能する
- release gate と advisory 指標の境界が崩れていない
- GitHub 上で壊れるローカル絶対パスと空の例が docs / examples から除去されている
