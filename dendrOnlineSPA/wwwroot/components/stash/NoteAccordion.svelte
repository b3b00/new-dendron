<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { getContext, setContext } from 'svelte';
    import SvelteMarkdown from 'svelte-markdown';
    import { StashApi, type StashNote } from '../../scripts/stashApi';
    import { StashCache } from '../../scripts/stashCache';
    import NoteEditor from './NoteEditor.svelte';
    import ConfirmDialog from '../ConfirmDialog.svelte';
    import { Context } from 'svelte-simple-modal';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faPen, faTrash, faChevronDown, faChevronRight } from '@fortawesome/free-solid-svg-icons/index.js';
    import CodeMarkdown from '../CodeMarkdown.svelte';
    import 'highlight.js/styles/github-dark.css';
    import TaskRenderer from '../TaskRenderer.svelte';
    import type { ViewContext } from '../../scripts/types';

    export let note: StashNote;
    export let categoryId: string;
    export let noteIndex: number;

    const dispatch = createEventDispatcher();
    const modal = getContext<Context>('simple-modal');

    export let isOpen = false;
    let isEditing = false;
    
    // Key to force re-render of markdown when content changes
    let contentKey = 0;

    // Refresh note content after task toggle
    async function refreshNote() {
        console.log('NoteAccordion: Refreshing note at index', noteIndex);
        // Clear cache to force fresh fetch
        StashCache.clearNotes(categoryId);
        const notesResult = await StashApi.getNotes(categoryId);
        if (notesResult.isOk && notesResult.theResult) {
            // Find by index instead of ID since ID changes after update
            const updatedNote = notesResult.theResult[noteIndex];
            if (updatedNote) {
                console.log('NoteAccordion: Note refreshed from', note.id, 'to', updatedNote.id);
                note = updatedNote;
                contentKey++; // Force re-render
            }
        }
    }

    // Set up view context for TaskRenderer - make it reactive to note changes
    $: setContext<ViewContext>('view-context', {
        getNoteId: () => note.id,
        type: 'stash',
        categoryId: categoryId,
        onNoteUpdated: refreshNote
    });

    function toggleOpen() {
        if (!isEditing) {
            isOpen = !isOpen;
        }
    }

    function handleEdit() {
        isEditing = true;
        isOpen = true;
    }

    function handleEditSave() {
        isEditing = false;
        dispatch('updated');
    }

    function handleEditCancel() {
        isEditing = false;
    }

    async function handleDelete() {
        modal.open(
            ConfirmDialog,
            {
                message: `Are you sure you want to delete the note <b>${note.title || 'Untitled'}</b>?`,
                detail: 'This action cannot be undone.',
                oncancel: () => {},
                onOkay: async () => {
                    const result = await StashApi.deleteNote(categoryId, note.id);
                    if (result.isOk) {
                        dispatch('deleted');
                    } else {
                        console.error('Failed to delete note:', result.errorMessage);
                        // You might want to show an error message to the user
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

    function getPreviewText(content: string): string {
        // Remove markdown header if present
        let text = content.replace(/^#\s+.*\n/, '');
        // Get first line or first 80 characters
        const firstLine = text.split('\n')[0];
        return firstLine.length > 80 ? firstLine.substring(0, 80) + '...' : firstLine;
    }
</script>

<div class="note-accordion" class:open={isOpen}>
    <div class="note-header" on:click={toggleOpen}>
        <div class="header-left">
            <button class="toggle-btn" aria-label={isOpen ? 'Collapse' : 'Expand'}>
                <Fa icon={isOpen ? faChevronDown : faChevronRight} />
            </button>
            <div class="note-title">
                <strong>{note.title || 'Untitled'}</strong>
            </div>
        </div>
        <div class="action-buttons" on:click|stopPropagation>
            {#if !isEditing}
                <button class="btn-icon" on:click={handleEdit} aria-label="Edit note">
                    <Fa icon={faPen} />
                </button>
                <button class="btn-icon btn-danger" on:click={handleDelete} aria-label="Delete note">
                    <Fa icon={faTrash} />
                </button>
            {/if}
        </div>
    </div>

    {#if isOpen}
        <div class="note-content">
            {#if isEditing}
                <NoteEditor 
                    {categoryId}
                    noteId={note.id}
                    initialContent={note.content}
                    on:save={handleEditSave}
                    on:cancel={handleEditCancel}
                />
            {:else}
                <div> <!--class="markdown-content"> -->
                  {#key contentKey}
                    <SvelteMarkdown renderers={{ code: CodeMarkdown, listitem: TaskRenderer }} source={note.content} />
                  {/key}
                </div>
            {/if}
        </div>
    {/if}
</div>

<style>
    .note-accordion {
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        background-color: white;
        transition: box-shadow 0.2s;
    }

    .note-accordion:hover {
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .note-accordion.open {
        border-color: #4a90e2;
    }

    .note-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 3px 3px;
        cursor: pointer;
        user-select: none;
    }

    .header-left {
        display: flex;
        align-items: center;
        gap: 12px;
        flex: 1;
        min-width: 0;
    }

    .toggle-btn {
        background: none;
        border: none;
        padding: 4px;
        cursor: pointer;
        color: #666;
        display: flex;
        align-items: center;
        transition: color 0.2s;
    }

    .toggle-btn:hover {
        color: #333;
    }

    .note-title {
        flex: 1;
        min-width: 0;
    }

    .note-title strong {
        display: block;
        color: #333;
        margin-bottom: 2px;
    }

    .preview {
        display: block;
        color: #999;
        font-size: 0.9rem;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .action-buttons {
        display: flex;
        gap: 8px;
    }

    .btn-icon {
        background: none;
        border: none;
        padding: 8px;
        cursor: pointer;
        color: #666;
        transition: color 0.2s, background-color 0.2s;
        border-radius: 4px;
        display: flex;
        align-items: center;
    }

    .btn-icon:hover {
        background-color: #f0f0f0;
        color: #333;
    }

    .btn-danger:hover {
        color: #dc3545;
        background-color: #ffe6e6;
    }

    .note-content {
        padding: 16px;
        border-top: 1px solid #e0e0e0;
        background-color: #fafafa;
    }

    .markdown-content {
        line-height: 1.6;
        color: #333;
    }

    .markdown-content :global(h1) {
        font-size: 1.5rem;
        margin: 0 0 12px 0;
    }

    .markdown-content :global(h2) {
        font-size: 1.3rem;
        margin: 16px 0 10px 0;
    }

    .markdown-content :global(h3) {
        font-size: 1.1rem;
        margin: 14px 0 8px 0;
    }

    .markdown-content :global(p) {
        margin: 0 0 12px 0;
    }

    .markdown-content :global(ul), 
    .markdown-content :global(ol) {
        margin: 0 0 12px 0;
        padding-left: 24px;
    }

    .markdown-content :global(li) {
        margin-bottom: 4px;
    }

    .markdown-content :global(code) {
        background-color: #f4f4f4;
        padding: 2px 6px;
        border-radius: 3px;
        font-family: 'Courier New', monospace;
        font-size: 0.9em;
    }

    .markdown-content :global(pre) {
        background-color: #f4f4f4;
        padding: 12px;
        border-radius: 4px;
        overflow-x: auto;
        margin: 0 0 12px 0;
    }

    .markdown-content :global(pre code) {
        background: none;
        padding: 0;
    }

    .markdown-content :global(blockquote) {
        border-left: 4px solid #ddd;
        padding-left: 16px;
        margin: 0 0 12px 0;
        color: #666;
    }

    @media (max-width: 600px) {
        .note-header {
            padding: 10px 12px;
        }

        .action-buttons {
            gap: 4px;
        }

        .btn-icon {
            padding: 6px;
        }
    }
</style>
