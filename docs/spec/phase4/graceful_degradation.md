# Graceful Degradation

## 原則

- `Rulixa` は unsupported construct を理由に pack 全体を落とさない
- 取れる signal は返し、取れない部分は diagnostics に送る
- 失敗を隠さない
  ただし exception text をそのまま見せるのではなく、分類した failure reason を返す

## degradation の種類

- `xaml.parse-degraded`
  - XAML 正規化後も一部要素を解釈できない
- `root.resolve-degraded`
  - root candidate を確信できない
- `workflow.route-degraded`
  - command / event / helper の下流が確定しない
- `viewmodel.resolve-degraded`
  - DataContext と ViewModel の対応が部分的
- `resource.resolve-degraded`
  - ResourceDictionary / alias が解決しきれない

## 表現方針

- pack 本文には partial に分かった map を残す
- unknowns / diagnostics には
  - 何は分かったか
  - どこで degraded したか
  - 次に見る symbol / file は何か
  を入れる

## 禁止事項

- parse failure をそのまま top-level failure にする
- unsupported construct を silently ignore する
- fallback が走った理由を隠す
