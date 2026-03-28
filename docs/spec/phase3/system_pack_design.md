# System Pack Design

## 定義

`System Pack` は root seed を起点に複数 sub-map を統合した pack である。
単一 entry の詳細説明を増やすのではなく、システム全体の輪郭を最小コンテキストで伝えることを目的とする。

## Root 起点拡張

root seed は次のいずれかで決まる。

- root data context に対応する ViewModel
- startup root ViewModel
- ユーザーが明示した symbol entry が root-level ViewModel の場合

root seed を起点に、first expansion では次を候補にする。

- workflow family
- hub object family
- dialog / window を開く service
- sibling ViewModel
- major service

## Sub-map family

system pack は内部で次のような family 単位の sub-map を扱う。

- `Shell`
- `Drafting`
- `Settings`
- `3D`
- `Report/Export`
- `Architecture`

family 名は出力上の section 名ではなく、内部の canonicalization と aggregation の単位として使う。

## 成功条件

system pack の成功条件は件数ではなく「最低限伝わる地図」とする。

- 起動経路が分かる
- 中心状態が分かる
- 永続化境界が分かる
- 外部資産が分かる
- 主要サブシステムが分かる
- 回帰拘束が分かる

## AssessMeister acceptance

`AssessMeister` では `ShellViewModel` pack から少なくとも次を読めることを acceptance にする。

- `Shell`
- `Drafting`
- `Settings / Report`
- `Architecture`
- 中心状態としての `ProjectDocument`

`Drafting` が直接 chain として表現できない場合でも、system-level unknown guidance により次に掘る候補が示されていれば成立とみなす。
