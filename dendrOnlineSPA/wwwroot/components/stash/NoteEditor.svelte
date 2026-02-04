<script lang="ts">
    import { createEventDispatcher, onMount } from 'svelte';
    import { getContext } from 'svelte';
    import { StashApi } from '../../scripts/stashApi';
    import ErrorDialog from '../ErrorDialog.svelte';
    import { Context } from 'svelte-simple-modal';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faCheck, faTimes, faSpinner } from '@fortawesome/free-solid-svg-icons/index.js';

    export let categoryId: string;
    export let noteId: string | null = null; // null for new note, ID for editing
    export let initialContent = '';

    const dispatch = createEventDispatcher();
    const modal = getContext<Context>('simple-modal');

    let content = initialContent;
    let isSaving = false;
    let textareaElement: HTMLTextAreaElement;

    onMount(() => {
        if (textareaElement) {
            textareaElement.focus();
            // Optional: move cursor to end if needed, but for "# " it's already there
        }
    });

    async function handleSave() {
        if (!content.trim()) {
            alert('Note content cannot be empty');
            return;
        }

        isSaving = true;

        try {
            let result;
            if (noteId) {
                // Update existing note
                result = await StashApi.updateNote(categoryId, noteId, content);
            } else {
                // Create new note
                result = await StashApi.createNote(categoryId, content);
            }

            if (result.isOk) {
                dispatch('save', content);
            } else {
                // Handle conflict
                if (result.code === 409) {
                    modal.open(
                        ErrorDialog,
                        {
                            message: 'Conflict Detected',
                            detail: result.errorMessage + '\n\nThe note has been modified by someone else. Please refresh and try again.',
                        },
                        {
                            closeButton: true,
                            closeOnEsc: true,
                            closeOnOuterClick: true,
                        }
                    );
                } else {
                    alert(`Failed to save note: ${result.errorMessage}`);
                }
            }
        } catch (error) {
            alert(`Error saving note: ${error.message}`);
        } finally {
            isSaving = false;
        }
    }

    function handleCancel() {
        dispatch('cancel');
    }
</script>

<div class="note-editor">
    <textarea
        bind:this={textareaElement}
        bind:value={content}
        placeholder="Write your note here... (Markdown supported)"
        disabled={isSaving}
    ></textarea>

    <div class="editor-footer">
        <div class="char-count">
            {content.length} characters
        </div>
        <div class="editor-actions">
            <button 
                class="btn btn-cancel" 
                on:click={handleCancel}
                disabled={isSaving}
            >
                <Fa icon={faTimes} />
                <span>Cancel</span>
            </button>
            <button 
                class="btn btn-save" 
                on:click={handleSave}
                disabled={isSaving || !content.trim()}
            >
                {#if isSaving}
                    <Fa icon={faSpinner} spin />
                    <span>Saving...</span>
                {:else}
                    <Fa icon={faCheck} />
                    <span>Save</span>
                {/if}
            </button>
        </div>
    </div>
</div>

<style>
    .note-editor {
        display: flex;
        flex-direction: column;
        gap: 12px;
    }

    textarea {
        width: 100%;
        min-height: 200px;
        padding: 12px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-family: inherit;
        font-size: 0.95rem;
        line-height: 1.6;
        resize: vertical;
        transition: border-color 0.2s;
    }

    textarea:focus {
        outline: none;
        border-color: #4a90e2;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    textarea:disabled {
        background-color: #f5f5f5;
        cursor: not-allowed;
    }

    textarea::placeholder {
        color: #999;
    }

    .editor-footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .char-count {
        font-size: 0.85rem;
        color: #999;
    }

    .editor-actions {
        display: flex;
        gap: 8px;
    }

    .btn {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 8px 16px;
        border: none;
        border-radius: 4px;
        font-size: 0.9rem;
        cursor: pointer;
        transition: background-color 0.2s, opacity 0.2s;
    }

    .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .btn-cancel {
        background-color: #6c757d;
        color: white;
    }

    .btn-cancel:hover:not(:disabled) {
        background-color: #5a6268;
    }

    .btn-save {
        background-color: #28a745;
        color: white;
    }

    .btn-save:hover:not(:disabled) {
        background-color: #218838;
    }

    @media (max-width: 600px) {
        .editor-footer {
            flex-direction: column;
            gap: 12px;
            align-items: stretch;
        }

        .editor-actions {
            width: 100%;
        }

        .btn {
            flex: 1;
            justify-content: center;
        }
    }
</style>
