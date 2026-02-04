<script lang="ts">
    import { onMount, getContext } from 'svelte';
    import { StashApi, type StashCategory, type StashNote, searchStashNotesWithExcerpt } from '../../scripts/stashApi';
    import { StashCache } from '../../scripts/stashCache';
    import CategorySelector from './CategorySelector.svelte';
    import SearchBox from './SearchBox.svelte';
    import NotesList from './NotesList.svelte';
    import CommandPalette from '../CommandPalette.svelte';
    import ConfirmDialog from '../ConfirmDialog.svelte';
    import { Context } from 'svelte-simple-modal';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faSpinner, faSync } from '@fortawesome/free-solid-svg-icons/index.js';
    import { push } from 'svelte-spa-router';
    import type { Note } from '../../scripts/types';

    let categories: StashCategory[] = [];
    let selectedCategory: StashCategory | null = null;
    let notes: StashNote[] = [];
    let searchTerm = '';
    let isLoading = false;
    let isReloadingAll = false;
    let notification: { message: string, type: 'success' | 'warning' | 'error' } | null = null;
    let showingAddNoteEditor = false;

    let expandedNoteId: string | null = null;
    let commandPaletteVisible = false;
    const modal = getContext<Context>('simple-modal');

    function handleGlobalKeydown(e: KeyboardEvent) {
        if (((e.ctrlKey || e.metaKey) && e.altKey && e.key === 'k')
        || ((e.ctrlKey || e.metaKey) && e.key === 'k')) {
            e.preventDefault();
            commandPaletteVisible = !commandPaletteVisible;
        }
    }

    async function handleNoteSelect(event: CustomEvent<Note>) {
        const note = event.detail;
        // If the note has a categoryId, ensure that category is loaded
        if (note.header.categoryId) {
            if (!selectedCategory || selectedCategory.id !== note.header.categoryId) {
                // Find the category in the list
                let category = categories.find(c => c.id === note.header.categoryId);
                if (!category) {
                    // If not found, reload all categories
                    await loadCategoriesWithNotes();
                    category = categories.find(c => c.id === note.header.categoryId);
                }
                if (category) {
                    await handleCategorySelect(category);
                }
            }
            // Set search term and expand the note
            searchTerm = note.header.title;
            expandedNoteId = note.header.id;
        } else {
            push(`/view/${note.header.name}`);
        }
    }

    onMount(async () => {
        await loadCategoriesWithNotes();
    });

    async function loadCategoriesWithNotes() {
        isLoading = true;
        
        const result = await StashApi.getCategoriesWithNotes();
        
        if (result.isOk && result.theResult) {
            // Extract categories from the result
            categories = result.theResult.map(item => item.category);
            
            // If a category is selected, update its notes from cache
            if (selectedCategory) {
                const categoryData = result.theResult.find(item => item.category.id === selectedCategory!.id);
                if (categoryData) {
                    selectedCategory = categoryData.category;
                    notes = categoryData.notes;
                }
            }
        } else {
            showError(result.errorMessage || 'Failed to load categories');
        }
        
        isLoading = false;
    }

    async function handleCategorySelect(category: StashCategory) {
        selectedCategory = category;
        expandedNoteId = null; // Reset expansion when category changes manually
        // Notes are already loaded from initial fetch, just get from cache
        const result = await StashApi.getNotes(category.id);
        if (result.isOk && result.theResult) {
            notes = result.theResult;
        }
    }

    async function handleCategoryCreated() {
        await loadCategoriesWithNotes();
        showSuccess('Category created successfully');
    }

    async function handleCategoryDeleted(event: CustomEvent<string>) {
        const deletedCategoryId = event.detail;
        if (selectedCategory?.id === deletedCategoryId) {
            selectedCategory = null;
            notes = [];
        }
        await loadCategoriesWithNotes();
        showSuccess('Category deleted successfully');
    }

    async function handleCategoryUpdated() {
        await loadCategoriesWithNotes();
        // Re-select the category to refresh its data
        if (selectedCategory) {
            const updated = categories.find(c => c.id === selectedCategory.id);
            if (updated) {
                selectedCategory = updated;
                // Get notes from cache
                const result = await StashApi.getNotes(updated.id);
                if (result.isOk && result.theResult) {
                    notes = result.theResult;
                }
            }
        }
        showSuccess('Category updated successfully');
    }

    async function handleCategoryReloaded(event: CustomEvent) {
        const { category, notes: reloadedNotes } = event.detail;
        
        // Update the category in the list
        const index = categories.findIndex(c => c.id === category.id);
        if (index >= 0) {
            categories[index] = category;
            categories = categories; // Trigger reactivity
        }
        
        // Update selected category
        selectedCategory = category;
        
        // Update notes directly from the reload result
        notes = reloadedNotes;
        
        showSuccess(`Category <b>${category.title}</b> reloaded successfully`);
    }

    async function loadNotes() {
        if (!selectedCategory) return;
        
        // Notes are already cached from initial load, just fetch from cache
        const result = await StashApi.getNotes(selectedCategory.id);
        
        if (result.isOk && result.theResult) {
            notes = result.theResult;
        } else {
            showError(result.errorMessage || 'Failed to load notes');
            notes = [];
        }
    }

    async function handleNoteAdded() {
        await loadNotes();
        // Refresh categories to update note count
        const result = await StashApi.getCategories();
        if (result.isOk && result.theResult) {
            categories = result.theResult;
        }
        showSuccess('Note added successfully');
    }

    async function handleNoteUpdated() {
        await loadNotes();
        showSuccess('Note updated successfully');
    }

    async function handleNoteDeleted() {
        await loadNotes();
        // Refresh categories to update note count
        const result = await StashApi.getCategories();
        if (result.isOk && result.theResult) {
            categories = result.theResult;
        }
        showSuccess('Note deleted successfully');
    }

    function showNotification(message: string, type: 'success' | 'warning' | 'error') {
        notification = { message, type };
        setTimeout(() => {
            notification = null;
        }, 3000);
    }

    function showSuccess(message: string) {
        showNotification(message, 'success');
    }

    function showWarning(message: string) {
        showNotification(message, 'warning');
    }

    function showError(message: string) {
        showNotification(message, 'error');
    }

    function handleSearchChange(term: string) {
        searchTerm = term;
    }

    async function handleReloadAll() {
        isReloadingAll = true;
        
        // Clear all caches to force fresh fetch
        StashCache.clearAll();
        
        // Force bypass backend cache as well
        const result = await StashApi.getCategoriesWithNotes(true);
        
        if (result.isOk && result.theResult) {
            categories = result.theResult.map(item => item.category);
            
            // Update selected category and notes if one is selected
            if (selectedCategory) {
                const categoryData = result.theResult.find(item => item.category.id === selectedCategory!.id);
                if (categoryData) {
                    selectedCategory = categoryData.category;
                    notes = categoryData.notes;
                }
            }
            
            showSuccess('All categories reloaded successfully');
        } else {
            showError(result.errorMessage || 'Failed to reload categories');
        }
        
        isReloadingAll = false;
    }

    // Use StashApi.searchStashNotesWithExcerpt for search callback
    $: stashCommands = {
        'reloadAll': {
            action: () => handleReloadAll(),
            description: 'Reload all categories and notes from server'
        },
        'reload': {
            action: async () => {
                if (!selectedCategory) {
                   showWarning('Please select a category first');
                   return;
                }
                isLoading = true;
                // Call backend reload API
                const result = await StashApi.reloadCategory(selectedCategory.id);
                if (result.isOk && result.theResult) {
                    const categoryTitle = selectedCategory.title;
                    // Update selected category and notes with fresh data
                    selectedCategory = result.theResult.category;
                    notes = result.theResult.notes;
                    // Optionally update categories list
                    const index = categories.findIndex(c => c.id === selectedCategory.id);
                    if (index >= 0) {
                        categories[index] = result.theResult.category;
                        categories = categories;
                    }
                    showSuccess(`Category <b>${categoryTitle}</b> reloaded successfully`);
                } else {
                    showError(result.errorMessage || 'Failed to reload category');
                }
                isLoading = false;
            },
            description: selectedCategory ? `Reload category <b>${selectedCategory.title}</b>` : 'Reload current category notes'
        },
        'add': {
            action: () => {
                if (!selectedCategory) {
                    showWarning('Please select a category first');
                    return;
                }
                showingAddNoteEditor = true;
            },
            description: 'Add a new note to the current category'
        },
        'delete': {
            action: async (noteId) => {
                if (!selectedCategory || !noteId) return;
                
                const note = notes.find(n => n.id === noteId);
                const noteTitle = note?.title || noteId;

                modal.open(
                    ConfirmDialog,
                    {
                        message: `Are you sure you want to delete the note <b>${noteTitle}</b>?`,
                        detail: 'This action cannot be undone.',
                        oncancel: () => {},
                        onOkay: async () => {
                            const result = await StashApi.deleteNote(selectedCategory.id, noteId);
                            if (result.isOk) {
                                await handleNoteDeleted();
                            } else {
                                showError(result.errorMessage || 'Failed to delete note');
                            }
                        }
                    },
                    {
                        closeButton: true,
                        closeOnEsc: true,
                        closeOnOuterClick: true,
                    }
                );
            },
            description: 'Delete a note from the current category',
            suggestions: async (arg) => {
                if (!selectedCategory) {
                    showError('A category must be selected to delete notes');
                    return [];
                }
                const filteredNotes = arg 
                    ? notes.filter(n => 
                        (n.title && n.title.toLowerCase().includes(arg.toLowerCase())) || 
                        (n.content && n.content.toLowerCase().includes(arg.toLowerCase()))
                      )
                    : notes;

                return filteredNotes.map(n => ({
                    label: n.title || n.id,
                    value: n.id,
                    description: n.content.substring(0, 50) + (n.content.length > 50 ? '...' : '')
                }));
            },
            suggestionsDescription: 'Select a note to delete'
        }
    };
</script>

<svelte:window on:keydown={handleGlobalKeydown} />

<CommandPalette
    searchCallback={(pattern, searchInContent) => searchStashNotesWithExcerpt(pattern, searchInContent, selectedCategory?.id)}
    commands={stashCommands}
    bind:visible={commandPaletteVisible}
    on:select={handleNoteSelect}
    on:close={() => commandPaletteVisible = false}
>
    <svelte:fragment slot="result" let:result>
        <div class="note-title">
            {result.header?.title || result.header?.id}
            {#if result.header?.categoryTitle}
                <span class="note-category">({result.header.categoryTitle})</span>
            {/if}
        </div>
        {#if result.header?.description}
            <div class="note-desc">{@html result.header.description}</div>
        {/if}
    </svelte:fragment>
</CommandPalette>

<div class="stash-page">
    {#if notification}
            <div class="alert alert-{notification.type}">
                {@html notification.message}
            </div>
    {/if}

    <div class="stash-container">
        <div class="stash-header">
            <CategorySelector 
                {categories}
                {selectedCategory}
                on:select={(e) => handleCategorySelect(e.detail)}
                on:created={handleCategoryCreated}
                on:updated={handleCategoryUpdated}
                on:deleted={handleCategoryDeleted}
                on:reloaded={handleCategoryReloaded}
            />
            <button 
                class="btn-reload-all" 
                on:click={handleReloadAll}
                disabled={isReloadingAll || isLoading}
                title="Reload all categories and notes"
            >
                <Fa icon={faSync} spin={isReloadingAll} />
                <span>Reload All</span>
            </button>
        </div>

        {#if isLoading}
            <div class="loading">
                <Fa icon={faSpinner} spin />
                <span>Loading...</span>
            </div>
        {:else if selectedCategory}
            <SearchBox 
                value={searchTerm}
                on:change={(e) => handleSearchChange(e.detail)}
            />

            <NotesList 
                {notes}
                {searchTerm}
                {expandedNoteId}
                categoryId={selectedCategory.id}
                bind:showingAddNote={showingAddNoteEditor}
                on:added={handleNoteAdded}
                on:updated={handleNoteUpdated}
                on:deleted={handleNoteDeleted}
            />
        {:else if categories.length > 0}
            <div class="empty-state">
                <p>Select a category to view your notes</p>
            </div>
        {:else}
            <div class="empty-state">
                <p>No categories yet. Create your first category to get started!</p>
            </div>
        {/if}
    </div>
</div>

<style>
    .stash-page {
        padding: 20px;
        max-width: 1200px;
        margin: 0 auto;
    }

    .stash-header {
        margin-bottom: 30px;
        display: flex;
        gap: 3px;
        align-items: flex-start;
        flex-wrap: wrap;
    }

    .btn-reload-all {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 8px 16px;
        background-color: #007bff;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.95rem;
        transition: background-color 0.2s;
        white-space: nowrap;
    }

    .btn-reload-all:hover:not(:disabled) {
        background-color: #0056b3;
    }

    .btn-reload-all:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .stash-container {
        background: white;
        border-radius: 8px;
        padding: 20px;
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .loading {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 40px;
        color: #666;
    }

    .empty-state {
        text-align: center;
        padding: 60px 20px;
        color: #999;
    }

    .empty-state p {
        font-size: 1.1rem;
    }

    .alert {
        padding: 12px 16px;
        border-radius: 4px;
        margin-bottom: 16px;
        font-size: 0.95rem;
    }

    .alert-success {
        background-color: #d4edda;
        color: #155724;
        border: 1px solid #c3e6cb;
    }

    .alert-warning {
        background-color: #fff3cd;
        color: #856404;
        border: 1px solid #ffeeba;
    }

    .alert-error {
        background-color: #f8d7da;
        color: #721c24;
        border: 1px solid #f5c6cb;
    }

    .note-category {
        font-style: italic;
    }
</style>
