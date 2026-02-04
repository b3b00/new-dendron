<script lang="ts">
    // svelte
    import {getContext} from 'svelte';
    
    // client and store
    import {draftNotes, loadedNotes, noteId, repository, setTree, tree} from "./scripts/dendronStore";
    import { DendronClient } from './scripts/dendronClient';
    import { StashCache } from './scripts/stashCache';
    
    
 
    // components
    import Fa from 'svelte-fa/src/fa.svelte';
    import { faList, faPen, faEye, faFolderTree, faRefresh, faImages, faStickyNote, faRightFromBracket } from '@fortawesome/free-solid-svg-icons/index.js';
    import Router, { push } from 'svelte-spa-router'
    import Repositories from './components/Repositories.svelte'
    import Tree from './components/Tree.svelte'
    import Assets from './components/Assets.svelte'
    import EditWrapper from './components/EditWrapper.svelte';
    import ViewWrapper from './components/ViewWrapper.svelte';
    import ConfirmDialog from './components/ConfirmDialog.svelte';
    import Home from './components/Home.svelte'
    import NotFound from "./components/NotFound.svelte";
    import StashPage from './components/stash/StashPage.svelte';
    import {Context} from 'svelte-simple-modal';

    const modal = getContext<Context>('simple-modal');

    const routes = {
        // Exact path
        '/': Home,

        // Using named parameters, with last being optional
        '/repositories': Repositories,

        // Wildcard parameter
        '/edit/:id': EditWrapper,
        '/view/:id': ViewWrapper,
        '/tree/:id/:refresh?': Tree,
        '/new/:id': EditWrapper,
        '/assets': Assets,
        '/stashes': StashPage,

        // Catch-all
        // This is optional, but if present it must be the last
        '*': NotFound,
    }

    const doRefresh = async () => {
        const dendron = await DendronClient.GetDendron($repository.id, false);
        if (dendron.isOk) {
            
            setTree(dendron.theResult.hierarchy);
            $loadedNotes = dendron.theResult.notes;
            $draftNotes = [];
            push(`/tree/${$repository.id}`);
        }
        else {
            console.log(`an error happened ${dendron.code}-${dendron.conflictCode} : ${dendron.errorMessage}`);
            push(`/repositories`);
        }
    }

    const refresh= async () => {

        if ($draftNotes.length > 0) {
            const editedNotes = $draftNotes.map(x => `${x.header.title}`).join('\n- ');
            const message = `Are you sure to reload data ? Unsaved work will be lost. 
${editedNotes}`;
            modal.open(
                ConfirmDialog,
                {
                    message: message,
                    hasForm: true,
                    oncancel: async () => {},
                    onOkay: doRefresh
                },
                {
                    closeButton: true,
                    closeOnEsc: true,
                    closeOnOuterClick: true,
                }
            );
    
        }
        else {
            await doRefresh();
        }

        
    }

    const handleLogout = async () => {
        // Clear stash cache (categories and notes)
        StashCache.clearAll();
        
        // Call logout API to clear session and revoke GitHub token
        await DendronClient.logout();
        
        // Small delay to ensure cookie deletion is processed by browser
        await new Promise(resolve => setTimeout(resolve, 200));
        
        // Redirect to repositories endpoint which requires authentication
        // This will immediately trigger the OAuth flow since session is cleared
        window.location.replace('/repositories');
    }

</script>

<header>

    <a class="logo" href="#/">Dendr-Online</a>

    <input id="nav" type="checkbox">
    <label for="nav" id="burger"></label>

    <nav>
        <ul>
            <li><a href="#/repositories" ><Fa icon="{faList}"/><span style="margin-left: 5px">Repositories</span></a></li>
            {#if $repository && $repository.id}
                <li><a href="#/tree/{$repository.id}/refresh" on:click={refresh}><Fa icon="{faRefresh}"/><span style="margin-left: 5px">Refresh tree</span></a></li>
                <li><a href="#/tree/{$repository.id}" ><Fa icon="{faFolderTree}"/><span style="margin-left: 5px">Tree</span></a></li>
                <li><a href="#/assets"><Fa icon="{faImages}"/><span style="margin-left: 5px">Assets</span></a></li>
                <li><a href="#/stashes"><Fa icon="{faStickyNote}"/><span style="margin-left: 5px">Stashes</span></a></li>
                {/if}
            <li><a href="#/" on:click|preventDefault={handleLogout}><Fa icon="{faRightFromBracket}"/><span style="margin-left: 5px">Logout</span></a></li>
            {#if $noteId}
            <li>
                <ul>
                    <li><a href="#/edit/{$noteId}"><Fa icon="{faPen}"/><span style="margin-left: 5px">Edit</span></a></li>
                    <li><a href="#/view/{$noteId}"><Fa icon="{faEye}"/><span style="margin-left: 5px">View</span></a></li>
                </ul>
            </li>
            {/if}
        </ul>
    </nav>

</header>

<main>
    <Router {routes}/>
</main>