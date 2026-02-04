# Command Palette Implementation Plan

The Command Palette will provide a centralized interface for advanced actions, specifically searching within notes and toggling advanced search features.

## Proposed Changes

### [Component] Command Palette
#### [NEW] [CommandPalette.svelte](file:///c:/Users/olduh/dev/dendrOnline/dendrOnlineSPA/wwwroot/components/CommandPalette.svelte)
- Create a modal-like overlay (or a fixed top bar) with a search input.
- Implement keyboard-driven navigation for search results.
- Add a toggle for "Search in Note Content".
- Emit events or use a store to communicate search actions.

### [View] Tree Integration
#### [MODIFY] [Tree.svelte](file:///c:/Users/olduh/dev/dendrOnline/dendrOnlineSPA/wwwroot/components/Tree.svelte)
- Register a global `keydown` listener (e.g., `Ctrl+P` or `Ctrl+K`) to open the Command Palette.
- Use the Command Palette as an alternative or enhancement to the existing `TreeFilter`.

### [View] Stash Integration
#### [MODIFY] [StashPage.svelte](file:///c:/Users/olduh/dev/dendrOnline/dendrOnlineSPA/wwwroot/components/stash/StashPage.svelte)
- Similarly, add a global `keydown` listener to open the Command Palette from the Stash view.

### [Misc] Global Shortcut
#### [MODIFY] [App.svelte](file:///c:/Users/olduh/dev/dendrOnline/dendrOnlineSPA/wwwroot/App.svelte)
- Consider adding the global shortcut at the `App.svelte` level to ensure it's available everywhere.

### tasks 

  - [x] Research existing shortcut and search implementation
  - [ ] Design the command palette UI
  - [ ] Implement the command palette component
  - [ ] Integrate command palette into Tree view
  - [ ] Integrate command palette into Stash view
  - [ ] Add command palette entry to enable search in note
  - [ ] Implement keyboard shortcut to open command palette
  - [ ] Verify functionality and shortcut overriding

## Verification Plan

### Manual Verification
- Press `Ctrl+K` (or the chosen shortcut) to toggle the Command Palette.
- Perform searches and ensure results from the backend are displayed.
- Toggle "Search in Note" and verify results include content matches.
- Navigate results using arrow keys and select with 'Enter'.
- Verify browser shortcuts (like `Ctrl+P`) are overridden when the command palette is active.
