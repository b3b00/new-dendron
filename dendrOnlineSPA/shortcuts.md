# DendrOnline Keyboard Shortcuts & Commands

This document lists the keyboard shortcuts and command palette commands available across the different views.

## Global / Common Shortcuts

Many shortcuts are accessible in most views if a note is currently active in context.

* **Toggle Command Palette**: `Ctrl+K`, `Meta+K`, `Ctrl+Alt+K`, `Ctrl+Meta+K`
* **Go to Tree View**: `Ctrl+Alt+T`, `Ctrl+Meta+T`
* **Go to Stashes View**: `Ctrl+Alt+S`, `Ctrl+Meta+S`
* **View Active Note**: `Ctrl+Alt+V`, `Ctrl+Meta+V`
* **Edit Active Note**: `Ctrl+Alt+E`, `Ctrl+Meta+E`, `AltGraph+E`

---

## View Mode (`View.svelte`)

When viewing a note, the following shortcuts are available:

| Action | Shortcut |
| --- | --- |
| Edit current note | `Ctrl+Alt+E`, `Ctrl+Meta+E` |
| View Tree | `Ctrl+Alt+T`, `Ctrl+Meta+T` |
| View Stashes | `Ctrl+Alt+S`, `Ctrl+Meta+S` |

---

## Edit Mode (`Edit.svelte`)

When editing a note:

| Action | Shortcut |
| --- | --- |
| Save changes | `Ctrl+S`, `Meta+S`, `Ctrl+Alt+S`, `Ctrl+Meta+S` |
| View current note | `Ctrl+Alt+V`, `Ctrl+Meta+V` |
| View Tree | `Ctrl+Alt+T`, `Ctrl+Meta+T` |
| View Stashes | `Ctrl+Alt+S`, `Ctrl+Meta+S` | *(If save uses `Ctrl+Alt+S`, they might overlap)* |

---

## Tree View (`Tree.svelte`)

Keyboard shortcuts:

| Action | Shortcut |
| --- | --- |
| Toggle Command Palette | `Ctrl+K`, `Meta+K`, `Ctrl+Alt+K`, `Ctrl+Meta+K` |
| Edit current note | `Ctrl+Alt+E`, `Ctrl+Meta+E`, `AltGraph+E` |
| View current note | `Ctrl+Alt+V`, `Ctrl+Meta+V` |
| Reload Tree | `Ctrl+Alt+R`, `Ctrl+Meta+R` |
| View Tree | `Ctrl+Alt+T` |
| View Stashes | `Ctrl+Alt+S`, `Ctrl+Meta+S` |

### Tree Command Palette Commands
When the Command Palette is opened in the Tree View (`Ctrl+K`):

| Command | Description |
| --- | --- |
| `>reload` | Reload the current repository tree from backend |
| `>create [name]` | Create a new note |

---

## Stashes View (`StashPage.svelte`)

Keyboard shortcuts:

| Action | Shortcut |
| --- | --- |
| Toggle Command Palette | `Ctrl+K`, `Meta+K`, `Ctrl+Alt+K`, `Ctrl+Meta+K` |
| Edit current note | `Ctrl+Alt+E`, `Ctrl+Meta+E` |
| View current note | `Ctrl+Alt+V`, `Ctrl+Meta+V` |
| Reload Stashes | `Ctrl+Alt+R`, `Ctrl+Meta+R` |
| View Tree | `Ctrl+Alt+T` |
| View Stashes | `Ctrl+Alt+S`, `Ctrl+Meta+S` |

### Stash Command Palette Commands
When the Command Palette is opened in the Stashes View (`Ctrl+K`):

| Command | Description |
| --- | --- |
| `>reloadAll` | Reload all categories and notes from server |
| `>reload` | Reload current category notes |
| `>add` | Add a new note to the current category |
| `>delete` | Delete a note from the current category |
| `>promote` | Promote a stash note to a tree note |

---

## Dialogs

### Autocomplete Prompt Dialog
Used for selecting notes (e.g., when promoting a stash note to a tree note).

| Action | Shortcut |
| --- | --- |
| Select highlighted item or confirm | `Enter` |
| Move selection down | `ArrowDown` |
| Move selection up | `ArrowUp` |
| Cycle through items | `Tab` |
| Cycle through items backwards | `Shift+Tab` |
| Close dropdown / Cancel | `Escape` |
