<script>
    import { getContext } from 'svelte';
    export let message;
    export let hasForm = false;
    export let onCancel = () => {};
    export let onOkay = () => {};
    export let label = '';
    export let descLabel = '';
    export let hasDescription = false;
    export let initialDescription = '';

    const { close } = getContext('simple-modal');

    export let parent;
    let description = initialDescription;
    let onChange = () => {};

    function _onCancel() {
        onCancel();
        close();
    }

    function _onOkay() {
        if (hasDescription) {
            onOkay(parent, description);
        } else {
            onOkay(parent);
        }
        close();
    }

    $: onChange(parent)
</script>

<style>
    h2 {
        font-size: 2rem;
        text-align: center;
    }

    .form-group {
        display: flex;
        flex-direction: column;
        gap: 16px;
        margin: 20px 0;
    }

    .input-group {
        display: flex;
        flex-direction: column;
        gap: 6px;
    }

    label {
        font-weight: 600;
        font-size: 0.9rem;
        color: #333;
    }

    input, textarea {
        width: 100%;
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 0.95rem;
        font-family: inherit;
    }

    input:focus, textarea:focus {
        outline: none;
        border-color: #4a90e2;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    textarea {
        resize: vertical;
        min-height: 80px;
    }

    .buttons {
        display: flex;
        justify-content: space-between;
    }
</style>

<h2>{message}</h2>

{#if hasForm}
    <div class="form-group">
        {#if label}
            <div class="input-group">
                <label for="main-input">{label}</label>
                <input
                    id="main-input"
                    type="text"
                    bind:value={parent}
                    on:keydown={e => e.key === "Enter" && !hasDescription && _onOkay()} />
            </div>
        {:else}
            <input
                type="text"
                bind:value={parent}
                on:keydown={e => e.key === "Enter" && !hasDescription && _onOkay()} />
        {/if}
        
        {#if hasDescription}
            <div class="input-group">
                <label for="desc-input">{descLabel || 'Description'}</label>
                <textarea
                    id="desc-input"
                    bind:value={description}
                    placeholder="Enter description..."
                ></textarea>
            </div>
        {/if}
    </div>
{/if}

<div class="buttons">
    <button on:click={_onCancel}>
        Cancel
    </button>
    <button on:click={_onOkay}>
        Okay
    </button>
</div>