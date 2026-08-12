# Changelog

Notable changes to Clipboard Wizard. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

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
