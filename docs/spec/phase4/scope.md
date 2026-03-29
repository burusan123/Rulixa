# Scope

## In scope

- WPF workspace の legacy compatibility 強化
  - `App.xaml StartupUri`
  - code-behind 主導起動
  - manual `new Window()` / `new ViewModel()`
  - service locator / static resolver
  - partial class 分散
  - ResourceDictionary / merged dictionaries
  - `x:Key` / local namespace / duplicate alias の揺れ
  - command ではなく event handler 主導の画面遷移
- graceful degradation
  - extraction failure を pack failure にしない
  - partial pack と diagnostics を返す
- compatibility fixtures と smoke の追加
- diagnostics / evidence の品質向上

## Out of scope

- WPF 以外への一般化
- Avalonia / WinUI / WinForms 対応
- 全文検索の代替を目指す recall 最大化
- CLI schema や evidence manifest の破壊的変更
- 4 hop 以上の一般探索
- mode 分離や deep drilldown の本格導入
  これは Phase 5 以降へ送る

## 目的

- `Rulixa` を modern WPF 専用ツールにしない
- `pack が出ない workspace` を減らす
- 市販レベルで必要な「互換性」「安定性」「診断性」を先に整える
