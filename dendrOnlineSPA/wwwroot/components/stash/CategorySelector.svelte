<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { getContext } from 'svelte';
    import { StashApi, type StashCategory, type CreateCategoryRequest } from '../../scripts/stashApi';
    import PromptDialog from '../PromptDialog.svelte';
    import ConfirmDialog from '../ConfirmDialog.svelte';
    import { Context } from 'svelte-simple-modal';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faPlus, faTrash, faPencil, faSync } from '@fortawesome/free-solid-svg-icons/index.js';

    export let categories: StashCategory[] = [];
    export let selectedCategory: StashCategory | null = null;

    const dispatch = createEventDispatcher();
    const modal = getContext<Context>('simple-modal');
    
    let isReloading = false;

    function handleSelect(event: Event) {
        const select = event.target as HTMLSelectElement;
        const categoryId = select.value;
        
        if (categoryId) {
            const category = categories.find(c => c.id === categoryId);
            if (category) {
                dispatch('select', category);
            }
        }
    }

    function showCreateDialog() {
        modal.open(
            PromptDialog,
            {
                message: 'Create New Category',
                hasForm: true,
                label: 'Category Title',
                descLabel: 'Description (optional)',
                hasDescription: true,
                oncancel: () => {},
                onOkay: async (title: string, description: string) => {
                    if (!title || title.trim() === '') {
                        alert('Category title is required');
                        return;
                    }

                    const request: CreateCategoryRequest = {
                        title: title.trim(),
                        description: description?.trim() || ''
                    };

                    const result = await StashApi.createCategory(request);
                    
                    if (result.isOk) {
                        dispatch('created');
                    } else {
                        alert(`Failed to create category: ${result.errorMessage}`);
                    }
                }
            },
            {
                closeButton: true,
                closeOnEsc: true,
                closeOnOuterClick: true,
            }
        );
    }

    function showEditDialog() {
        if (!selectedCategory) return;
        
        const categoryToEdit = selectedCategory;
        modal.open(
            PromptDialog,
            {
                message: 'Edit Category',
                hasForm: true,
                label: 'Category Title',
                descLabel: 'Description (optional)',
                hasDescription: true,
                parent: categoryToEdit.title,
                initialDescription: categoryToEdit.description || '',
                oncancel: () => {},
                onOkay: async (title: string, description: string) => {
                    if (!title || title.trim() === '') {
                        alert('Category title is required');
                        return;
                    }

                    const result = await StashApi.updateCategory(categoryToEdit.id, {
                        title: title.trim(),
                        description: description?.trim() || ''
                    });
                    
                    if (result.isOk) {
                        dispatch('updated');
                    } else {
                        alert(`Failed to update category: ${result.errorMessage}`);
                    }
                }
            },
            {
                closeButton: true,
                closeOnEsc: true,
                closeOnOuterClick: true,
            }
        );
    }

    function showDeleteDialog() {
        if (!selectedCategory) return;
        
        const categoryToDelete = selectedCategory;
        modal.open(
            ConfirmDialog,
            {
                message: `Delete category "${categoryToDelete.title}"?`,
                details: `This will permanently delete the category and all ${categoryToDelete.notesCount} note(s) in it. This action cannot be undone.`,
                confirmText: 'Delete',
                confirmClass: 'danger',
                oncancel: () => {},
                onOkay: async () => {
                    const result = await StashApi.deleteCategory(categoryToDelete.id);
                    
                    if (result.isOk) {
                        dispatch('deleted', categoryToDelete.id);
                    } else {
                        alert(`Failed to delete category: ${result.errorMessage}`);
                    }
                }
            },
            {
                closeButton: true,
                closeOnEsc: true,
                closeOnOuterClick: false,
            }
        );
    }

    async function handleReload() {
        if (!selectedCategory || isReloading) return;
        
        isReloading = true;
        const result = await StashApi.reloadCategory(selectedCategory.id);
        isReloading = false;
        
        if (result.isOk && result.theResult) {
            // Update selected category with fresh data
            selectedCategory = result.theResult.category;
            // Dispatch event with both category and notes
            dispatch('reloaded', result.theResult);
        } else {
            alert(`Failed to reload category: ${result.errorMessage}`);
        }
    }
</script>

<div class="category-selector">
    <div class="selector-group">
        <label for="category-select">Category:</label>
        <select 
            id="category-select"
            value={selectedCategory?.id || ''}
            on:change={handleSelect}
        >
            <option value="">Select a category...</option>
            {#each categories as category}
                <option value={category.id}>
                    {category.description ?? category.title} ({category.notesCount} notes)
                </option>
            {/each}
        </select>
    </div>

    <button class="btn-create" on:click={showCreateDialog}>
        <Fa icon={faPlus} />
        <span>New Category</span>
    </button>

    {#if selectedCategory}
        <div class="category-info">
            <div class="category-header">
                <div class="category-text">
                    <strong>{selectedCategory.title}</strong>
                    {#if selectedCategory.description}
                        <p class="description">{selectedCategory.description}</p>
                    {/if}
                </div>
                <div class="button-group">
                    <button 
                        class="btn-reload" 
                        on:click={handleReload} 
                        title="Reload category from GitHub"
                        disabled={isReloading}
                    >
                        <Fa icon={faSync} spin={isReloading} />
                    </button>
                    <button class="btn-edit" on:click={showEditDialog} title="Edit category">
                        <Fa icon={faPencil} />
                    </button>
                    <button class="btn-delete" on:click={showDeleteDialog} title="Delete category">
                        <Fa icon={faTrash} />
                    </button>
                </div>
            </div>
        </div>
    {/if}
</div>

<style>
    .category-selector {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin-bottom: 24px;
        padding-bottom: 24px;
        border-bottom: 1px solid #e0e0e0;
    }

    .selector-group {
        display: flex;
        align-items: center;
        gap: 12px;
        flex-wrap: wrap;
    }

    label {
        font-weight: 600;
        color: #333;
        font-size: 0.95rem;
    }

    select {
        flex: 1;
        min-width: 250px;
        padding: 10px 12px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 0.95rem;
        background-color: white;
        cursor: pointer;
        transition: border-color 0.2s;
    }

    select:hover {
        border-color: #999;
    }

    select:focus {
        outline: none;
        border-color: #4a90e2;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    .btn-create {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 16px;
        background-color: #4a90e2;
        color: white;
        border: none;
        border-radius: 4px;
        font-size: 0.95rem;
        cursor: pointer;
        transition: background-color 0.2s;
        align-self: flex-start;
    }

    .btn-create:hover {
        background-color: #357abd;
    }

    .btn-create:active {
        background-color: #2868a8;
    }

    .category-info {
        padding: 12px;
        background-color: #f8f9fa;
        border-radius: 4px;
        border-left: 3px solid #4a90e2;
    }

    .category-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 12px;
    }

    .category-text {
        flex: 1;
    }

    .category-info strong {
        display: block;
        margin-bottom: 4px;
        color: #333;
    }

    .description {
        margin: 0;
        color: #666;
        font-size: 0.9rem;
    }

    .button-group {
        display: flex;
        gap: 8px;
        flex-shrink: 0;
    }

    .btn-reload {
        padding: 8px 12px;
        background-color: #17a2b8;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-reload:hover:not(:disabled) {
        background-color: #138496;
    }

    .btn-reload:active:not(:disabled) {
        background-color: #117a8b;
    }

    .btn-reload:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .btn-edit {
        padding: 8px 12px;
        background-color: #6c757d;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-edit:hover {
        background-color: #5a6268;
    }

    .btn-edit:active {
        background-color: #545b62;
    }

    .btn-delete {
        padding: 8px 12px;
        background-color: #dc3545;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-delete:hover {
        background-color: #c82333;
    }

    .btn-delete:active {
        background-color: #bd2130;
    }

    @media (max-width: 600px) {
        .selector-group {
            flex-direction: column;
            align-items: stretch;
        }

        select {
            min-width: 100%;
        }
    }
</style>
