<script>
    import { getContext } from 'svelte';
    import { onMount } from 'svelte';
    
    export let message;
    export let parent = '';
    export let items = []; // List of all tree notes
    
    export let onCancel = () => {};
    export let onOkay = (value) => {};

    const { close } = getContext('simple-modal');

    let search = '';
    let displayedItems = [];
    let showDropdown = false;
    let selectedIndex = -1;
    let isCycling = false;
    let cycleItems = [];
    let inputElement;

    $: {
        if (search && !isCycling) {
            displayedItems = items.filter(x => x.toLowerCase().includes(search.toLowerCase())).slice(0, 50);
            showDropdown = displayedItems.length > 0;
            if (selectedIndex >= displayedItems.length) selectedIndex = -1;
        } else if (!search) {
            displayedItems = [];
            showDropdown = false;
            isCycling = false;
        }
    }

    function _onCancel() {
        close();
        onCancel();
    }

    function _onOkay() {
        if (search.trim() !== '') {
            const val = search.trim();
            close();
            onOkay(val);
        }
    }

    function onKeyDown(e) {
        if (!showDropdown) {
            if (e.key === 'Enter') {
                e.preventDefault();
                _onOkay();
            }
            return;
        }

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            selectedIndex = (selectedIndex + 1) % displayedItems.length;
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            selectedIndex = (selectedIndex - 1 + displayedItems.length) % displayedItems.length;
        } else if (e.key === 'Tab') {
            e.preventDefault();
            if (displayedItems.length > 0) {
                if (!isCycling) {
                    isCycling = true;
                    cycleItems = [...displayedItems];
                    selectedIndex = 0;
                } else {
                    if (e.shiftKey) {
                        selectedIndex = (selectedIndex - 1 + cycleItems.length) % cycleItems.length;
                    } else {
                        selectedIndex = (selectedIndex + 1) % cycleItems.length;
                    }
                }
                search = cycleItems[selectedIndex];
            }
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (selectedIndex >= 0 && selectedIndex < displayedItems.length) {
                search = displayedItems[selectedIndex];
                showDropdown = false;
                // Just complete text field, allow user to continue editing OR press Enter again
                selectedIndex = -1;
                if (inputElement) inputElement.focus();
            } else {
                _onOkay();
            }
        } else if (e.key === 'Escape') {
            showDropdown = false;
            isCycling = false;
        }
    }

    function onInput() {
        isCycling = false;
    }
    
    function selectItem(item) {
        search = item;
        showDropdown = false;
        selectedIndex = -1;
        if (inputElement) inputElement.focus();
    }
    
    onMount(() => {
        //search = parent;
        if (inputElement) {
            setTimeout(() => {
                if (inputElement) inputElement.focus();
            }, 100);
        }
    });

</script>

<style>
    h2 {
        font-size: 1.5rem;
        text-align: center;
        margin-bottom: 20px;
    }

    .dialog-content {
        min-height: 400px;
        display: flex;
        flex-direction: column;
    }

    .autocomplete-container {
        position: relative;
        width: 100%;
        margin-bottom: 20px;
    }

    input {
        width: 100%;
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 0.95rem;
        font-family: inherit;
    }

    input:focus {
        outline: none;
        border-color: #4a90e2;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    .dropdown {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        max-height: 300px;
        overflow-y: auto;
        background: white;
        border: 1px solid #ddd;
        border-radius: 4px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        z-index: 1000;
        margin-top: 4px;
        padding: 0;
        list-style: none;
    }

    .dropdown li {
        padding: 10px;
        cursor: pointer;
        color: #333;
        display: flex;
        flex-direction: column;
    }

    .note-id {
        font-size: 1rem;
    }

    .note-title {
        font-size: 0.85rem;
        font-style: italic;
        text-align: right;
        color: #666;
        margin-top: 2px;
    }

    .dropdown li:hover, .dropdown li.selected {
        background: #f0f0f0;
        color: #000;
    }

    .buttons {
        display: flex;
        justify-content: space-between;
        margin-top: 20px;
    }

    button {
        padding: 8px 16px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        background: #f0f0f0;
        color: #333;
    }
    
    button.primary {
        background: #4a90e2;
        color: white;
    }
    
    button:hover {
        opacity: 0.9;
    }
</style>

<div class="dialog-content">
    <h2>{message}</h2>

    <div class="autocomplete-container">
        <input
            bind:this={inputElement}
            type="text"
            bind:value={search}
            on:input={onInput}
            on:keydown={onKeyDown}
            placeholder="Enter note name..."
            autocomplete="off"
        />
        {#if showDropdown}
            <ul class="dropdown">
                {#each displayedItems as item, i}
                    <li class:selected={i === selectedIndex} 
                        on:click={() => selectItem(item)}
                        on:keydown={(e) => e.key === 'Enter' && selectItem(item)}
                        role="option"
                        aria-selected={i === selectedIndex}
                        tabindex="0">
                        <span class="note-id">{item}</span>
                        <span class="note-title">{item.substring(item.lastIndexOf('.') + 1)}</span>
                    </li>
                {/each}
            </ul>
        {/if}
    </div>

    <div class="buttons">
        <button on:click={_onCancel}>Cancel</button>
        <button class="primary" on:click={_onOkay}>Okay</button>
    </div>
</div>
