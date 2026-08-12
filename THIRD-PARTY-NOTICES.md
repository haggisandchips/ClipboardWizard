# Third-Party Notices

Clipboard Wizard uses the following third-party components. Licenses are as
declared by each package's own NuGet metadata (`.nuspec`) at the versions
currently referenced; re-check on upgrade.

## Shipped in the built application

| Component | License | Notes |
|---|---|---|
| [FontAwesome5](https://github.com/MartinTopfstedt/FontAwesome5) 2.1.4 | MIT | .NET/WPF wrapper controls. |
| [Font Awesome Free](https://fontawesome.com) 5.15.3 (bundled by FontAwesome5) | Icons: [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); Fonts: [SIL OFL 1.1](https://scripts.sil.org/OFL); Code: MIT | The app's window/taskbar icon and several UI glyphs are the Font Awesome Free Solid "hat-wizard", "trash", "pencil-alt", "arrow-up/down", "image", "lock"/"lock-open", and "plus" icons, rendered via FontAwesome5. |
| [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) 2.4.9 | MIT | |
| [ControlzEx](https://github.com/ControlzEx/ControlzEx) 4.4.0 | MIT | MahApps.Metro dependency. |
| [Microsoft.Xaml.Behaviors.Wpf](https://github.com/microsoft/XamlBehaviorsWpf) 1.1.19 | MIT | MahApps.Metro dependency. |
| [SharpClipboard](https://github.com/Willy-Kimura/SharpClipboard) 3.5.2 | MIT | |
| [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) 1.8.116 | MIT | |
| [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw) (core, bundle_green, lib.e_sqlite3, provider.dynamic_cdecl) 2.0.4 | Apache-2.0 | Bundles the native SQLite engine, which is itself [public domain](https://www.sqlite.org/copyright.html). **Apache-2.0 is not compatible with GPLv2-only** - relevant if this project adopts GPL (see below). |
| [System.Memory](https://github.com/dotnet/corefx) 4.5.3 | MIT | |
| [System.Text.Json](https://github.com/dotnet/runtime) 4.7.2 | MIT | |
| [Velopack](https://velopack.io) 1.2.0 | MIT | |

## Build/test-only (not distributed to end users)

| Component | License |
|---|---|
| xunit 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio 3.1.4 | Apache-2.0 |
| Microsoft.NET.Test.Sdk 17.14.1 | MIT |
| coverlet.collector 6.0.4 | MIT |

## GPL compatibility (this project is licensed GPLv3 - see [LICENSE](LICENSE))

Everything above is MIT except the **SQLitePCLRaw family (Apache-2.0)**,
which is shipped in the built app, and Font Awesome's icon/font assets
(CC BY 4.0 / SIL OFL 1.1, not code - bundling non-code assets under a
compatible separate license alongside GPL code is standard practice and not
a compliance issue on its own).

The Apache-2.0 dependency is why GPLv3 was chosen over GPLv2:

- **GPLv2-only would not have been viable** - the FSF lists Apache-2.0 as
  incompatible with GPLv2 (patent-clause conflict).
- **GPLv3** is fine - it was explicitly drafted to be Apache-2.0-compatible.

MIT dependencies are compatible with any GPL version and impose no
constraint on the choice.
