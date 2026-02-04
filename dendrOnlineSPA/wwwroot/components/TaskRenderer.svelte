<script lang="ts">

import { onMount, getContext, setContext } from 'svelte';
import {TaskToggler} from '../scripts/TaskToggler';
import type { ViewContext } from '../scripts/types';
import { getDraftNote, updateNote, repository, getLoadedNote, unDraft, setTree } from '../scripts/dendronStore';
import { DendronClient } from '../scripts/dendronClient';
import { StashApi } from '../scripts/stashApi';
import ErrorDialog from './ErrorDialog.svelte';
import { Context } from 'svelte-simple-modal';



export let text: string;

export let task: boolean;

export let checked: boolean;

let id: string;

const modal = getContext<Context>('simple-modal');

onMount(async () => {
    id = await TaskToggler.hashItem(text);    
    if (task) {
    console.log(`RENDERER.onMount() :: todo item :>${text}< with id:>${id}< - chekced:>${checked}<`);
    }
    else {
        console.log(`RENDERER.onMount() :: item :>${text}< with id :>${id}<`);
    }
})


const context = getContext<ViewContext>('view-context');

let toggle = async () => {
    checked = !checked; 
    let noteId = context.getNoteId();
    
    if (context.type === 'stash' && context.categoryId) {
        // Handle stash note toggling
        console.log('TaskRenderer: Toggling stash note checkbox', { text, id, noteId });
        const notesResult = await StashApi.getNotes(context.categoryId);
        if (!notesResult.isOk || !notesResult.theResult) {
            console.error('TaskRenderer: Failed to get notes', notesResult.errorMessage);
            modal.open(ErrorDialog, { message: notesResult.errorMessage });
            return;
        }
        
        console.log('TaskRenderer: Got notes, looking for noteId:', noteId, 'in', notesResult.theResult.length, 'notes');
        console.log('TaskRenderer: Available note IDs:', notesResult.theResult.map(n => n.id));
        const note = notesResult.theResult.find(n => n.id === noteId);
        if (note) {
            console.log('TaskRenderer: Found note, toggling content');
            let content = await TaskToggler.Toggle(text, id, note.content);
            console.log('TaskRenderer: Sending update to server');
            const result = await StashApi.updateNote(context.categoryId, noteId, content);
            if (!result.isOk) {
                console.error('TaskRenderer: Update failed', result.errorMessage);
                modal.open(ErrorDialog, { message: result.errorMessage });
            } else {
                console.log('TaskRenderer: Update successful, refreshing note');
                if (context.onNoteUpdated) {
                    // Notify parent component to refresh note content
                    await context.onNoteUpdated();
                }
            }
        } else {
            console.error('TaskRenderer: Note not found! noteId:', noteId, 'available:', notesResult.theResult.map(n => n.id));
        }
    } else {
        // Handle dendron note toggling (original behavior)
        var note  = getLoadedNote(noteId);
        if (note === undefined) {
            note = getDraftNote(noteId);
        }
        if (note) {
            let content = await TaskToggler.Toggle(text, id, note.body);
            note.body = note.body = content;
            updateNote(noteId, note);
            const newTree = await DendronClient.SaveNote($repository.id, note);
            if (newTree.isOk) {
                note.sha = newTree.theResult.sha;
                setTree(newTree.theResult.hierarchy);
                unDraft(noteId);
            }
            else {
                modal.open(ErrorDialog, { message: newTree.errorMessage });
            }
        }
    }
}




</script>

{#if task}
    <li>
        <span><input type="checkbox" on:change={toggle} checked={checked} style="padding-right: 15px; display:inline"/><span><slot></slot></span></span>
    </li>
{:else}
    <li><slot></slot></li>
{/if}
