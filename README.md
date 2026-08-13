# Clipboard Wizard

[![Release](https://img.shields.io/github/v/release/haggisandchips/ClipboardWizard)](https://github.com/haggisandchips/ClipboardWizard/releases/latest)
[![License: GPLv3](https://img.shields.io/badge/license-GPLv3-blue.svg)](LICENSE)

A small Windows clipboard manager. It watches the clipboard, lets you pin
text and image snippets to a persistent list, and re-copy them later -
separate from (and outliving) the OS's own volatile clipboard history.

## Features

- Watches the clipboard for text and images; saved snippets highlight when
  they match what's currently on the clipboard.
- Optional recording mode to automatically capture new clipboard content
  as snippets, or add them manually.
- Organize snippets into user-defined categories, shown as an
  expandable/collapsible accordion, with drag-and-drop to assign,
  recategorize, and reorder.
- Edit a snippet's description (and, for text, its content), drag-and-drop
  to reorder within a category, and copy any snippet back to the clipboard
  with a click.
- Permanent locking to protect a snippet from accidental deletion, with a
  brief unlock window for when it genuinely needs to go.
- Remembers window position, size, and maximized state across restarts.
- Self-updating: checks this repo's GitHub Releases on startup and offers
  to apply new versions.

See [SPEC.md](SPEC.md) for the full functional behaviour.

## Install

Download the latest installer from the
[Releases page](https://github.com/haggisandchips/ClipboardWizard/releases/latest):

- **`ClipboardWizard-win-Setup.exe`** - installs the app and keeps itself
  updated automatically. Recommended.
- **`ClipboardWizard-win-Portable.zip`** - no installation, just extract
  and run `ClipboardWizard.exe`. Does not auto-update.

Windows only.

## Building from source

Requires the .NET 8 SDK on Windows.

```
dotnet build ClipboardWizard.sln
dotnet run --project ClipboardWizard.csproj
dotnet test ClipboardWizard.Tests/ClipboardWizard.Tests.csproj
```

See [CLAUDE.md](CLAUDE.md) for architecture notes, conventions, and the
release process.

## License

[GPLv3](LICENSE). Third-party components and their licenses are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
