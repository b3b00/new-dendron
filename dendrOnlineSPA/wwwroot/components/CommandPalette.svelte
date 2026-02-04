<script lang="ts">
    import { createEventDispatcher, onMount } from 'svelte';
    import type { PaletteCommandMap } from '../scripts/types';
    import Fa from 'svelte-fa';
    import { faSearch, faTimes, faToggleOn, faToggleOff, faTerminal } from '@fortawesome/free-solid-svg-icons/index.js';

    const dispatch = createEventDispatcher();

    export let visible = false;
    export let searchCallback: (query: string, searchInContent: boolean) => Promise<any[]>;
    export let commands: PaletteCommandMap = {};
    
    let query = '';
    let results: any[] = [];
    let commandResults: { name: string, arg?: string, description: string, isSuggestion?: boolean, suggestionValue?: string }[] = [];
    let selectedIndex = 0;
    let loading = false;
    let searchInContent = false;
    let inputElement: HTMLInputElement;

    $: if (visible && inputElement) {
        setTimeout(() => inputElement.focus(), 50);
    }

    // Reset state when visibility changes
    $: if (!visible) {
        query = '';
        results = [];
        commandResults = [];
        selectedIndex = 0;
    }

    // Debounced search / Command handling
    let searchTimeout: any;
    $: {
        if (query.startsWith('>')) {
            clearTimeout(searchTimeout);
            const fullCommand = query.substring(1);
            const [cmdName, ...argParts] = fullCommand.split(' ');
            const arg = argParts.join(' ');
            
            const matchedCommands = Object.keys(commands)
                .filter(name => name.toLowerCase().startsWith(cmdName.toLowerCase()));

            // If we have an exact match and it has suggestions, show suggestions instead of command list
            const exactMatch = matchedCommands.find(name => name.toLowerCase() === cmdName.toLowerCase());
            const command = exactMatch ? commands[exactMatch] : null;

            if (command && typeof command !== 'function' && command.suggestions) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(async () => {
                    const suggestions = await command.suggestions!(arg);
                    commandResults = suggestions.map(s => ({
                        name: s.label,
                        arg: '',
                        description: s.description || command.suggestionsDescription || 'Select an option',
                        isSuggestion: true,
                        suggestionValue: s.value,
                        cmdName: exactMatch
                    }));
                    if (selectedIndex >= commandResults.length) {
                        selectedIndex = commandResults.length > 0 ? 0 : -1;
                    }
                }, 100);
            } else {
                commandResults = matchedCommands
                    .map(name => {
                        const cmd = commands[name];
                        const description = typeof cmd === 'function' ? 'Execute command' : (cmd.description || 'Execute command');
                        return { name: `>${name}`, arg, description };
                    });
                
                // Ensure selectedIndex is within bounds
                if (selectedIndex >= commandResults.length) {
                    selectedIndex = commandResults.length > 0 ? 0 : -1;
                } else if (selectedIndex === -1 && commandResults.length > 0) {
                    selectedIndex = 0;
                }
            }
            results = [];
        } else if (query.trim().length > 0) {
            commandResults = [];
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                searchNotesBackend(query);
            }, 300);
        } else {
            results = [];
            commandResults = [];
            if (selectedIndex > 0) selectedIndex = 0;
        }
    }




    async function searchNotesBackend(pattern: string) {
        if (!searchCallback) return;
        loading = true;
        try {
            const found = await searchCallback(pattern, searchInContent);
            results = found;
            selectedIndex = results.length > 0 ? 0 : -1;
        } catch (e) {
            console.error('[CommandPalette] Search failed:', e);
        } finally {
            loading = false;
        }
    }

    function toggleSearchInContent() {
        searchInContent = !searchInContent;
        if (query.trim().length > 0 && !query.startsWith('>')) {
            searchNotesBackend(query);
        }
    }



    /**
     * Returns the greatest common prefix among all strings.
     * Example: ["flower","flow","flight"] -> "fl"
     * Time: O(n log n + m) where n = number of strings, m = prefix length of first/last after sort
     * Space: O(1) extra (ignoring sort's internal memory)
     */
    export function greatestCommonPrefix(arr: string[]): string {
    if (!arr || arr.length === 0) return "";
    if (arr.length === 1) return arr[0] ?? "";
    console.log(`greatestCommonPrefix(${arr})`);    
    // Sort lexicographically
    const sorted = [...arr].sort();
    console.log(`  sorted: ${sorted}`);
    const first = sorted[0];
    const last = sorted[sorted.length - 1];
    console.log(`  first: >${first}<, last: >${last}<`);

    let i = 0;
    const limit = Math.min(first.length, last.length);
    console.log(`  limit: ${limit}`);
    
    let prefix = '';
    while (i < limit && first[i] == last[i]) {
        prefix += first[i];
        console.log(`${first[i]} == ${last[i]} => ${prefix}`);
        i++;
    } 
    console.log(`  common prefix length: ${i} => >${first.slice(0, i)}<`);    
    return first.slice(0, i);
    }


    function getGreatestCommonPrefix(strings: string[]): string {
        console.log(`getGreatestCommonPrefix(${strings})`);
        if (strings.length === 0) return '';
        let prefix = strings[0];
        for (let i = 1; i < strings.length; i++) {
            console.log(`computing prefix #${i}(${strings[i]}): current prefix >${prefix}<`);
            while (strings[i].indexOf(prefix) !== 0) {

                prefix = prefix.substring(0, prefix.length - 1);
                if (prefix === '') {
                    console.log(`failing to find prefix >${prefix}< for >${strings[i]}< => empty prefix`);
                    return '';
                }
            }
        }
        console.log(`getGreatestCommonPrefix(${strings}) = ${prefix}`);
        return prefix;
    }

    function handleKeydown(e: KeyboardEvent) {
        console.log(`handleKeydown(${e.key})`);
        if (e.key === 'Escape') {
            close();
        } else if (e.key === 'ArrowDown') {
            e.preventDefault();
            const totalWidth = results.length + commandResults.length + (query === '' ? 1 : 0);
            if (totalWidth > 0) {
                selectedIndex = (selectedIndex + 1) % totalWidth;
            }
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            const totalWidth = results.length + commandResults.length + (query === '' ? 1 : 0);
            if (totalWidth > 0) {
                selectedIndex = (selectedIndex - 1 + totalWidth) % totalWidth;
            }
        } else if (e.key === 'Enter' && selectedIndex >= 0) {
            
            if (commandResults.length > 0 && commandResults[selectedIndex]) {
                const cmd = commandResults[selectedIndex];
                if (cmd.isSuggestion) {
                    // @ts-ignore
                    executeSuggestion(cmd);
                } else {
                    executeCommand(cmd);
                }
            } else if (results.length > 0 && results[selectedIndex]) {
                selectItem(results[selectedIndex]);
            } else if (query === '' && selectedIndex === 0) {
                toggleSearchInContent();
            }                     
        } 
        else if (e.key === 'Enter' && query !== undefined && query !== null && query != '' && query.startsWith('>')) { // TODO : check is free text allowed
            console.log(`free text input => query = >${query}<`,commandResults,selectedIndex);
            // get the command name
            let cmdName = query.substring(1);
            const cmdIndex = query.indexOf(' ');
            let arg = '';
            if (cmdIndex !== -1) {
                cmdName = cmdName.substring(0, cmdIndex-1);            
                arg = query.substring(cmdIndex + 1);
            }            
            if (cmdName) {
                const cmd = commands[cmdName];
                // Type guard for PaletteCommand
                function isPaletteCommand(obj: any): obj is { allowFreeText: boolean } {
                    return obj && typeof obj === 'object' && 'allowFreeText' in obj && typeof obj.allowFreeText === 'boolean';
                }

                if (isPaletteCommand(cmd) && cmd.allowFreeText) {
                    console.log(`Executing command ${cmdName} with arg ${arg}`);
                    if (typeof cmd === 'function') {
                        cmd(arg);
                        close();
                    } else {                        
                        cmd.action(arg);
                        close();
                    }
                }
            }

            
        }   
        else if (e.key === 'Tab') {
            if (query.startsWith('>') && commandResults.length > 0 && commandResults[0].isSuggestion) {
                e.preventDefault();
                const labels = commandResults.map(c => c.name);
                const gcp = greatestCommonPrefix(labels);
                console.log(`Tab pressed: GCP among ${labels.length} labels is >${gcp}<`);
                if (gcp) {
                    const firstSpaceIndex = query.indexOf(' ');
                    if (firstSpaceIndex !== -1) {
                        const commandPart = query.substring(0, firstSpaceIndex + 1);
                        query = commandPart + gcp;
                    }
                }
            }
        }
    }

    function executeCommand(cmd: { name: string, arg?: string }) {
        const cmdName = cmd.name.substring(1);
        const command = commands[cmdName];
        if (command) {
            if (typeof command === 'function') {
                command(cmd.arg);
                close();
            } else {
                if (!command.suggestions) {
                    command.action(cmd.arg);
                    close();
                } else {
                    // If it has suggestions, don't close, let the user pick one
                    query = `>${cmdName} `;
                }
            }
        }
    }

    function executeSuggestion(suggestion: { cmdName: string, suggestionValue: string }) {
        const command = commands[suggestion.cmdName];
        if (command && typeof command !== 'function') {
            command.action(suggestion.suggestionValue);
        }
        close();
    }

    function selectItem(result: any) {
        dispatch('select', result);
        close();
    }

    function close() {
        visible = false;
        query = '';
        results = [];
        commandResults = [];
        dispatch('close');
    }

</script>

{#if visible}
    <div 
        class="modal-backdrop" 
        on:click={close} 
        on:keydown={(e) => {
            if (e.key === 'Escape' || e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                close();
            }
        }} 
        role="button"
        tabindex="0"
        aria-label="Close command palette"
    >
        <!-- svelte-ignore a11y-no-noninteractive-element-interactions -->
        <div 
            class="command-palette" 
            on:click|stopPropagation 
            on:keydown|stopPropagation
            role="document" 
            aria-label="Command Palette"
        >
            <div class="search-header">
                <span class="search-icon-wrapper">
                    <Fa icon={faSearch} />
                </span>
                <input
                    bind:this={inputElement}
                    bind:value={query}
                    on:keydown={handleKeydown}
                    placeholder="Search notes or type '>' for commands..."
                    type="text"
                />
                <button class="close-btn" on:click={close} aria-label="Close">
                    <Fa icon={faTimes} />
                </button>
            </div>

            <div class="results-container">
                {#if loading}
                    <div class="loading" role="status">Searching...</div>
                {:else if commandResults.length > 0}
                    <ul role="listbox">
                        {#each commandResults as cmd, i}
                            <li
                                class:selected={i === selectedIndex}
                                on:click={() => executeCommand(cmd)}
                                on:mouseenter={() => selectedIndex = i}
                                on:keydown={(e) => e.key === 'Enter' && executeCommand(cmd)}
                                role="option"
                                aria-selected={i === selectedIndex}
                                tabindex="0"
                            >
                                <div class="note-title">
                                    <Fa icon={faTerminal} style="margin-right: 8px; font-size: 0.8em;" />
                                    {cmd.name} {cmd.arg || ''}
                                </div>
                                <div class="note-desc">{@html cmd.description}</div>
                            </li>
                        {/each}
                    </ul>
                {:else if results.length > 0}
                    <ul role="listbox">
                        {#each results as result, i}
                            <li
                                class:selected={i === selectedIndex}
                                on:click={() => selectItem(result)}
                                on:mouseenter={() => selectedIndex = i}
                                on:keydown={(e) => e.key === 'Enter' && selectItem(result)}
                                role="option"
                                aria-selected={i === selectedIndex}
                                tabindex="0"
                            >
                                <slot name="result" {result} />
                            </li>
                        {/each}
                    </ul>
                {:else if query === ''}
                    <ul role="listbox">
                        <li
                            class:selected={selectedIndex === 0}
                            on:click={() => { toggleSearchInContent(); }}
                            on:mouseenter={() => selectedIndex = 0}
                            on:keydown={(e) => e.key === 'Enter' && (searchInContent = !searchInContent)}
                            role="option"
                            aria-selected={selectedIndex === 0}
                            tabindex="0"
                        >
                            <div class="note-title">Toggle Search in Note Content</div>
                            <div class="note-desc">Currently: {searchInContent ? 'ON' : 'OFF'}</div>
                        </li>
                    </ul>
                {:else if query.length >= 3}
                    <div class="no-results" role="status">No results found for "{query}"</div>
                {/if}
            </div>

            <div class="footer">
                <label class="toggle-container">
                    <span>Search in content</span>
                    <button 
                        class="toggle-btn" 
                        on:click={toggleSearchInContent}
                        aria-pressed={searchInContent}
                    >
                        <Fa icon={searchInContent ? faToggleOn : faToggleOff} />
                    </button>
                </label>
                <div class="shortcuts">
                    <span><kbd>↑↓</kbd> to navigate</span>
                    <span><kbd>Enter</kbd> to select</span>
                    <span><kbd>Esc</kbd> to close</span>
                </div>
            </div>
        </div>
    </div>
{/if}

<style>
    .modal-backdrop {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: flex-start;
        padding-top: 100px;
        z-index: 1000;
        backdrop-filter: blur(2px);
    }

    .command-palette {
        width: 600px;
        background: #1e1e1e;
        color: #d4d4d4;
        border-radius: 8px;
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
        overflow: hidden;
        border: 1px solid #333;
    }

    .search-header {
        display: flex;
        align-items: center;
        padding: 15px;
        border-bottom: 1px solid #333;
        gap: 10px;
    }

    .search-icon-wrapper {
        color: #888;
        display: flex;
        align-items: center;
    }

    input {
        flex: 1;
        background: transparent;
        border: none;
        color: white;
        font-size: 1.1rem;
        outline: none;
    }

    .close-btn {
        background: transparent;
        border: none;
        color: #888;
        cursor: pointer;
    }

    .close-btn:hover {
        color: white;
    }

    .results-container {
        max-height: 400px;
        overflow-y: auto;
    }

    ul {
        list-style: none;
        padding: 0;
        margin: 0;
    }

    li {
        padding: 12px 15px;
        cursor: pointer;
        border-left: 3px solid transparent;
    }

    li.selected {
        background: #2d2d2d;
        border-left-color: #007acc;
    }

    .note-title {
        font-weight: bold;
        color: #569cd6;
    }

    .note-desc {
        font-size: 0.9rem;
        color: #888;
        margin-top: 4px;
    }

    .loading, .no-results {
        padding: 30px;
        text-align: center;
        color: #888;
    }

    .footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 10px 15px;
        background: #252526;
        border-top: 1px solid #333;
        font-size: 0.85rem;
        color: #888;
    }

    .toggle-container {
        display: flex;
        align-items: center;
        gap: 8px;
        cursor: pointer;
    }

    .toggle-btn {
        background: transparent;
        border: none;
        color: #007acc;
        font-size: 1.2rem;
        cursor: pointer;
        padding: 0;
    }

    .shortcuts {
        display: flex;
        gap: 15px;
    }

    kbd {
        background: #333;
        padding: 2px 4px;
        border-radius: 3px;
        color: #ccc;
        font-family: inherit;
    }
</style>
