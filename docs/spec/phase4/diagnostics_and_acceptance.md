# Diagnostics And Acceptance

## diagnostics の目的

- `Rulixa` の弱い場所を隠さず示す
- ユーザーが全文検索へ移るべきかを判断できるようにする
- サポート時に「どこが未対応か」を再現可能にする

## diagnostics に必須の要素

- category
  - parse
  - root resolution
  - workflow route
  - viewmodel binding
  - resource resolution
- severity
  - info
  - warning
  - degraded
- known-good signal
  - 何までは取れたか
- next candidates
  - 次に見る symbol / file を 3 件以内で返す

## acceptance 対象

### real workspace

- `<modern-real-workspace>`
  - modern WPF acceptance
- `<legacy-real-workspace>`
  - legacy WPF acceptance

### synthetic workspace

- code-behind startup
- `new ViewModel()` 直結
- service locator
- dialog-heavy navigation
- ResourceDictionary heavy

## acceptance 条件

- modern workspace では Phase 3 相当の system pack 品質を維持する
- legacy workspace では crash せず partial pack 以上を返す
- `LegacyRealWorkspace` のような旧構成でも、root、中心状態、主要 workflow 候補のいずれかが出る
- unsupported construct がある場合でも diagnostics が安定して出る

## 市販レベルの品質条件

- 同じ入力で同じ diagnostics が出る
- unsupported workspace でもサポート可能な説明が残る
- 実ワークスペース 2 系統 + synthetic fixture 群で回帰を固定する
