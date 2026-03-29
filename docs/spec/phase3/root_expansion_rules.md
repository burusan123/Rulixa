# Root Expansion Rules

## 1 hop で拾う family

root seed から 1 hop では次を対象にする。

- workflow-like service
- hub object candidate
- dialog / window activation service
- sibling ViewModel
- major service

ここでの `major service` は、複数 section で証拠が重なる service を指す。

## dialog / window の扱い

dialog / window は単体では section に昇格しない。
次の条件を満たす場合に system map に含める。

- dialog / window の先に別 ViewModel または major workflow がある
- root system の主要サブシステムを構成する

例:

- `DraftingWindowService -> DraftingWindowViewModel`
- `SettingWindowService -> SettingWindow`

## sibling ViewModel の扱い

`ShellThreeDViewModel` のような sibling ViewModel は次の条件で system map に含める。

- root ViewModel から直接構成される
- 独立した sub-map family を持つ
- persistence / hub object / external asset のいずれかに接続する

## System-level section への昇格条件

### Hub Objects

- 複数 sub-map から参照される
- または root family と sub-system family の両方で使われる

### Persistence

- system 全体の主要永続化 family を代表できる
- または root から sibling / sub-window を経由して使われる

### External Assets

- sub-map ローカルではなく system の説明に効く
- 例: settings master, report template, algorithm model

## helper / lambda 深掘りの扱い

Phase 3 では helper / lambda 深掘りを全面的には広げない。
対象は system expansion に必要な場合のみとする。

- dialog/window から sub-map ViewModel に接続するケース
- root から sub-system family へ到達するために不可欠な helper 1 段

一般的な deep drilldown 強化は別 backlog として扱う。
