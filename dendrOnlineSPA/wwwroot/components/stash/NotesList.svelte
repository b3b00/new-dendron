<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import type { StashNote } from '../../scripts/stashApi';
    import NoteAccordion from './NoteAccordion.svelte';
    import NoteEditor from './NoteEditor.svelte';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faPlus } from '@fortawesome/free-solid-svg-icons/index.js';

    export let notes: StashNote[] = [];
    export let searchTerm = '';
    export let categoryId: string;
    export let expandedNoteId: string | null = null;

    const dispatch = createEventDispatcher();

    export let showingAddNote = false;
    let filteredNotes: StashNote[] = [];

    $: {
        if (searchTerm) {
            const term = searchTerm.toLowerCase();
            filteredNotes = notes.filter(note => {
                const titleMatch = note.title?.toLowerCase().includes(term);
                const contentMatch = note.content.toLowerCase().includes(term);
                return titleMatch || contentMatch;
            });
        } else {
            filteredNotes = notes;
        }
    }

    function handleAddClick() {
        showingAddNote = true;
    }

    function handleAddSave(event: CustomEvent<string>) {
        dispatch('added');
        showingAddNote = false;
    }

    function handleAddCancel() {
        showingAddNote = false;
    }
</script>

<div class="notes-list">
    <div class="notes-header">
        <h2>Notes ({filteredNotes.length})</h2>
        <button class="btn-add" on:click={handleAddClick}>
            <Fa icon={faPlus} />
            <span>Add Note</span>
        </button>
    </div>

    {#if showingAddNote}
        <div class="add-note-editor">
            <h3>New Note</h3>
            <NoteEditor 
                {categoryId}
                initialContent="# "
                on:save={handleAddSave}
                on:cancel={handleAddCancel}
            />
        </div>
    {/if}

    {#if filteredNotes.length === 0}
        <div class="empty-notes">
            {#if searchTerm}
                <p>No notes found matching "{searchTerm}"</p>
            {:else}
                <p>No notes yet. Add your first note!</p>
            {/if}
        </div>
    {:else}
        <div class="accordion-list">
            {#each filteredNotes as note, index (note.id)}
                <NoteAccordion 
                    {note}
                    noteIndex={index}
                    {categoryId}
                    isOpen={note.id === expandedNoteId}
                    on:updated={() => dispatch('updated')}
                    on:deleted={() => dispatch('deleted')}
                />
            {/each}
        </div>
    {/if}
</div>

<style>
    .notes-list {
        margin-top: 20px;
    }

    .notes-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 16px;
    }

    .notes-header h2 {
        margin: 0;
        font-size: 1.5rem;
        color: #333;
    }

    .btn-add {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 16px;
        background-color: #28a745;
        color: white;
        border: none;
        border-radius: 4px;
        font-size: 0.95rem;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-add:hover {
        background-color: #218838;
    }

    .add-note-editor {
        margin-bottom: 20px;
        padding: 16px;
        background-color: #f8f9fa;
        border-radius: 4px;
        border: 2px dashed #ddd;
    }

    .add-note-editor h3 {
        margin: 0 0 12px 0;
        font-size: 1.1rem;
        color: #333;
    }

    .empty-notes {
        text-align: center;
        padding: 40px 20px;
        color: #999;
        font-size: 1.05rem;
    }

    .accordion-list {
        display: flex;
        flex-direction: column;
        gap: 4px;
    }

    @media (max-width: 600px) {
        .notes-header {
            flex-direction: column;
            align-items: stretch;
            gap: 12px;
        }

        .btn-add {
            justify-content: center;
        }
    }
</style>
