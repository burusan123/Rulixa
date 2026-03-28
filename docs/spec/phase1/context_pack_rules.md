# Context Pack ルール

## 目的

Phase 1 の Context Pack は、`WPF + .NET 8` アプリケーションの変更作業に必要な最小ファイル束を、AI に渡せる形で決定的に組み立てるためのものとする。

## Pack の基本構造

1. `goal`
2. `entry`
3. `resolved entry`
4. `contracts`
5. `impact/index`
6. `selected files`
7. `unknowns / candidates`

## 1. goal

ユーザーが何をしたいかを短く固定する。

例:

- ボタン追加
- コマンド修正
- ダイアログ起動条件変更
- 保存処理追跡

## 2. entry

Phase 1 では次の入口を前提にする。

- `file:<path>`
- `symbol:<qualifiedName>`
- `auto:<text>`

## 3. resolved entry

曖昧な場合でも、Pack 側で解決結果を明示する。

例:

- `file:Views/ShellView.xaml`
- `symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel`

## 4. contracts

WPF 向けに最低限含める契約は次とする。

- Root 起動契約
- View-ViewModel 契約
- Command 契約
- Dialog 起動契約
- DI 契約

## 5. impact / index

最低限含める索引:

- どの View がどの ViewModel に対応するか
- どの ViewModel がどの Service を使うか
- どの Service がどの Window/Dialog を起動するか
- どのファイルが起動経路にあるか

## 6. selected files

各ファイルには、含めた理由を付与する。

例:

- `App.xaml.cs`
  起動経路
- `ServiceRegistration.cs`
  DI 登録
- `ShellView.xaml`
  DataTemplate と ContentControl によるページ切り替え
- `ShellViewModel.cs`
  CurrentPage とページ生成

## 7. unknowns / candidates

Phase 1 では完全解決を目指さないため、不確実なものは落とさず明示する。

例:

- ViewModel 対応候補が複数ある
- 実行時 `DataContext` のため静的には確定できない
- 外部フレームワーク要素のため解釈不能

## Pack の優先順位

Pack に入れる優先順位は次とする。

1. 入口ファイル / 入口シンボル
2. 明示契約を形成するファイル
3. 直接依存ファイル
4. UI 操作の到達先
5. 関連設定 / DI
6. 補助的な候補

## 削減ルール

budget 超過時は次の順で削る。

1. 候補ファイル
2. 補助設定
3. 関連度の低いサービス
4. 長大ファイルの低優先部分

ただし、次は削らない。

- 入口ファイル
- 対応 ViewModel または View
- 主要 Command / Dialog 契約
- DI 登録の根拠ファイル
