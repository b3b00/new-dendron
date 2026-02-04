// region partial tree loading helpers

/**
 * Insert children into a node in the tree by id, updating the store and cache.
 */
export function insertNodeChildren(parentId: string, children: Node[]) {
    tree.update(r => {
        if (!r) return r;
        // Find parent node recursively
        function findAndInsert(node: Node): boolean {
            if (node.id === parentId) {
                node.children = children;
                node.deployed = true;
                cacheNode(node);
                return true;
            }
            if (node.children) {
                for (let child of node.children) {
                    if (findAndInsert(child)) return true;
                }
            }
            return false;
        }
        findAndInsert(r);
        return r;
    });
}

/**
 * Mark a node as deployed (children loaded)
 */
export function markNodeDeployed(nodeId: string) {
    tree.update(r => {
        if (!r) return r;
        function findAndMark(node: Node): boolean {
            if (node.id === nodeId) {
                node.deployed = true;
                cacheNode(node);
                return true;
            }
            if (node.children) {
                for (let child of node.children) {
                    if (findAndMark(child)) return true;
                }
            }
            return false;
        }
        findAndMark(r);
        return r;
    });
}

/**
 * Check if a node's children are loaded (deployed)
 */
export function areNodeChildrenLoaded(nodeId: string): boolean {
    let loaded = false;
    tree.update(r => {
        function find(node: Node): boolean {
            if (node.id === nodeId) {
                loaded = !!node.deployed && Array.isArray(node.children) && node.children.length > 0;
                return true;
            }
            if (node.children) {
                for (let child of node.children) {
                    if (find(child)) return true;
                }
            }
            return false;
        }
        if (r) find(r);
        return r;
    });
    return loaded;
}

// endregion
// region localStorage caching for nodes/leaves

const NODE_CACHE_KEY = 'dendron_node_cache';
const TREE_FULLY_LOADED_KEY = 'dendron_tree_fully_loaded';

function getNodeCache(): Record<string, Node> {
    const raw = localStorage.getItem(NODE_CACHE_KEY);
    if (!raw) return {};
    try {
        return JSON.parse(raw);
    } catch {
        return {};
    }
}

function setNodeCache(cache: Record<string, Node>) {
    localStorage.setItem(NODE_CACHE_KEY, JSON.stringify(cache));
}

export function cacheNode(node: Node) {
    const cache = getNodeCache();
    cache[node.id] = node;
    setNodeCache(cache);
}

export function getCachedNode(id: string): Node | undefined {
    const cache = getNodeCache();
    return cache[id];
}

export function clearNodeCache() {
    localStorage.removeItem(NODE_CACHE_KEY);
}

export function setTreeFullyLoaded(fullyLoaded: boolean) {
    localStorage.setItem(TREE_FULLY_LOADED_KEY, fullyLoaded ? '1' : '0');
}

export function isTreeFullyLoaded(): boolean {
    return localStorage.getItem(TREE_FULLY_LOADED_KEY) === '1';
}

// endregion
import { Writable, writable } from 'svelte/store';
import {Node, Note, Repository} from '../scripts/types';

//region repositories
export const repository: Writable<Repository|undefined> = writable();

export const isFavoriteRepository: Writable<boolean> = writable(false);

export function setRepository(repo:Repository) {
    repository.update(r => { return repo });
}


export const repositories: Writable<Repository[]> = writable([]);

export function setRepositories(repos: Repository[]) {
    repositories.update(r => { return repos  });
}

export function addNote(note: Note) {
    const id = note.header.title;
    const path = id.split('.');
    const parentPath = path.slice(0, path.length - 1);
    tree.update(r => {
        let i = 0;
        let parent: Node|undefined = r;
        while (i < parentPath.length && parent) {
            const currentPath = parentPath.slice(0,i+1).join('.');
            let p = parent.children.filter(x => x.id === currentPath)[0];
            if (p) {
                parent = p;
            }
            else {
                parent = undefined;
                break;
            }
            i++;
        }
        if (parent) {
            if (!parent.children) {
                parent.children = [];
            }
            parent.children.push({
                id:note.header.id,
                name:note.header.title,
                children:[],
                deployed:true,
                edited:true,
                selected:true,
                isNode:false,
                isLeaf:true
            });
        }
        return r;
    });
    updateNote(id, note);

}

export function deleteNote(id: string, recurse:boolean) {
    // we will need something really brillant here    
    const path = id.split('.');
    const parentPath = path.slice(0, path.length - 1);
    tree.update(r => {
        let i = 0;
        let parent: Node|undefined = r;
        while (i < parentPath.length && parent) {
            const currentPath = parentPath.slice(0,i+1).join('.');
            let p = parent.children.filter(x => x.id === currentPath)[0];
            if (p) {
                parent = p;
            }
            else {
                parent = undefined;
                break;
            }
            i++;
        }
        if (parent) {
            if (!parent.children) {
                parent.children = [];
            }
            parent.children = parent.children.filter(x => x.id !== id);            
        }
        return r;
    });
}

// endregion

// region notes

export const noteId:Writable<string> = writable("");


export function getTitle(description: string) {
    if (description.startsWith("'")) {
        description = description.substring(1);
    }
    if (description.endsWith("'")) {
        description = description.substring(0,description.length-1);
    }
    return description;
}
export function setNoteId(id: string) {
    noteId.update((r:string) => { return id  });
}

export const draftNotes:Writable<Note[]> = writable([]);


export function updateNote(id:string,note:Note) {
    draftNotes.update((r) => {
        r = r.filter(x => x.header.title != id);
        r.push(note);        
        return r;
    });
}

export function unDraft(id:string) {
    draftNotes.update((r) => {
        r = r.filter(x => x.header.title != id);
        return r;
    } )
}

export function getDraftNote(id:string): Note|undefined {
    let note:Note|undefined = undefined;
    draftNotes.update((r) => {
        note =undefined;
        let f = r.filter(x => x.header.title == id);
        if (f.length > 0) {
            note = f[0];
        }
        return r;
    });
    return note;
}

export function isDraft(id:string) {
    let drafted = false;
    draftNotes.update((r) => {
        drafted = r.some(x => { 
            const d = x.header.title == id;
            return d;
        });
        return r;
    } );
    return drafted;
}


export const loadedNotes:Writable<Note[]> = writable([]);


export function setLoadedNote(id:string,note:Note) {
    loadedNotes.update((r) => {
        r = r.filter(x => x.header.title != id);
        r.push(note);        
        return r;
    });
}

export function unloadNote(id:string) {
    loadedNotes.update((r) => {
        r = r.filter(x => x.header.title != id);
        return r;
    } )
}

export function getLoadedNote(id:string): Note|undefined {
    let note:Note|undefined = undefined;
    loadedNotes.update((r) => {
        note =undefined;
        let f = r.filter(x => x.header.title == id);
        if (f.length > 0) {
            note = f[0];
        }        
        return r;
    });
    return note;
}

// endregion

// region tree
export const tree:Writable<Node|undefined> = writable();

export function setTree(currentTree: Node) {
    tree.update(r => {  return currentTree  });
}

export function getBackLinks(note:string): Note[] {
    let back:Note[] = []
    loadedNotes.update((notes) => {
        back = notes.filter(x => x.body.includes(`[[${note}]]`));
        return notes;
    })
    return back;
}

//endregion

