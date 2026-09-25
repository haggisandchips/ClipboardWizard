# SPEC draft: hide shared categories + Force Sync

Status: **draft, not implemented**. Requirements are still being worked out -
this captures what's settled and, more importantly, what isn't, so it can be
reviewed before any plan or code is written. Not yet folded into `SPEC.md`.

## Purpose

Let a device hide individual Shared categories from its own main window
(without affecting sync itself, or other devices), and give a way to force
an immediate Firestore sync from the Settings dialog rather than waiting on
the realtime listener.

## Settled

- **Per-category, not global.** Each Shared category can be hidden or shown
  independently on a given device - not one all-or-nothing switch for every
  Shared category at once.
- **Hiding removes the category from the main window entirely** - not a
  grayed-out or collapsed placeholder that still occupies visual space.
- **Force Sync is still wanted**, but not wired through the Settings
  dialog's Save/Cancel flow the way first proposed (see below) - that
  caused problems and needs a different shape.

## Open questions

### 1. What does "hidden" mean for local storage?

- **A - UI-only filter.** The category and its snippets stay fully
  persisted locally (SQLite); hiding just removes it from what's rendered.
  Un-hiding is instant, no network needed.
- **B - Locally dropped.** A hidden category (and its snippets) is removed
  from the local database entirely. Un-hiding re-pulls it from Firestore.
  Saves local storage but:
  - What happens to any local edits/snippets not yet confirmed synced at
    the moment it's hidden?
  - Un-hiding requires Firestore to be reachable - what happens if it isn't?

### 2. Where does category order fit once hiding is per-category?

- Idea: pin all Shared categories at the top of the list, ahead of every
  local (non-Shared) category, so ordering only has to be solved within
  each group separately rather than as one interleaved list.
- Does Shared-category order need to sync via Firestore too (so every
  device sees the same order), or can it stay purely local per device?
  - Syncing it means a new last-writer-wins-style conflict surface for
    order, the same shape as the existing per-category `ModifiedAtUtc`
    mechanism - worth it, or is a simpler rule (alphabetical, creation
    order) enough?
  - Staying local-only means two devices can legitimately show the same
    Shared categories in different orders.
- If a hidden category is still in local storage (option A above), does it
  still hold a position in the order? Or is order only ever assigned/read
  for categories that are currently visible?
- Reordering UX: could hidden categories be surfaced temporarily while a
  drag-reorder is in progress, so their position can still be adjusted
  without fully un-hiding them - or is that unnecessary complexity if a
  hidden category doesn't need a position at all while it's hidden?

### 3. Where/how does the user toggle "hidden" for a category?

No longer a single Settings-dialog checkbox now that it's per-category.
Candidates, not yet chosen: a header icon/button next to the existing
wrench/trash icons, an Edit Category dialog checkbox, a context menu entry.

### 4. Force Sync

- Still wanted, to get data flowing immediately after pasting a key,
  before Save.
- The earlier idea of gating the hide setting behind "has this device ever
  completed a sync" may not even be needed anymore: a category can only be
  hidden if it's already known locally, which already requires it to have
  synced at least once to exist here in the first place. So Force Sync and
  the hide feature may end up independent of each other.
- Where should the button live, and should it avoid mutating the live
  Firestore connection (so it's safe to click and then Cancel out of the
  dialog), or is that no longer a real constraint once it's not tied into
  the Settings Save/Cancel flow?

## Out of scope for now

Nothing beyond "Settled" above is decided. This document exists to lay out
the shape of the problem, not to lock in an approach - implementation
planning should wait until these are resolved.
