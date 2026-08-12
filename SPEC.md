# SPEC.md

Functional specification for Clipboard Wizard.

## Purpose

A small Windows utility for keeping a persistent list of clipboard snippets
that survive across reboots, separate from the OS's own volatile clipboard
history.

## Behaviour

**Clipboard watching.** The app monitors the system clipboard for text
changes. Whenever the clipboard changes, every saved snippet is marked
Active (highlighted) if its content matches the new clipboard text exactly,
otherwise Inactive. At most the snippets matching the current clipboard
content are shown Active at any one time.

**Recording.** A "Recording" toggle in the window chrome. While on, any
clipboard text that doesn't match an existing snippet is automatically
saved as a new snippet. While off, clipboard changes only update snippets'
Active/Inactive highlighting.

**Manual add.** Two ways to add a snippet without recording:
- The **+** button saves the current clipboard content as a new snippet.
- The **New** button opens a dialog to type a description and content
  directly (does not require anything to be on the clipboard).

**Snippet tile.** Each saved snippet is shown as a tile displaying its
description if set, otherwise its raw content. Clicking the tile copies its
content back to the clipboard. Tiles arrange themselves in a grid that
reflows as the window is resized.

**Per-snippet actions** (shown on hover):
- **Copy** — click the tile itself.
- **Delete** — permanently removes the snippet. Disabled while the snippet
  is protected (see Locking, below).
- **Edit** — opens a dialog to change the description and/or content.
  Always available, regardless of lock state — editing isn't a single
  accidental click away from losing the snippet the way deleting is.
- **Move up / Move down** — reorders the snippet in the list. Disabled at
  the top/bottom of the list respectively.
- **Lock** — see below.

**Locking.** A snippet can be permanently protected from deletion:
1. Clicking the lock icon on an unprotected snippet locks it permanently.
   This cannot be undone — there is no unlock action, by design, for
   snippets the user wants to keep forever.
2. Clicking the lock icon on a locked snippet opens a **3-second window**
   during which Delete becomes available, then the snippet re-locks itself
   automatically. This exists so a locked-but-no-longer-wanted snippet can
   still be removed, without making deletion a single accidental click away
   for content the user marked as worth keeping.

**Persistence.** Snippets are stored in a local SQLite database at
`%LocalAppData%\ClipboardWizard\Snippets.db`. All changes are persisted
immediately; there is no explicit save step.

## Non-goals (current version)

- No sync across machines.
- No search/filter over snippets.
- No clipboard formats other than plain text (rich text, files, images are
  ignored).

## Planned

Not yet implemented. Noted here so current design decisions don't
foreclose them:

- **Image snippets** — saving and displaying image clipboard content
  alongside text.
- **Categories** — organizing snippets into user-defined groups/categories
  rather than a single flat list.
