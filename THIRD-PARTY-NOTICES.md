# Third-Party Notices

Clipboard Wizard uses the following third-party components. Licenses are as
declared by each package's own NuGet metadata (`.nuspec`) at the versions
currently referenced; re-check on upgrade.

## Shipped in the built application

| Component | License | Notes |
|---|---|---|
| [FontAwesome5](https://github.com/MartinTopfstedt/FontAwesome5) 2.1.4 | MIT | .NET/WPF wrapper controls. |
| [Font Awesome Free](https://fontawesome.com) 5.15.3 (bundled by FontAwesome5) | Icons: [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); Fonts: [SIL OFL 1.1](https://scripts.sil.org/OFL); Code: MIT | The app's window/taskbar icon and several UI glyphs are the Font Awesome Free Solid "hat-wizard", "trash", "pencil-alt", "arrow-up/down", "image", "lock"/"lock-open", "cog", and "plus"/"exclamation-triangle" icons, rendered via FontAwesome5. |
| [Google.Cloud.Firestore](https://github.com/googleapis/google-cloud-dotnet) 4.4.0 (+ Google.Cloud.Firestore.V1, Google.Cloud.Location, Google.LongRunning) | Apache-2.0 | Firestore client used for category/snippet sync. |
| [Google.Apis](https://github.com/googleapis/google-api-dotnet-client) / Google.Apis.Auth / Google.Apis.Core 1.73.0 | Apache-2.0 | Google.Cloud.Firestore dependency; handles service-account credential loading and OAuth token refresh. |
| [Google.Api.CommonProtos](https://github.com/googleapis/gax-dotnet) / Google.Api.Gax / Google.Api.Gax.Grpc 2.17.0 / 4.13.1 | BSD-3-Clause | Google.Cloud.Firestore dependency. |
| [Google.Protobuf](https://github.com/protocolbuffers/protobuf) 3.31.1 | BSD-3-Clause | Google.Cloud.Firestore dependency (wire format). |
| [Grpc.Auth](https://github.com/grpc/grpc-dotnet) / Grpc.Core.Api / Grpc.Net.Client / Grpc.Net.Common 2.71.0 | Apache-2.0 | Google.Cloud.Firestore dependency; the gRPC channel its realtime `Listen()` streaming runs over. |
| [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) 13.0.4 | MIT | Google.Apis dependency. |
| [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) 2.4.9 | MIT | |
| [ControlzEx](https://github.com/ControlzEx/ControlzEx) 4.4.0 | MIT | MahApps.Metro dependency. |
| [Microsoft.Xaml.Behaviors.Wpf](https://github.com/microsoft/XamlBehaviorsWpf) 1.1.19 | MIT | MahApps.Metro dependency. |
| [Microsoft.Bcl.AsyncInterfaces](https://github.com/dotnet/runtime) 10.0.10 | MIT | Transitive dependency. |
| [Microsoft.Extensions.DependencyInjection.Abstractions](https://github.com/dotnet/runtime) / Microsoft.Extensions.Logging.Abstractions 6.0.0 | MIT | Google.Cloud.Firestore dependency. |
| [SharpClipboard](https://github.com/Willy-Kimura/SharpClipboard) 3.5.2 | MIT | |
| [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) 1.8.116 | MIT | |
| [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw) (core, bundle_green, lib.e_sqlite3, provider.dynamic_cdecl) 2.0.4 | Apache-2.0 | Bundles the native SQLite engine, which is itself [public domain](https://www.sqlite.org/copyright.html). **Apache-2.0 is not compatible with GPLv2-only** - relevant if this project adopts GPL (see below). |
| [System.CodeDom](https://github.com/dotnet/runtime) 7.0.0 | MIT | Transitive dependency. |
| [System.Collections.Immutable](https://github.com/dotnet/runtime) 8.0.0 | MIT | Google.Cloud.Firestore dependency. |
| [System.Diagnostics.DiagnosticSource](https://github.com/dotnet/runtime) 6.0.2 | MIT | Transitive dependency. |
| [System.Linq.AsyncEnumerable](https://github.com/dotnet/runtime) 10.0.10 | MIT | Google.Cloud.Firestore dependency. |
| [System.Management](https://github.com/dotnet/runtime) 7.0.2 | MIT | Transitive dependency. |
| [System.Memory](https://github.com/dotnet/corefx) 4.5.3 | MIT | |
| [System.Security.Cryptography.ProtectedData](https://github.com/dotnet/runtime) 8.0.0 | MIT | Windows DPAPI, used to encrypt the Firestore service-account key at rest in the local settings file. |
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

Everything above is MIT except the **SQLitePCLRaw family, Google.Cloud.Firestore
and its Google.Apis/Grpc.* dependencies (all Apache-2.0)**, **Google.Api.Gax
and Google.Protobuf (BSD-3-Clause)**, all of which are shipped in the built
app, and Font Awesome's icon/font assets (CC BY 4.0 / SIL OFL 1.1, not code -
bundling non-code assets under a compatible separate license alongside GPL
code is standard practice and not a compliance issue on its own).

The Apache-2.0 dependencies are why GPLv3 was chosen over GPLv2:

- **GPLv2-only would not have been viable** - the FSF lists Apache-2.0 as
  incompatible with GPLv2 (patent-clause conflict).
- **GPLv3** is fine - it was explicitly drafted to be Apache-2.0-compatible.

BSD-3-Clause is permissive and compatible with any GPL version, same as MIT.
MIT and BSD-3-Clause dependencies impose no constraint on the GPL version
choice; Apache-2.0 is the one that requires GPLv3 rather than GPLv2.
