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

## Releasing

Releases are packaged with [Velopack](https://velopack.io) and published to
GitHub Releases. Pushing a tag matching `v*` (e.g. `v1.2.3`) triggers
`.github/workflows/release.yml`, which builds, tests, publishes a
self-contained `win-x64` build, packs it with `vpk`, and uploads the result
(installer, portable zip, delta feed) to a GitHub Release tied to that tag —
no manual steps beyond pushing the tag.

To do the same locally (e.g. to test a packaging change before tagging):

```
dotnet tool install -g vpk
dotnet publish ClipboardWizard.csproj -c Release -r win-x64 --self-contained true -p:Version=0.1.0 -o publish
vpk pack --packId ClipboardWizard --packVersion 0.1.0 --packDir publish --mainExe ClipboardWizard.exe --outputDir release --packTitle "Clipboard Wizard" --packAuthors "Ivor Potter" --icon wizard-hat.ico
```

`vpk pack` prints a warning that `VelopackApp.Run()` "does not look like
your application's entry point" — expected and harmless for WPF, which has
no conventional `Main()` to hook; the call in `App`'s constructor (before
`InitializeComponent()`, i.e. as early as this app can intercept) is the
documented pattern for WPF apps and does still work correctly.

In-app, `App.CheckForUpdatesAsync` checks GitHub Releases for a newer
version on startup (fire-and-forget, never blocks startup) via
`Velopack.Sources.GithubSource` pointed at this repo, and no-ops entirely
(`UpdateManager.IsInstalled == false`) when running from a plain `dotnet
build`/`publish` rather than an installed copy — so it's safe to leave
running while developing.

## Licensing

Licensed under [GPLv3](LICENSE). Third-party dependency licenses are
inventoried in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) - GPLv3
rather than GPLv2-only was required because one shipped dependency
(SQLitePCLRaw, Apache-2.0) is incompatible with GPLv2-only. Re-check that
file, and this compatibility question, when adding or upgrading a
dependency.

The app's window/taskbar icon is the Font Awesome Free "hat-wizard" icon
(CC BY 4.0), rendered at runtime via the FontAwesome5 dependency already in
the project (`View/WizardView.xaml`'s `IconTemplate`) and, for the `.exe`'s
embedded icon (`wizard-hat.ico`, which has to be an actual raster file, not
a live glyph), rasterized from the same vector path data at build time -
not a separately-sourced image asset. It replaced an earlier icon file
whose provenance couldn't be confirmed.

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

