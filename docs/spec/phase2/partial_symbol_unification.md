# Partial Symbol Unification

## 背景

WPF 業務アプリでは巨大 ViewModel や code-behind が partial class に分割されることが多い。
Phase 1 は entry 解決時に単一ファイルへ偏ることがあり、`DraftingWindowViewModel.Arc.cs` のような一部ファイルだけから全体像を誤認しやすかった。

## 目的

同じ fully-qualified symbol に属する partial 宣言を 1 つの論理シンボルとして扱う。

## 対象

- `partial class`
- `partial record`
- `partial struct`

Phase 2 ではまず C# のみを対象にする。

## 仕様

### symbol aggregate

1 つの symbol に対して次を束ねる。

- 参加ファイル一覧
- constructor 一覧
- command 定義候補
- public / internal API
- event subscription
- high-signal method

### entry resolve

- `resolve-entry` の `resolvedPath` は代表ファイルを返してよい
- ただし `pack` では代表ファイルだけでなく symbol aggregate 全体を使う
- 出力には「partial 統合対象ファイル数」を載せてよい

### snippet 選定

partial symbol から snippet を選ぶときは次を優先する。

1. constructor
2. command 実行点
3. 状態更新の中核メソッド
4. 外部 service / repository 呼び出し

特定ファイルだけに寄った要約は禁止しないが、Pack では aggregate の観点を失わないこと。

### file selection

partial 参加ファイルはすべて必須にしない。
signal score に基づき 1〜3 ファイル程度を採用し、残りは index や契約に反映する。

## unknowns との関係

partial symbol の一部しか scan できていない、または命名衝突で統合できない場合は `unknowns` に出す。

## 期待効果

- `DraftingWindowViewModel` の pack が `.Arc.cs` だけに偏らない
- constructor dependency と command 定義が統合される
- 巨大 ViewModel の「どの責務がどの partial にいるか」が見える
