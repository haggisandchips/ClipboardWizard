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
  a dialog for a name and, optionally, Shared (see Sharing, below).
- **Edit** — click the pencil icon on a category's header to rename it, and
  (see Sharing, below) turn Shared on if it isn't already - Shared can
  never be turned back off, so once it's on the checkbox is gone.
- **Delete** — click the trash icon on a category's header. This isn't a
  single click: a confirmation prompt must be accepted first - except a
  non-Shared, empty category, which is deleted immediately, since nothing
  beyond its own name is at stake. For a non-Shared category with snippets,
  they move to Uncategorized rather than being deleted with it. A Shared
  category is all-or-nothing instead: the same delete also removes it from
  Firestore, and its snippets are deleted outright too, not uncategorized -
  matching what every other device sharing that category ends up doing on
  the same delete. That second, larger consequence gets its own, separate
  confirmation - spelling out how many snippets are about to be permanently
  lost - on top of the first.
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

**Sharing.** A category can be marked **Shared** (a checkbox in the New or
Edit Category dialog) to sync its snippets - text and images - to a
Firestore database, and pull down matching changes from any other machine
sharing that same category, in near-realtime and without polling. Turning
Shared on for an existing category pushes everything already in it, not
just changes from that point on. Shared is one-way: once on, it can never
be turned back off, so a category that's already Shared no longer shows
the checkbox at all when edited. Each user points the app at their own
Firebase project:
- **Settings** - opened via the cog icon in the title bar - holds a
  GCP service-account key (pasted or loaded from its downloaded `.json`
  file), encrypted at rest with Windows DPAPI. A **Configure...** button
  tests the connection and, on success, lists every Shared category
  already in the remote database with a checkbox each - uncheck any this
  machine shouldn't sync or show (at most 10 at once, a Firestore limit),
  then **OK**, then **Save**. Unchecked categories are filtered out at the
  Firestore query level, so their snippets - images especially - are never
  even downloaded, not just hidden after the fact.
- Which categories are hidden is remembered per machine, as the *unchecked*
  set - so a newly-shared category from any machine shows up on every
  other machine by default; you opt individual machines out; you don't opt
  categories in. Unchecking one that's already synced here removes it and
  its snippets from this machine's local database only, never from the
  shared database - with a confirmation first if any of its snippets might
  not have finished syncing yet. Re-checking it re-downloads it.
- While a category is Shared but Firebase hasn't been set up or isn't
  currently connected, its header shows a warning icon; clicking it opens
  Settings.
- Deleting a Shared category always deletes its Firestore copy, and its
  snippets, too (see Delete, above) - there's no way to delete it locally
  while leaving the cloud copy, or its snippets, in place. This is
  different from hiding it (above), which only ever affects this machine.
- Category order is never synced - different machines may want different
  priorities. A Shared category, whenever it first appears on a machine
  (via normal sync or by re-checking it in Configure), lands at the top of
  that machine's list, ahead of every other category; from then on it
  reorders the same as any other category, locally, on that machine only.
- Conflicts between two machines' offline edits are resolved by last
  writer wins, silently - there's no merge or conflict UI.
- Images larger than Firestore's per-document limit are chunked
  automatically; this and the general size of shared data are a real
  scaling limit for categories with many/large images, not just a
  theoretical one.

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

- No search/filter over snippets.
- No clipboard formats other than plain text and images (rich text, files,
  and anything else are ignored).
