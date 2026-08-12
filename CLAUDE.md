# CLAUDE.md

Guidance for working in this repository.

## What this is

A Windows desktop (WPF, .NET 8) clipboard manager. It watches the system
clipboard, lets the user pin text and image snippets to a persistent list,
and re-copy them later. See [SPEC.md](SPEC.md) for functional behaviour.

## Build, run, test

```
dotnet build ClipboardWizard.sln
dotnet run --project ClipboardWizard.csproj
dotnet test ClipboardWizard.Tests/ClipboardWizard.Tests.csproj
```

Windows only (WPF). `ClipboardWizard.Tests` lives under the main project's
own directory, so its files are excluded from the main `.csproj` via an
explicit `<Compile Remove>` — if you add a third project under this root,
give it the same treatment or the default SDK globs will pull its files
into the main build.

## Architecture

MVVM, composed by hand in `App.xaml.cs` (no DI container — the object graph
is small enough not to need one):

- **Model** (`Model/`) — `Snippet` (SQLite-mapped; `Type` selects whether
  `Content` (text) or `ImageData` (PNG bytes) is the live payload) and
  `State` (Active/Inactive, whether a snippet matches the current clipboard
  contents).
- **Service** (`Service/`) — `ISnippetRepository`/`SnippetRepository` (async
  SQLite persistence, one reused connection), `IClipboardMonitor`/
  `SharpClipboardMonitor` (wraps the third-party SharpClipboard so the rest
  of the app depends on our own interface, and our own `ClipboardContent`
  type, instead), `ImageCodec` (the one place that knows snippet images are
  PNG-encoded), `Logger` (best-effort file logging).
- **ViewModel** (`ViewModel/`) — `WizardViewModel` owns the snippet
  collection and implements `ISnippetHost`, the interface each
  `SnippetViewModel` uses to ask its owner to persist, remove, or reorder it.
  This replaced an earlier design based on static events on `App`; prefer
  explicit interfaces like this over static/global event buses when adding
  new cross-cutting behaviour.
- **View** (`View/`) — WPF windows/controls, mostly declarative bindings.
  `WizardView` and `EditSnippetView` receive their view model via
  constructor injection rather than instantiating it from XAML.

Commands (`ViewModel/Command/`) are `async void Execute` (required by
`ICommand`) wrapped in try/catch via `CommandErrorHandler`, since an
unhandled exception in async void would otherwise crash the app silently.

## Conventions

- Errors are never silently swallowed: user-initiated actions (commands) log
  and show a `MessageBox`; the one background/automatic path (clipboard
  auto-recording) logs only, since it isn't a response to something the user
  just clicked.
- Tests use hand-written fakes (`ClipboardWizard.Tests/Fakes/`) rather than a
  mocking library — the interfaces are small enough that this stays cheap.
  `SnippetRepositoryTests` is the exception: it exercises the real SQLite
  path against a temp file, since that mapping is the one thing worth
  testing for real.
- `InternalsVisibleTo` exposes `internal` ViewModel members to the test
  project so async command-backing methods can be awaited directly in
  tests, instead of trying to await `ICommand.Execute`'s `async void`.
- Clipboard/snippet matching for "is this Active" and "should this be
  auto-recorded" is centralized in `WizardViewModel.Matches`/`IsSaveable`
  rather than duplicated per content type — extend those, don't add a
  parallel comparison path, if a third content type is ever added.
- `SnippetRepository` relies on sqlite-net-pcl adding missing columns via
  `ALTER TABLE` on `CreateTableAsync`, so adding a new `Snippet` property is
  schema-compatible with existing databases for free — no migration code
  needed, but do add a test like
  `SnippetRepositoryMigrationTests` for new columns that need a specific
  default for pre-existing rows (int/bool default to `0`/`false`, which
  isn't always the right value).

## Planned work

Not yet implemented — see [SPEC.md](SPEC.md#planned) before making
structural decisions that would make it harder to add later:

- Organizing snippets into categories
