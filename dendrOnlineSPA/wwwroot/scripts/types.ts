import { TVNode } from "@bolduh/svelte-nested-accordion/src/AccordionTypes";

export class Node implements TVNode {
  name: string = "";
  isLeaf: boolean = true;
  isNode: boolean = false;
  deployed: boolean = false;
  selected: boolean = false;
  edited: boolean = false;
  children: Node[] = [];
  id: string = "";
}

export class HierarchyAndSha {
  hierarchy: Node = new Node();
  sha: string = "";
}

export interface NoteHeader {
  id: string;
  name: string;
  title: string;
  description: string;
  categoryId?: string;
  lastUpdatedTS: number;
  createdTS: number;
}

export interface Note {
  header: NoteHeader;
  body: string;
  sha: string | undefined;
}

export interface TaggedNote {
  isDraft: boolean;
  note: Note;
}

export interface Repository {
  id: string;
  name: string;
  owner:string;
}

export interface SelectionItem {
  id: string;
  label: string;
}


export const emptyNote: Note = {
  header: {
    id: "**none**",
    name: "",
    title: "",
    description: "",
    lastUpdatedTS: 0,
    createdTS: 0,
  },
  body: "",
  sha: undefined,
};

export enum ConflictCode {
    NoConflict,
    Modified,
    Deleted,
    Created
}

export enum BackEndResultCode {
  Ok,
  Conflict,
  InternalError,
  NotFound
}



export enum ResultCode {
  Modified,
  Deleted,
  Created,
}


export interface BackEndResult<T> {
    theResult: T|undefined;
    code: BackEndResultCode;
    conflictCode : ConflictCode;
    isOk: boolean;
    errorMessage: string;
}

export class Dendron {
  hierarchy: Node = new Node();
  notes: Note[] = [];
  repositoryId: string = "";
  repositoryName: string = "";
  repositoryOwner: string = "";
  isFavoriteRepository: boolean = false;  
}

export class ImageAsset {
  name: string = "";
  url: string = "";
}

export interface NoteFilter {
  filter : string,
  searchInNotes : boolean
}

export interface Favorite {
  user : number,
  repository : number,
  repositoryName : string
}

export interface ViewContext {
  getNoteId() : string;
  type: 'dendron' | 'stash';
  categoryId?: string; // Only for stash notes
  onNoteUpdated?: () => void; // Callback when note content is updated
}

export interface PaletteCommand {
  action: (arg?: string) => void;
  description?: string;
  suggestions?: (arg?: string) => Promise<{ label: string, value: string, description?: string }[]>;
  suggestionsDescription?: string;
  allowFreeText?: boolean;
}

export type PaletteCommandMap = Record<string, PaletteCommand | ((arg?: string) => void)>;