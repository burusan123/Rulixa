# Corpus Expansion Strategy

## 目的

現在の corpus は synthetic が中心で、real workspace は observation-only の 2 系統に寄っている。  
Phase 7 では、real workspace と synthetic の両方で「どの構造に強く、どこで弱いか」を説明できるように corpus を増やす。

## 追加対象

### synthetic

- code-behind heavy root
- service locator root
- sibling ViewModel root
- template-heavy resources
- weak-signal root
- dialog-heavy root

### real workspace

- modern DI-based WPF
- legacy code-behind WPF
- dialog-heavy WPF
- settings-heavy / report-heavy WPF

## corpus の役割分担

### required corpus

- deterministic
- weak-signal
- root resolution
- false confidence

を機械判定するための synthetic 群

### observed corpus

- 汎用性
- handoff quality
- performance

を観測するための real workspace 群

## acceptance の固定観点

- root entry で crash-free
- non-root entry で system pack を過剰に広げない
- representative chain と unknown guidance のどちらかで地図を返す
- unsupported case で diagnostic と next candidates が出る
- corpus ごとの期待 family が summary に現れる

## 命名方針

- 固有プロダクト名を fixture 名に使わない
- 構造を表す名前に寄せる
  - `LegacyDialogHeavy`
  - `ModernSiblingRoot`
  - `ServiceLocatorRoot`
  - `TemplateHeavyResources`

## 完了条件

- required corpus が bias の少ない synthetic 群で回る
- observed corpus に real workspace が複数含まれる
- corpus の不足分を README や summary で説明できる
- docs の例が特定ローカル環境前提ではなく、public repo で読める形になっている
