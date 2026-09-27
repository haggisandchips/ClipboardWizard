# Changelog

Notable changes to Clipboard Wizard. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added

- Per-machine control over which Shared categories actually sync/show here:
  Settings' "Configure..." button (replacing "Test Connection") tests the
  connection and then lists every Shared category in the remote database,
  letting you uncheck any this machine shouldn't sync - at most 10 at once,
  a Firestore limit. Unchecked categories (and their snippets, images
  included) are never even downloaded, not just hidden after the fact.
  Unchecking one that's already synced here removes it and its snippets
  locally only, never from the shared database - with a confirmation if any
  of its snippets might not have finished syncing yet. A newly-shared
  category (from any machine) always shows up everywhere by default; you
  opt individual machines *out*, not in.
- A Shared category first seen via sync now always appears at the top of
  the list, ahead of local categories too - category order is no longer
  synced between machines at all (different machines may want different
  priorities), only within a single machine.

### Changed

- A category's Shared setting can now be turned on at any time via Edit
  Category, not just when it's created - turning it on pushes every
  snippet already in the category, not just changes from that point on.
  It's still one-way: once Shared, a category can never be turned back
  off, so the checkbox disappears from Edit once it's on.
- Deleting an empty, non-Shared category no longer asks for confirmation -
  there's nothing at stake beyond the category's own name. Every other
  delete (Shared, or non-Shared with snippets) still confirms as before.

**Breaking**: upgrading requires clearing the remote Firestore database and
re-sharing every category from each machine - existing snippet documents
predate the `CategorySyncId` field the new per-category filtering relies on,
and would otherwise silently disappear from any machine that hides even one
category. See the README's updated "Sharing across devices" setup steps,
including a new required Firestore index.

## [1.5.0] - 2026-09-24

### Changed

- A category's Shared setting can now only be chosen when it's created -
  the Edit Category dialog only renames it, Shared can no longer be turned
  on or off afterward.
- Deleting a Shared category is now all-or-nothing: the same confirmation
  removes it from Firestore too, and its snippets are deleted outright
  rather than becoming uncategorized - matching what every other device
  sharing it already did on the same delete. A second confirmation spells
  out how many snippets are about to be permanently lost before it happens.
- The category header's wrench (rename) icon now sits next to the trash
  icon, instead of at the opposite end of the header.

### Fixed

- Editing a snippet's content could leave a stale tile showing as the
  current clipboard match after saving, if the clipboard had changed while
  the edit dialog was open - the edit only rechecked the snippet being
  edited, not the rest. Every snippet's match state is now rechecked
  after an edit, the same as after any other clipboard change.

## [1.4.1] - 2026-09-23

### Fixed

- A Shared category's snippets could stay missing until the app was
  restarted after first connecting Firestore (e.g. right after pasting a
  service-account key), since the category and snippet listeners are
  independent and Firestore doesn't guarantee the category's own put
  arrives first. A snippet whose category hasn't shown up yet is now
  buffered and applied as soon as it does, instead of being dropped.
- Editing a shared snippet's description on one machine didn't visibly
  update its tile on another, even though the change was correctly synced
  and persisted - only a restart (which reloads every tile from scratch)
  made it show up. The tile now refreshes immediately on a remote edit,
  the same as a local one.

## [1.4.0] - 2026-09-23

### Added

- Category sharing: mark a category Shared (New/Edit Category dialog) to
  sync its snippets - text and images - to a Firestore database and pull
  down matching changes from any other machine sharing that same category,
  in near-realtime and without polling. Configure a GCP service-account key
  via Settings (cog icon in the title bar), with a Test Connection check
  before saving. Images too large for a single Firestore document are
  chunked automatically.
- A Shared category's header shows a flame icon next to its name; while
  Shared is on but Firebase isn't set up or connected, a warning icon
  appears instead (or alongside), and clicking it opens Settings.

### Changed

- The category header's edit icon is now a wrench and sits to the left of
  the other icons, with a small gap separating it from them.

### Fixed

- Deleting a Shared category's "also delete from Firebase" prompt now has a
  Cancel option, so backing out there aborts the whole deletion instead of
  always deleting the category regardless.
- Copying a shared image snippet could silently do nothing after the app
  restarted or Firestore reconnected, for images large enough to be synced
  in chunks - a stale, redundant re-fetch of already-synced image data
  could clobber it locally. Image data is now fetched from Firestore at
  most once per snippet and never re-applied afterward.

## [1.3.0] - 2026-09-03

### Added

- Adding a snippet now expands whichever section (category, or the
  pinned Uncategorized bucket) receives it, if it was collapsed - a
  newly added or auto-recorded snippet could otherwise land somewhere
  invisible until you happened to expand it yourself.
- Each category header now has its own New Snippet and Add icon
  buttons, pre-populating that category - no need to pick it from the
  New Snippet dialog's category list or drag the result afterward.

## [1.2.2] - 2026-08-13

### Fixed

- Dragging a snippet toward an empty or collapsed category could land it in
  the wrong (neighbouring) category instead, since the thin/empty target
  had no tiles of its own to hit-test against. Category sections now
  correctly claim their own header, gap, and tile area as a drop target
  ahead of any neighbour's.
- The drop highlight and target no longer appear when hovering a snippet
  over the category it's already in.
- The expand/collapse chevron no longer shifts the category name next to
  it when toggled (the two chevron glyphs weren't the same width).
- Toggling an empty category no longer nudges the categories below it,
  since its (empty) snippet grid no longer reserves space it doesn't need.

## [1.2.1] - 2026-08-13

### Fixed

- Changing a snippet's category via the Edit dialog now actually moves it
  in the accordion. Previously the tile stayed under its old category
  until you dragged it - and since its category was already set to the
  new one, that drag silently did nothing either, forcing a
  clear-then-drag workaround.

## [1.2.0] - 2026-08-13

### Added

- Snippet categories: organize snippets into user-defined categories, shown
  as an expandable/collapsible accordion (with an always-last, pinned
  Uncategorized section). Create categories via a "New Category" dialog,
  delete via a confirmation-gated trash icon on the header (a category's
  snippets move to Uncategorized rather than being deleted), assign a
  snippet via the New/Edit dialog's Category dropdown or by dragging its
  tile onto a category, and reorder categories via drag-and-drop.
- Window position, size, and maximized state now persist across restarts.

### Changed

- Removed the Move up/down buttons - drag-and-drop is now the only way to
  reorder snippets.

### Fixed

- The drag-drop indicator is no longer shown for a position that wouldn't
  actually reorder anything (e.g. dropping a tile on itself or immediately
  next to where it already sits).
- Added a small margin around the snippet grid so the drop indicator is
  visible when a tile is dragged near the window edge.

## [1.1.0] - 2026-08-12

### Added

- Drag-and-drop snippet reordering: drag a tile onto another to move it
  there directly, alongside the existing Move up/down buttons. A blue edge
  indicator shows whether it'll land before or after the tile you're
  hovering over.

### Fixed

- The New/Update button in the snippet editor now enables as soon as you
  type, instead of only after the content field loses focus.

## [1.0.0] - 2026-08-12

### Added

- Persistent clipboard snippet list, backed by a local SQLite database,
  surviving across restarts.
- Automatic clipboard watching: snippets highlight as Active/Inactive
  depending on whether they match what's currently on the clipboard.
- Recording toggle to automatically capture new clipboard content (text or
  images) as snippets.
- Manual snippet creation: save the current clipboard content directly, or
  type a text snippet from scratch via a dialog.
- Image snippet support alongside text, including thumbnails on tiles and a
  read-only image preview when editing.
- Per-snippet actions: copy back to clipboard, edit (description always,
  content for text snippets), delete, and reorder within the list.
- Permanent snippet locking to protect against accidental deletion, with a
  brief, explicit unlock window when a locked snippet genuinely needs to be
  removed.
- Self-updating installer/release mechanism: checks GitHub Releases on
  startup and offers to apply new versions.
- GPLv3 license.
