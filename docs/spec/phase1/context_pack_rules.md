# Context Pack ルール

## 位置づけ

この文書は Phase 1 の `WPF + .NET 8` 向け Context Pack 選定ルールです。
Rulixa 全体の一般規則ではなく、現在の具体攻略対象に最適化したルールとして扱います。

## 目的

Phase 1 の Context Pack は、`WPF + .NET 8` アプリケーションの変更作業に必要な最小限のファイルと契約を AI に渡すための形式です。

## Pack の基本構成

1. `goal`
2. `entry`
3. `resolved entry`
4. `contracts`
5. `impact / index`
6. `selected files`
7. `unknowns`

## contracts

Phase 1 では次を基本セットにします。

- 起動経路
- DI 登録
- View と ViewModel の対応
- ナビゲーション
- コマンド
- ダイアログ起動

## impact / index

少なくとも次を読める状態にします。

- どの View がどの ViewModel に対応するか
- どの UI 選択がどの表示切り替えを駆動するか
- `SelectedItem` と `CurrentPage` の更新地点
- どのサービスがどの Window / Dialog を起動するか

## selected files

各ファイルには理由を持たせます。

例:

- `entry`
- `startup`
- `dependency-injection`
- `root-binding`
- `view-binding`
- `data-template`
- `command-support`
- `navigation-update`

## 選定優先順位

1. 入口ファイル / 入口シンボル
2. 明示的な binding / 起動経路
3. ViewModel 側更新点
4. コマンドとダイアログ関連
5. DataTemplate 由来の二次文脈

## budget が厳しい場合に先に落とすもの

1. `DataTemplate` 由来の二次文脈
2. 任意の関連サービス
3. 巨大ファイルの補助的な全文

ただし次は落とさない:

- 入口ファイル
- 入口 ViewModel
- 対応 View / code-behind
- 起動経路
- DI 登録
- `SelectedItem` / `CurrentPage` の更新点
