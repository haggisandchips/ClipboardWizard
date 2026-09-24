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
- Share a category across your own machines: mark it **Shared** and its
  snippets sync to your own Firebase project in near-realtime. See
  [Sharing across devices](#sharing-across-devices) below to set it up.
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

## Sharing across devices

A category's **Shared** toggle syncs its snippets - text and images - to a
Firestore database, and pulls down changes from any other machine sharing
that same category, in near-realtime. It talks to *your own* Firebase
project, not a shared/hosted one, so you need to set that project up once.
The steps below take you from a blank Google account to a working
connection.

### 1. Create a Firebase project

1. Go to the [Firebase console](https://console.firebase.google.com) and
   click **Add project** (or **Create a project**).
2. Give it a name (e.g. `clipboard-wizard`). Google Analytics isn't
   needed - decline it.
3. Wait for provisioning to finish.

### 2. Enable Firestore

1. In the Firebase console's left sidebar: **Build → Firestore Database**.
2. Click **Create database**.
3. Pick a location - this can't be changed later, so pick one near you.
4. Choose **Production mode** over test mode. It doesn't actually matter
   for Clipboard Wizard itself - the service account you create next
   bypasses security rules entirely - but production mode is the safer
   default in case anything else ever touches this project.

### 3. Create a service account

Firebase projects are Google Cloud projects under the hood, so this step
happens in the Cloud Console, not the Firebase one:

1. Go to [Cloud Console → IAM & Admin → Service
   Accounts](https://console.cloud.google.com/iam-admin/serviceaccounts),
   and make sure the project selector at the top is set to the Firebase
   project you just created.
2. Click **Create Service Account**.
3. Name it something like `clipboard-wizard-sync`, click **Create and
   Continue**.
4. Under "Grant this service account access to project", add the role
   **Cloud Datastore User** - the minimal role that covers Firestore
   read/write access. Don't grant anything broader (Editor/Owner) - this
   key will live on your PC, so keep its blast radius small.
5. Skip the optional "grant users access" step, click **Done**.

### 4. Generate the key

1. Click into the service account you just created.
2. Go to the **Keys** tab.
3. **Add Key → Create new key → JSON → Create**.
4. A `.json` file downloads automatically. This file *is* the credential
   - treat it like a password: don't commit it anywhere, don't share it,
   and revoke/rotate it if it ever leaks.

### 5. Configure Clipboard Wizard

1. Open Clipboard Wizard, click the **cog icon** in the title bar.
2. Click **Browse...** and pick the downloaded `.json` file (or paste its
   contents directly into the box).
3. The dialog shows the parsed project id underneath - confirm it matches
   your Firebase project.
4. Click **Test Connection** - it should report success.
5. Click **Save**.

### 6. Share a category

1. Create a new category (or edit an existing one) and check **Shared**.
2. The warning icon on that category's header stays hidden as long as
   Firebase is configured and connected; it appears if Shared is on but
   the connection isn't.

To sync between machines, repeat steps 3-5 on each of them, pointed at
the *same* Firebase project (reuse the same key file, or generate a
separate key per machine).

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
