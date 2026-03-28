# Aggregation And Compression

## 基本方針

- section を増やしすぎない
- 既存 section に system-level representative を載せる
- 同じ意味の route は canonicalize して 1 件に束ねる
- sub-map ごとの重複 signal は system-level で除去する

## Aggregation の単位

system-level aggregation は次の単位で行う。

- root symbol
- sub-map family
- first hop family
- downstream family
- goal category

同じ単位に入る route は、最も説明力の高い 1 件だけを残す。

## Section ごとの扱い

### Workflow

- system 全体の仕事の流れを代表する chain を残す
- `Shell -> Drafting` のような sub-map 間導線もここに含めてよい

### Persistence

- system 全体で重要な永続化 family を代表 1 件ずつ残す

### Hub Objects

- system の中心状態オブジェクトを 1〜3 件に絞る

### External Assets

- 外部資産種別ごとの代表経路を残す

### Architecture Tests

- family 単位の回帰拘束地図として扱う

## Unknown 集約

- sub-map 単位の unknown を system-level に統合する
- 同じ code / family / root に属する unknown は 1 件にまとめる
- `Candidates` は重複除去して最大 3 件

## 品質指標

Phase 3 でも次の指標を継続利用する。

- `signal density`
- `map usefulness`
- `unknown guidance quality`
- `determinism`

Phase 3 は件数を増やすことより、system-level representative の説明力を高めることを優先する。
