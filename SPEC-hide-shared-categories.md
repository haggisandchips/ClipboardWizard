# SPEC draft: Hide Shared Categories

Let a device connected to the remote database hide individual categories.

## Purpose

This is to allow a single remote database to contain categories relevant to
both personal and work projects. Individual devices can then pick only the
categories relevant to the device's purpose be displayed.

## Implementation

**Configure button.** Change the Test Connection button to a "Configure ..."
button. When the key has been changed (pasted, loaded, or edited) the Save
button should remain disabled until Configure has been completed successfully.
Reopening Settings without changing the key leaves Save enabled as now.

On clicking Configure access the remote database (as Test Connection did) and
download the list of categories. If the connection fails show the error as
Test Connection does now, open no dialogue, and leave Save disabled.

Otherwise present a new dialogue to the user with this list of categories with
a checkbox beside each (to the left). The list includes every Shared category
in the remote database, including ones created on this device. Selecting none
is acceptable, but at most 10 categories may be unchecked (a Firestore `not-in`
limit, see below) - OK is disabled with an explanation beyond that. Dialogue
should also have an OK button and does not need any others.

On clicking OK, if any category is unchecked, run the filtered queries (below)
once to confirm the required index exists. If it doesn't, show the index
creation link from Firestore's error in Settings and leave Save disabled.
Otherwise close the dialogue and the list of categories should be remembered
in event the user then clicks Save.

If the user clicks Cancel then forget everything.

**Save.** If the user clicks Save then setup the live connection as the
application does now. On downloading the categories however, ignore any that
were not checked. Ignored shared categories are not stored in the local
database.

The unchecked categories are persisted, by category id, alongside the
Firestore credentials in `SettingsService`. Storing the *unchecked* set
(rather than the checked one) is what lets new remote categories appear
automatically. Changing the key to a different Firebase project discards the
stored set. Existing installations upgrading to this version start with an
empty set, i.e. every current Shared category is checked.

**Reconfiguring.** If user clicks Settings -> Configure they are presented with
the list with categories checked and unchecked as appropriate. All changes made
there are applied only when the user clicks Save in the parent dialogue:

- A newly unchecked category is removed from the local database, along with
  its snippets. This is local only - it must not delete anything from the
  remote database. If the category has local snippets that haven't yet synced
  to the remote database, warn the user that they'll be lost and ask them to
  confirm before proceeding.
- A newly checked category is downloaded and saved to the local database.

**Normal operation.** When new categories are notified during normal operation
always add it to the UI as currently happens - the user needs to go through
Settings to hide it.

Snippet changes notified for ignored categories should be ignored.

**Filtering in Firestore.** Ignored categories are filtered out by the
Firestore queries themselves, not after download, since snippets (images
especially) are the heavyweight documents:

- The categories listener filters on document id `not-in` the unchecked set.
- Snippet documents gain a `CategorySyncId` field (their parent category's
  id), and the snippets collection-group listener filters on it `not-in` the
  unchecked set.
- When the unchecked set is empty no filter is applied (Firestore rejects an
  empty `not-in`, and no index is then needed).

Filtering snippets on `CategorySyncId` needs a single-field index exemption on
the `snippets` collection group for `CategorySyncId`, with collection-group
scope enabled. Add this as a new step after "2. Enable Firestore" in the
README's "Sharing across devices" setup steps. If the listener later fails at
runtime (e.g. the index is missing), Shared category headers show the existing
warning icon, which opens Settings where the index creation link is shown.

No migration: existing Firestore documents lack `CategorySyncId` and would be
excluded by `not-in`, so the remote database is cleared, every device upgraded,
and categories re-shared.

**Order.** The remote database should not have any concept of category order
because individual devices may have different priorities. `Order` is removed
from the category document and a remote category change no longer alters local
order. Shared categories downloaded from the remote database are added at the
top, in the order they arrive - both on initial download and later during
normal operation - but can be reordered the same as other categories. Shared
categories created on this device are added at the bottom, as now.

Snippet order within a Shared category still syncs as now.

**Force Sync** is dropped; Configure covers getting data flowing immediately
after pasting a key.
