# Architecture

## 概要

Phase 4 では `scan -> resolve-entry -> pack` の外形は維持し、内部に `Compatibility Layer` を追加する。

## 主な責務

- `XamlCompatibilityNormalizer`
  - namespace alias、`x:Key`、ResourceDictionary、legacy markup を正規化する
- `LegacyRootResolver`
  - modern DI 前提に依存せず、legacy 起動経路から root candidate を推定する
- `FallbackRouteResolver`
  - constructor DI だけでなく manual `new`、service locator、event handler を route 候補として扱う
- `PartialPackAssembler`
  - 途中失敗があっても残せる signal だけで pack を構築する
- `CompatibilityDiagnosticsBuilder`
  - failure reason、degraded reason、次に見る候補を deterministic に返す

## 設計方針

- 既存の抽出経路を捨てない
- modern path はそのまま残し、legacy path を fallback として足す
- fallback が走っても `ContextPack` shape は変えない
- unsupported construct は exception にしない
  - classify
  - degrade
  - diagnose

## 依存関係

- normalizer は scan の直後
- root resolver は resolve-entry と pack の前段
- fallback route resolver は existing planner の補助として動く
- diagnostics builder は renderer の直前で集約する
