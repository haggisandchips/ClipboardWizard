# Changelog

Notable changes to Clipboard Wizard. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

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
