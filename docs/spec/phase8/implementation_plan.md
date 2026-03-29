# Implementation Plan

## 実装順

1. 人間向け出力 3 種の schema を固める
2. handoff review score の設計を固める
3. observed corpus の CI / manual 層分けを反映する
4. performance warning の段階分けを実装する
5. release review 用の summary と human outputs を整形する
6. product-readiness と GitHub workflow を更新する

## テスト方針

```powershell
dotnet build .\Rulixa.sln
dotnet test .\Rulixa.sln --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1
```

必要に応じて:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-LocalQualityGate.ps1 -IncludeOptionalSmoke
```

## 重点確認

- `Review Brief` / `Audit Snapshot` / `Design Knowledge Snapshot` の責務が分かれている
- handoff review score が advisory として一貫している
- observed corpus のカテゴリごとに運用先が決まっている
- performance warning が過剰でも過少でもない
- release review で見る artifact が固定されている
- public docs と product-readiness が現状に追従している

## リスク

- handoff score を強くしすぎると false miss が増える
- performance warning を厳しくしすぎると hosted runner の揺らぎに引っ張られる
- observed corpus を無理に required gate に入れると運用が不安定になる
- 人間向け出力で推定を断定に見せると監査品質が落ちる

## 完了条件

- 人間向け出力の運用ルールが固まる
- release review が artifact ベースで回る
- 次フェーズで required gate へ昇格する候補が明確になる
