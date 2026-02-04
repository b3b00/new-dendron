<script lang="ts">

    import {repository, isFavoriteRepository, tree, setTree, loadedNotes, getLoadedNote, getCachedNode, cacheNode, noteId} from '../scripts/dendronStore.js';
    import { onMount, getContext } from 'svelte';    
    import { StashApi } from '../scripts/stashApi.js';
    import Accordion from "@bolduh/svelte-nested-accordion/src/Accordion.svelte";
    import NoteNodeWraper from "./NoteNodeWraper.svelte";
    import {Dendron, Node, Note, NoteFilter, Repository} from '../scripts/types'
    import {Tools} from '../scripts/tools.js';
    
            // ...rest of existing code...

    import NoteFilterTemplate from './TreeFilter.svelte';
    import CommandPalette from './CommandPalette.svelte';
    import { Wave } from 'svelte-loading-spinners';
    import Fa from 'svelte-fa';
    import { faHeart } from '@fortawesome/free-solid-svg-icons/index.js';    
    import ErrorDialog from './ErrorDialog.svelte';
    import { push } from 'svelte-spa-router';
    import type { Context } from 'svelte-simple-modal';
    import { DendronClient } from '../scripts/dendronClient.js';
    
    const modal = getContext<Context>('simple-modal');

    export const id:string = '';

    export let refresh:string = undefined;

    let currentRepository : Repository = undefined;
    
    let currentTree : Node = undefined;

    let loading:boolean = false;

    let commandPaletteVisible = false;

    const paletteSelector = Tools.getShortcutSelector(['Ctrl+Alt+K', 'Ctrl+Meta+K', 'Ctrl+K', 'Meta+K']);

    const toggleCommandPalette = () => {
        commandPaletteVisible = !commandPaletteVisible;
    }

    const shortcuts = Tools.setShortcuts(
        [
            { shortcuts: ['Ctrl+Alt+K','Ctrl+Meta+K','Ctrl+K','Meta+K'], callback: toggleCommandPalette},
            { shortcuts: ['Ctrl+Alt+E', 'Ctrl+Meta+E'], callback: () => {
                if (!$noteId) return;
                push(`/edit/${$noteId}/`);
            } }, 
            { shortcuts: ['Ctrl+Alt+V','Ctrl+Meta+V'], callback: () => {
                if (!$noteId) return;
                push(`/view/${$noteId}/`);
            } },
            { shortcuts: ['Ctrl+Alt+R', 'Ctrl+Meta+R'], callback: async () => { window.alert('reloading tree.... (to come)')} },
            { shortcuts: ['Ctrl+Alt+T'], callback: () => { push('/tree/${$repository.id}');}} ,
            { shortcuts: ['Ctrl+Alt+S', 'Ctrl+Meta+S'], callback: () => {
                console.log(`navigating to stahshes`);
                push('/stashes');            
            } }
        ]
    )    


    function handleNoteSelect(event: CustomEvent<Note>) {
        const note = event.detail;
        push(`/view/${note.header.name}`);
    }


    // Helper to check if all notes are loaded
    function allNotesLoaded(tree, loadedNotes) {
        if (!tree || !tree.children) return false;
        let stack = [tree];
        while (stack.length > 0) {
            let node = stack.pop();
            if (!loadedNotes[node.id]) return false;
            if (node.children && node.children.length > 0) {
                stack.push(...node.children);
            }
        }
        return true;
    }

    // Enhanced noteFilter: if not all notes loaded, search via backend
    let backendSearchResults = [];
    let lastBackendQuery = "";

    let abortController: AbortController | null = null;

    async function searchNotesBackend(query: string, searchInContent: boolean): Promise<Note[]> {
        console.log(`searchNotesBackend called with query=${query} searchInContent=${searchInContent} in ${currentRepository.id}`);
        if (!currentRepository?.id || !query || query.length < 3) return [];

        if (abortController) {
            abortController.abort();
        }
        abortController = new AbortController();

        const url = `/notes/${currentRepository.id}/search?pattern=${encodeURIComponent(query)}&searchInContent=${searchInContent}`;
        console.log(`[Tree] searchNotesBackend fetching from: ${url}`);
        
        try {
            const resp = await fetch(url, {
                signal: abortController.signal
            });
            if (resp.ok) {
                const results = await resp.json();
                console.log(`[Tree] searchNotesBackend success. Found ${results.length} matches.`);
                return results;
            } else {
                console.error(`[Tree] searchNotesBackend failed with status: ${resp.status}`);
            }
        } catch (e: any) { 
            if (e.name !== 'AbortError') {
                console.error('[Tree] searchNotesBackend fetch error:', e); 
            }
        }
        return [];
    }

    const noteFilter = (node:Node, filter:NoteFilter) : boolean => {
        let note = getLoadedNote(node.id);
        if (filter.filter !== undefined && filter.filter !== null && filter.filter.length > 0) {
            // If searching in note content and not all notes loaded, use backend
            if (filter.searchInNotes && !allNotesLoaded($tree, $loadedNotes)) {
                // If query too short, skip search (both backend and local)
                if (filter.filter.length < 3) {
                    return false;
                }

                const currentSearchState = `${filter.filter}:${filter.searchInNotes}`;
                if (currentSearchState !== lastBackendQuery) {
                    // Update currentSearchState synchronously to prevent redundant concurrent requests
                    const queryToFire = filter.filter;
                    const searchInContentToFire = filter.searchInNotes;
                    lastBackendQuery = currentSearchState;
                    
                    // Fire backend search and cache results
                    searchNotesBackend(queryToFire, searchInContentToFire).then(results => {
                        backendSearchResults = results;
                    });
                }
                // Skip local search recursion: Only show nodes that are in backendSearchResults
                // The backend search results include all matching nodes.
                return backendSearchResults.some(n => n.id === node.id);
            }
            // Client-side search (fallback or when all notes loaded)
            if(node.name.toLocaleLowerCase().includes(filter.filter.toLocaleLowerCase())) {
                return true;
            }
            if (filter.searchInNotes && note) {
                if(note.body.toLocaleLowerCase().includes(filter.filter.toLocaleLowerCase())) {
                    return true;
                }
            }
            return false;
        }
        return true;
    }
    const getDescendanceNames = (node: Node): string[] => {
        let names: string[] = [];
        if (!node) return names;
        if (node.name && node.name !== 'root') {
            names.push(node.name);
        }
        if (node.children && node.children.length > 0) {
            for (const child of node.children) {
                names.push(...getDescendanceNames(child));
            }
        }
        return names;
    }

    $: {
        console.log('reactive statement : refreshing tree',$tree);
        currentTree = $tree;
    }

    const setFavoriteRepository = async () => {
        DendronClient.setFavorite($repository.id);
        $isFavoriteRepository = !$isFavoriteRepository;
    }
    

    onMount(async () => {
        currentRepository = $repository;
        currentTree = $tree;
        // Try to load root node from cache first
        let cachedRoot = null;
        try {
            cachedRoot = getCachedNode('root');
        } catch {}
        if (cachedRoot) {
            $tree = cachedRoot;
            currentTree = $tree;
            loading = false;
            console.log('Loaded root node from cache');
        } else if (currentTree === null || currentTree === undefined || !currentTree.hasOwnProperty('name') || refresh) {
            console.log('loading root node from BackEnd');
            loading = true;
            // Fetch hierarchy and metadata (but skip full contents for speed)
            const dendron = await DendronClient.GetDendron(currentRepository.id, false);
            loading = false;
            console.log('Backend response is ', dendron);
            if (dendron.isOk) {
                $isFavoriteRepository = dendron.theResult.isFavoriteRepository;
                // Only set root node, children will be loaded lazily
                let rootNode = dendron.theResult.hierarchy;
                // Do not clear children; show first level immediately
                $tree = rootNode;
                currentTree = $tree;
                cacheNode(rootNode);
                $loadedNotes = [];
                // Optionally: load stashes after repository context is established
                const stashes = await StashApi.getCategoriesWithNotes(true);
                if (stashes.isOk) {
                    console.log(`Stashes loaded: ${stashes.theResult?.length || 0} categories`);
                } else {
                    console.warn('Failed to load stashes:', stashes.errorMessage);
                }
            } else {
                $isFavoriteRepository = false;
                $repository = undefined;
                setTree(undefined);
                modal.open(
                    ErrorDialog,
                    {
                        message: `An error occured: ${dendron.errorMessage} `
                    },
                    {
                        closeButton: true,
                        closeOnEsc: true,
                        closeOnOuterClick: true
                    });
            }
        } else {
            console.log('tree loaded from store', currentTree);
        }
    });

    const treeCommands = {
        'reload' : {
            action: async () => {
                if (!currentRepository?.id) return;
                loading = true;
                try {
                    // Fetch hierarchy and metadata from backend
                    const dendron = await DendronClient.GetDendron(currentRepository.id, false);
                    if (dendron.isOk) {
                        $isFavoriteRepository = dendron.theResult.isFavoriteRepository;
                        let rootNode = dendron.theResult.hierarchy;
                        $tree = rootNode;
                        currentTree = $tree;
                        cacheNode(rootNode);
                        $loadedNotes = [];
                    } else {
                        alert('Failed to reload tree: ' + (dendron.errorMessage || 'Unknown error'));
                    }
                } finally {
                    loading = false;
                }
            },
            description: 'Reload the current repository tree from backend'
        },
        'create': {
            action: (noteId) => {
                if (!noteId) return;
                const names = getDescendanceNames(currentTree);
                if (names.includes(noteId)) {
                    modal.open(
                        ErrorDialog,
                        {
                            message: `The note <b>${noteId}</b> already exists.`
                        },
                        {
                            closeButton: true,
                            closeOnEsc: true,
                            closeOnOuterClick: true
                        }
                    );
                    return;
                }
                push(`/new/${noteId}`);
            },
            allowFreeText: true,
            description: 'Create a new note',
            suggestions: async (arg) => {
                if (!currentTree) return [];
                const names = getDescendanceNames(currentTree);
                const filtered = arg 
                    ? names.filter(name => name.includes(arg))
                    : names;
                return filtered.map(name => ({ label: name, value: name }));
            },
            suggestionsDescription: 'Enter new note name'
        }
    };

 // Helper: get excerpt with highlight
    function getExcerpt2(body: string, pattern: string): string {
        const lines = body.split('\n');
        for (const line of lines) {
            if (line.toLowerCase().includes(pattern.toLowerCase())) {
                // Highlight
                const escapedPattern = pattern.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                const re = new RegExp(`(${escapedPattern})`, 'ig');
                let excerpt = line.replace(re, '<mark>$1</mark>');
                return excerpt.trim();
            }
        }
        return '';
    }

    // Wrap the search callback to inject excerpt into note descriptions
    async function searchNotesWithExcerpt(pattern: string, searchInContent: boolean) {
        const found = await searchNotesBackend(pattern, searchInContent);
        return found.map(note => {
            if (searchInContent && note.body) {
                const excerpt = getExcerpt2(note.body, pattern);
                if (excerpt) {
                    return {
                        ...note,
                        header: {
                            ...note.header,
                            description: excerpt || note.header.description
                        }
                    };
                }
            }
            return note;
        });
    }

</script>

<svelte:window on:keydown={shortcuts} />

<CommandPalette
    searchCallback={searchNotesWithExcerpt}
    commands={treeCommands}
    bind:visible={commandPaletteVisible}
    on:select={handleNoteSelect}
    on:close={() => commandPaletteVisible = false}
>
    <svelte:fragment slot="result" let:result>
        <div class="note-title">{result.header?.title || result.header?.id}</div>
        {#if result.header?.description}
            <div class="note-desc">{@html result.header.description}</div>
        {/if}
    </svelte:fragment>
</CommandPalette>

<div>
    {#if loading}
        <div class="spinner-item" title="Wave">
            <Wave  size="45" />
            <div class="spinner-title">Dendron is loading...</div>
        </div>
    {:else}
    <span on:click={setFavoriteRepository}
          on:keydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { setFavoriteRepository(); } }}
          tabindex="0"
          role="button"
          aria-pressed="{$isFavoriteRepository}"
          style="color:{$isFavoriteRepository ? 'tomato':'grey'};font-weight:bold">
        <Fa size="2x" icon="{faHeart}" color="{$isFavoriteRepository ? 'tomato':'grey'}"></Fa>
        {currentRepository?.name}
    </span>
    <br>
        <Accordion tab="25px" disposition="left" emptyTreeMessage="nothing to show..." root={currentTree} nodeTemplate={NoteNodeWraper} searchTemplate={NoteFilterTemplate} complexFilter={noteFilter} nodeClass="dendron">
            <style slot="style">
                .dendron {
                    border-bottom: thin solid black;
                    border-left: thin dotted black;
                    padding : 10px
                }

                .dendron:hover {
                    background-color:lightgrey
                }
            </style>
        </Accordion>
    {/if}
</div>