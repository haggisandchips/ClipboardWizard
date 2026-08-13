# SPEC.md

Functional specification for Clipboard Wizard.

## Purpose

A small Windows utility for keeping a persistent list of clipboard snippets
that survive across reboots, separate from the OS's own volatile clipboard
history.

## Behaviour

**Clipboard watching.** The app monitors the system clipboard for text and
image changes. Whenever the clipboard changes, every saved snippet is
marked Active (highlighted) if its content matches the new clipboard
content exactly (same text, or byte-identical image), otherwise Inactive.
Text and image snippets never match each other. At most the snippets
matching the current clipboard content are shown Active at any one time.

**Recording.** A "Recording" toggle in the window chrome. While on, any
clipboard text or image that doesn't match an existing snippet is
automatically saved as a new snippet. While off, clipboard changes only
update snippets' Active/Inactive highlighting.

**Manual add.** Two ways to add a snippet without recording:
- The **+** button saves the current clipboard content (text or image) as
  a new snippet.
- The **New Snippet** button opens a dialog to type a description and text
  content directly (does not require anything to be on the clipboard).
  Text only — there's no dialog-based way to hand-author an image snippet.

The **New Category** button (to New Snippet's left) opens the equivalent
dialog for creating a category: just a name.

**Snippet tile.** Each saved text snippet is shown as a tile displaying its
description if set, otherwise its raw content. Each saved image snippet
shows a thumbnail of the image if it has no description; if it does have
one, the tile shows that description instead, with a small image icon
underneath so it still reads as a picture rather than a text snippet.
Clicking the tile copies its content back to the clipboard. Tiles arrange
themselves in a grid that reflows as the window is resized. The hover
toolbar (below) sits on an opaque light panel so its icons stay legible
over a dark image thumbnail.

**Per-snippet actions** (shown on hover):
- **Copy** — click the tile itself.
- **Delete** — permanently removes the snippet. Disabled while the snippet
  is protected (see Locking, below).
- **Edit** — opens a dialog to change the description, and (text snippets
  only) the content. Editing isn't a single accidental click away from
  losing the snippet the way deleting is, so unlike delete it isn't gated
  by the lock. For an image snippet the dialog shows the picture read-only
  (auto-sized to the dialog, so it scales if the dialog is resized) and
  only the description is editable — there's no sensible way to "edit" a
  picture's pixels in a text box, so replacing the image itself still means
  delete and copy/save a new one.
- **Lock** — see below.

A snippet tile is reordered by **dragging and dropping it onto another
tile** - drag-and-drop is the only reordering mechanism. A thin blue line on
the left or right edge of the tile being hovered over shows where the
dragged tile will land - hovering the left half inserts before that tile,
the right half inserts after, regardless of which direction it was dragged
from. The indicator (and the drop itself) is suppressed for a position that
wouldn't actually move the tile - dropping it on itself, or on the near
side of its immediate neighbour on either side.

**Categories.** Snippets are organized into user-defined categories, shown
as an accordion: each category is a full-width, expandable/collapsible
section containing that category's snippet tiles. The pinned
**Uncategorized** section always comes last, holds any snippet with no
category, and can't be deleted or reordered.
- **Create** — the **New Category** button (see Manual add, above) opens
  a dialog for just a name.
- **Delete** — click the trash icon on a category's header. This isn't a
  single click: a confirmation prompt must be accepted first. Deleting a
  category never deletes its snippets - they move to Uncategorized instead.
- **Expand/collapse** — click a section's header (anywhere except the trash
  icon). Real categories persist this immediately; Uncategorized's state is
  saved with the window's other leftover placement on close.
- **Assign** — the New/Edit snippet dialog has a Category dropdown
  (defaulting to "(none)"). A snippet can also be assigned by dragging its
  tile onto another section's header or empty body (which highlights blue
  to show it's the target, then appends the snippet there), or directly
  onto one of that section's tiles to both move it there and position it
  precisely, in one gesture.
- **Reorder** — category sections are reordered by dragging one section
  onto another (anywhere in its bounds, not just the header), with the same
  drop-indicator and no-op-suppression behaviour as snippet tiles.

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

**Updates.** On every startup, an installed copy checks GitHub Releases in
the background for a newer version. If one is found it's downloaded
automatically, then the user is asked whether to restart immediately to
apply it; declining just defers the (already-downloaded) update to the next
normal restart. This never blocks or delays startup, and does nothing at
all when running a non-installed (development) build.

## Non-goals (current version)

- No sync across machines.
- No search/filter over snippets.
- No clipboard formats other than plain text and images (rich text, files,
  and anything else are ignored).
