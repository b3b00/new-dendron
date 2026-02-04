<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faSearch, faTimes } from '@fortawesome/free-solid-svg-icons/index.js';

    export let value = '';

    const dispatch = createEventDispatcher();
    let timeoutId: number;

    function handleInput(event: Event) {
        const input = event.target as HTMLInputElement;
        
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => {
            value = input.value;
            dispatch('change', value);
        }, 300);
    }

    function clear() {
        value = '';
        dispatch('change', '');
    }
</script>

<div class="search-box">
    <div class="search-input-wrapper">
        <Fa icon={faSearch} class="search-icon" />
        <input 
            type="text"
            placeholder="Search in notes..."
            value={value}
            on:input={handleInput}
        />
        {#if value}
            <button class="clear-btn" on:click={clear} aria-label="Clear search">
                <Fa icon={faTimes} />
            </button>
        {/if}
    </div>
</div>

<style>
    .search-box {
        margin-bottom: 20px;
    }

    .search-input-wrapper {
        position: relative;
        display: flex;
        align-items: center;
    }

    :global(.search-icon) {
        position: absolute;
        left: 12px;
        color: #999;
        pointer-events: none;
    }

    input {
        width: 100%;
        padding: 10px 40px 10px 38px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 0.95rem;
        transition: border-color 0.2s, box-shadow 0.2s;
    }

    input:focus {
        outline: none;
        border-color: #4a90e2;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    input::placeholder {
        color: #999;
    }

    .clear-btn {
        position: absolute;
        right: 8px;
        background: none;
        border: none;
        padding: 4px 8px;
        cursor: pointer;
        color: #999;
        transition: color 0.2s;
        display: flex;
        align-items: center;
    }

    .clear-btn:hover {
        color: #666;
    }
</style>
