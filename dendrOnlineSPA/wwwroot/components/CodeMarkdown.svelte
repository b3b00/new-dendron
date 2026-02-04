<script lang="ts">
	import hljs from 'highlight.js';
	import {onMount} from 'svelte';
	import mermaid from 'mermaid';

	export let text = '';
	export let lang = 'text';

	let content = "";
	let isMermaid = false;

	let container;
	let codeElement;
	let showToast = false;
	let toastTimeout;

	onMount(async () => {
		if (!lang ||lang.startsWith('plaintext')) {
			lang = "text";
		}
		if (lang == 'mermaid') {
			isMermaid = true;
			let c = await mermaid.render('todo',text);
			container.innerHTML=c.svg;
			content = c.svg
		}
		else {
			isMermaid = false;
			content = hljs.highlight(text, { language: lang }).value;
		}
	})
	async function copyCode() {
		if (codeElement) {
			// Get only the text content of the code block
			const text = codeElement.innerText || codeElement.textContent;
			try {
				await navigator.clipboard.writeText(text);
			} catch (e) {
				const textarea = document.createElement('textarea');
				textarea.value = text;
				document.body.appendChild(textarea);
				textarea.select();
				document.execCommand('copy');
				document.body.removeChild(textarea);
			}
			showToast = true;
			clearTimeout(toastTimeout);
			toastTimeout = setTimeout(() => { showToast = false; }, 1200);
		}
	}
	

	
</script>


{#if isMermaid} 
<div>
	<span bind:this={container}>		
</div>
{:else}
<div class="code-block-container">
	<button class="copy-btn" title="Copy code" on:click={copyCode} aria-label="Copy code">
		<svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg">
			<rect x="5" y="7" width="9" height="9" rx="2" stroke="#fff" stroke-width="1.5" fill="#444"/>
			<rect x="7" y="4" width="9" height="9" rx="2" stroke="#fff" stroke-width="1.5" fill="#444"/>
		</svg>
	</button>
	<pre class={`language-` + lang}>
		<code class="hljs" bind:this={codeElement}>{@html content}</code>
	</pre>
	{#if showToast}
		<div class="copy-toast">Copied!</div>
	{/if}
</div>
{/if}

<style>
		.code-block-container {
			position: relative;
			margin: 0;
			padding: 0;
			width: 100%;
			display: block;
		}
		.copy-btn {
			position: absolute;
			top: 1.1em;
			right: 1.1em;
			background: #222 !important;
			color: #fff !important;
			border: 1px solid #888 !important;
			border-radius: 4px;
			z-index: 10;
			padding: 0.3em 0.4em;
			display: flex;
			align-items: center;
			justify-content: center;
		}
		pre {
			margin: 0;
			width: 100%;
			display: block;
			padding: 1.5em 1em;
			border-radius: 10px;
			background: #11131a;
			color: #eaeaea;
			font-family: 'Fira Mono', 'Consolas', 'Menlo', monospace;
			font-size: 1em;
			overflow-x: auto;
			box-shadow: 0 2px 12px rgba(0,0,0,0.10);
			position: relative;
		}
.copy-btn svg {
  fill: #fff !important;
  stroke: #fff !important;
}
		.copy-toast {
			position: absolute;
			top: 0.7em;
			right: 3.5em;
			background: #222;
			color: #fff;
			padding: 0.5em 1.2em;
			border-radius: 6px;
			box-shadow: 0 2px 12px rgba(0,0,0,0.18);
			font-size: 1em;
			opacity: 0.93;
			z-index: 9999;
			pointer-events: none;
			animation: fadeOut 1.2s forwards;
		}
		@keyframes fadeOut {
			0% { opacity: 0.93; }
			80% { opacity: 0.93; }
			100% { opacity: 0; }
		}
</style>