---
name: pack
description: Generate a Context Pack with `Rulixa.Cli` for a WPF + .NET workspace using `entry=file` or `entry=symbol`.
---

# Rulixa Pack

Use this skill when the user wants to generate a Context Pack from a local workspace with `Rulixa.Cli`.

## Purpose

- Prefer `pack` as the main user flow.
- Support both `entry=file` and `entry=symbol`.
- Treat `scan` and `resolve-entry` as secondary support commands.

## Inputs to confirm

Collect these inputs before running the command:

- `workspace`
  Target workspace path to analyze.
- `entry`
  Use `symbol:` when the target ViewModel or type name is known.
  Use `file:` when the user points to a XAML or code file directly.
- `goal`
  The user intent to include in the generated Context Pack.
- Optional budget overrides
  `--max-files`
  `--max-total-lines`
  `--max-snippets-per-file`

If the user does not ask for a custom budget, use the CLI defaults.

## Recommended flow

1. If the target is a known ViewModel, prefer `entry=symbol`.
2. If the user points at a XAML file, use `entry=file`.
3. Build the `pack` command first.
4. Use `resolve-entry` only when the entry is ambiguous.
5. Use `scan` only when the user needs raw IR output.

## Commands

Run from the `Rulixa` repo root.

### Main command

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace <target-workspace> `
  --entry <entry> `
  --goal "<goal>"
```

### Example: symbol entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry symbol:AssessMeister.Presentation.Wpf.ViewModels.ShellViewModel `
  --goal "Shell 画面に新しいページを追加したい"
```

### Example: file entry

```powershell
dotnet run --project src\Rulixa.Cli -- pack `
  --workspace D:\C#\AssessMeister `
  --entry file:src/AssessMeister.Presentation.Wpf/Views/ShellView.xaml `
  --goal "Shell 画面に新しいページを追加したい"
```

### Support: resolve-entry

```powershell
dotnet run --project src\Rulixa.Cli -- resolve-entry `
  --workspace <target-workspace> `
  --entry <entry>
```

### Support: scan

```powershell
dotnet run --project src\Rulixa.Cli -- scan `
  --workspace <target-workspace>
```

## Output expectations

- The main output is Markdown.
- The pack should include the goal, resolved entry, contracts, index, selected files, and unknowns.
- For `ShellViewModel` flows, the preferred result is a compact pack focused on Shell-related files rather than all `DataTemplate` pages.
