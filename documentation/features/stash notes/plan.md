
Voici le contenu markdown brut sans bloc de code englobant :
Plan d'implémentation - Système d'identification des notes
Système d'identification retenu
Format de l'identifiant
{index}:{hash}
Exemple : "0:a3f2b8c1"
Index : position de la note dans le fichier (base 0)
Hash : 8 premiers caractères du SHA256 du contenu de la note
Avantages
✅ Aucune donnée technique ajoutée dans le contenu markdown
✅ Simple : l'index permet de cibler la note rapidement
✅ Sécurisé : le hash détecte les modifications concurrentes
✅ Performant : calcul du hash lors de la lecture uniquement
Implémentation Backend (C#)
1. Génération de l'identifiant
   using System.Security.Cryptography;
   using System.Text;
```csharp
public static string GenerateNoteId(int index, string content)
{
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
var shortHash = BitConverter.ToString(hashBytes)
.Replace("-", "")
.Substring(0, 8)
.ToLower();

    return $"{index}:{shortHash}";
}
```

2. Vérification du hash

```csharp 
   public static bool VerifyNoteHash(string noteId, string currentContent)
   {
   var parts = noteId.Split(':');
   if (parts.Length != 2) return false;

   if (!int.TryParse(parts[0], out var index)) return false;
   var providedHash = parts[1];

   var currentHash = GenerateNoteId(index, currentContent).Split(':')[1];
   return providedHash == currentHash;
   }
```
3. Parsing d'un identifiant

```csharp 
   public class NoteIdentifier
   {
   public int Index { get; set; }
   public string Hash { get; set; }

   public static NoteIdentifier Parse(string noteId)
   {
   var parts = noteId.Split(':');
   if (parts.Length != 2)
   throw new ArgumentException("Invalid note ID format");

        if (!int.TryParse(parts[0], out var index))
            throw new ArgumentException("Invalid index in note ID");
            
        return new NoteIdentifier
        {
            Index = index,
            Hash = parts[1]
        };
   }
   }
```   
   DTOs mis à jour
   CategoryListItemDto

```csharp
   public class CategoryListItemDto
   {
   public string Id { get; set; }
   public string Title { get; set; }
   public string Description { get; set; }
   public int NotesCount { get; set; }
   }
```   
   NoteDto
```csharp
   public class NoteDto
   {
   public string Id { get; set; }           // Format: "index:hash"
   public string Title { get; set; }        // null si pas de titre (pas de #)
   public string Content { get; set; }      // Contenu markdown complet
   }
```

   CreateNoteRequest
```csharp
   public class CreateNoteRequest
   {
   public string Content { get; set; }
   }
```  
   UpdateNoteRequest
```csharp
   public class UpdateNoteRequest
   {
   public string Content { get; set; }
   }
```   
   Logique des endpoints

**GET /stash/categories/{category_id}**

1. Lire le fichier de catégorie
2. Parser les notes (séparées par `______________`)
3. Pour chaque note (index i) :
   - Extraire le contenu
   - Extraire le titre (si commence par #)
   -Générer l'ID : `GenerateNoteId(i, content)`
4. Retourner la liste des NoteDto   

**POST /stash/categories/{category_id}**
       1. Lire le fichier de catégorie
       2. Ajouter le séparateur `______________`
       3 .Ajouter le nouveau contenu
       3. Mettre à jour le timestamp updated
       4. Écrire le fichier
       5. Retourner le NoteDto avec son nouvel ID

**PUT /stash/categories/{category_id}/note/{note_id}**
    1. Parser le note_id pour extraire index et hash
    2. Lire le fichier de catégorie
    3. Parser les notes
    4. Vérifier que l'index existe
    5. Vérifier le hash : VerifyNoteHash(note_id, notes[index])
        - Si différent → 409 Conflict : "Note has been modified by another user"
    6. Remplacer le contenu à l'index donné
    7. Mettre à jour le timestamp updated
    8. Écrire le fichier
    9. Retourner le NoteDto mis à jour avec son nouveau hash

**DELETE /stash/categories/{category_id}/note/{note_id}**
    1. Parser le note_id pour extraire index et hash
    2. Lire le fichier de catégorie
    3. Parser les notes
    4. Vérifier que l'index existe
    5. Vérifier le hash : VerifyNoteHash(note_id, notes[index])
        - Si différent → 409 Conflict
    6. Supprimer la note à l'index donné
    7. Supprimer le séparateur précédent
    8. Mettre à jour le timestamp updated
    9. Écrire le fichier
    10. Retourner 204 No Content

## Gestion des erreurs
**409 Conflict**
```csharp   
   public class ConflictResult
   {
   public string Error { get; set; } = "Note has been modified";
   public string CurrentNoteId { get; set; }  // Nouvel ID avec hash actuel
   public string CurrentContent { get; set; }  // Contenu actuel
   }
```   
**404 Not Found**
    - Catégorie inexistante
    - Index de note hors limites

**400 Bad Request**
 - Format de note_id invalide
 - Contenu vide dans POST/PUT


   ### Comportement Frontend


**Après chaque opération de modification**

Le frontend doit rafraîchir la liste des notes car :
 * Les index changent après un DELETE
 * Les hash changent après un PUT
 * Un nouveau note_id est attribué après chaque modification

**Gestion du 409 Conflict**
1. Afficher un message : "Cette note a été modifiée par quelqu'un d'autre"
2. Proposer :
  - Voir la version actuelle
  - Écraser avec ma version
  - Annuler
   
### Exemple de flux complet
#### Scénario : Modification concurrente**
**Utilisateur A :**
1. GET → reçoit note "0:a3f2b8c1" avec contenu "Hello"
2. Modifie localement en "Hello World"

**Utilisateur B (entre-temps) :**
1. GET → reçoit note "0:a3f2b8c1" avec contenu "Hello"
2. PUT "0:a3f2b8c1" avec "Bonjour" → 200 OK
3. Note devient "0:b7e4f9d2" (nouveau hash)
4. 
**Utilisateur A (continue) :**
1.PUT "0:a3f2b8c1" avec "Hello World"
2. Backend vérifie : hash a3f2b8c1 ≠ hash actuel b7e4f9d2
3. Retour 409 Conflict avec contenu actuel "Bonjour"
4. L'utilisateur A décide quoi faire

### Optimisations possibles

#### Cache du hash
Si performance critique, stocker les hash en mémoire :
```csharp
private static Dictionary<string, Dictionary<int, string>> _categoryNotesHashes = new();
```

Invalider le cache lors de toute modification du fichier.

#### Lecture partielle
Pour de très gros fichiers, lire uniquement la note à l'index donné plutôt que tout parser.

### Sécurité


#### Validation du path
Toujours valider que category_id ne contient pas de .. ou / pour éviter les path traversal.

```csharp 
public static bool IsValidCategoryId(string categoryId)
{
return !string.IsNullOrWhiteSpace(categoryId)
&& !categoryId.Contains("..")
&& !categoryId.Contains("/")
&& !categoryId.Contains("\\");
}
```` 

#### Validation du contenu

Limiter la taille du contenu d'une note (ex: 1 MB max).

### Tests unitaires à prévoir

1. `GenerateNoteId` : vérifier le format et la reproductibilité
2.`VerifyNoteHash` : cas valide et invalide
3. `NoteIdentifier.Parse` : formats valides et invalides
4. Endpoints : scénarios nominaux et d'erreur
5. Concurrence : simuler modifications simultanées

## Prochaines étapes
1. ✅ Spécification complète
2. ✅ Implémentation backend
  - ✅ Service de gestion des fichiers markdown
  - ✅ Parsing et génération des fichiers
  - ✅ Contrôleurs API: `StashController`
3. ✅ Tests unitaires et d'intégration
4. ⏳ Implémentation frontend
5. ✅ Documentation API

---

## Frontend Implementation Plan

### Architecture Overview

**Components Structure:**
```
wwwroot/
  components/
    stash/
      StashPage.svelte           - Main container page
      CategorySelector.svelte    - Category selection/creation
      CategoryList.svelte        - List of categories
      NotesList.svelte           - Accordion list of notes
      NoteAccordion.svelte       - Individual note accordion item
      NoteEditor.svelte          - Note editing component
      SearchBox.svelte           - Search/filter component
```

### 1. Navigation Integration

**Update Burger Menu:**
- Add "Stashes" entry in main navigation menu
- Route: `/stashes`
- Icon: appropriate stash/note icon

### 2. Main Stash Page Component

**StashPage.svelte** - Main container component

**State Management:**
```javascript
let categories = [];          // List of all categories
let selectedCategory = null;  // Currently selected category
let notes = [];               // Notes in selected category
let searchTerm = '';          // Search filter
let isLoading = false;
let error = null;
```

**Layout:**
- Header with title "Stashes"
- Category selector section (top)
- Search box (when category selected)
- Notes list (accordion view)

**API Integration:**
- Fetch categories on mount
- Handle category selection
- Refresh on operations (add/edit/delete)

### 3. Category Management Components

#### CategorySelector.svelte

**Features:**
- Dropdown/select to choose existing category
- "Create New Category" button
- Display current category description

**Actions:**
- `onSelectCategory(categoryId)` - Load notes for category
- `onCreateCategory()` - Show creation dialog

**Creation Dialog:**
```
Modal/Dialog with form:
  - Title (required)
  - Description (optional)
  - Cancel / Create buttons
```

**API Calls:**
- GET /stash/categories - Load categories
- POST /stash/category - Create new category

#### CategoryList.svelte (Alternative View)

**Features:**
- Display as clickable cards/list items
- Show: title, description, note count
- Visual indication of selected category
- Create new category button

### 4. Notes Display Components

#### SearchBox.svelte

**Features:**
- Text input for search term
- Clear button (X icon)
- Real-time filtering
- Search placeholder: "Search in notes..."

**Filtering Logic:**
- Filter by note title (if exists)
- Filter by note content
- Case-insensitive search
- Debounce input (300ms)

#### NotesList.svelte

**Features:**
- Display notes as accordion list
- Empty state message: "No notes yet. Add your first note!"
- Filtered list based on search term
- "Add New Note" button (prominent, top of list)

**Add Note Flow:**
```
Button click → Show inline editor / modal:
  - Textarea for content (markdown)
  - Markdown preview toggle (optional)
  - Cancel / Save buttons
```

**API Calls:**
- GET /stash/categories/{id} - Load notes
- POST /stash/categories/{id} - Create note

#### NoteAccordion.svelte

**Props:**
- `note` - Note object (id, title, content)
- `isOpen` - Accordion expanded state
- `isEditing` - Edit mode state

**Display Mode:**
- Collapsed: Show title or first line of content
- Expanded: 
  - Rendered markdown content
  - Edit button (pen icon, top-right)
  - Delete button (trash icon, top-right)

**Edit Mode:**
- Textarea with current content
- Save button (checkmark icon)
- Cancel button (X icon)
- Markdown preview toggle (optional)

**Actions:**
- `onToggle()` - Expand/collapse accordion
- `onEdit()` - Enter edit mode
- `onSave(content)` - Save changes (with conflict handling)
- `onDelete()` - Delete note (with confirmation)
- `onCancel()` - Cancel editing

**API Calls:**
- PUT /stash/categories/{catId}/note/{noteId} - Update note
- DELETE /stash/categories/{catId}/note/{noteId} - Delete note

#### NoteEditor.svelte (Reusable)

**Features:**
- Textarea for markdown content
- Character/line counter (optional)
- Save / Cancel buttons
- Validation (not empty)

**Props:**
- `initialContent` - Starting content
- `onSave(content)` - Save callback
- `onCancel()` - Cancel callback

### 5. UI/UX Details

#### Accordion Behavior
```
┌─────────────────────────────────────┐
│ [>] Shopping List          ✏️ 🗑️    │  ← Collapsed
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ [v] Shopping List          ✏️ 🗑️    │  ← Expanded
│                                     │
│ # Shopping List                     │
│                                     │
│ - Milk                              │
│ - Eggs                              │
│ - Bread                             │
│                                     │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ [v] Shopping List          ✔️ ❌    │  ← Edit Mode
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ # Shopping List               │ │
│ │                                 │ │
│ │ - Milk                          │ │
│ │ - Eggs                          │ │
│ │ - Bread                         │ │
│ │ - Cheese                        │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

#### Icons
- ✏️ Edit (pen/pencil icon)
- 🗑️ Delete (trash icon)
- ✔️ Save (checkmark icon)
- ❌ Cancel (X icon)
- ➕ Add (plus icon)
- 🔍 Search (magnifying glass)

#### Styling
- Use existing app styles/themes
- Accordion: smooth expand/collapse animation
- Hover states on buttons
- Loading spinners during API calls
- Toast notifications for success/error messages

### 6. Error Handling & User Feedback

#### Conflict Handling (409)
```javascript
try {
  const result = await updateNote(categoryId, noteId, content);
} catch (error) {
  if (error.status === 409) {
    // Show conflict dialog
    showDialog({
      title: "Conflict Detected",
      message: "This note was modified by someone else.",
      actions: [
        { label: "View Current Version", action: loadCurrentVersion },
        { label: "Overwrite Anyway", action: forceUpdate },
        { label: "Cancel", action: cancelEdit }
      ]
    });
  }
}
```

#### Delete Confirmation
```javascript
async function handleDelete() {
  const confirmed = await confirm(
    "Delete this note?",
    "This action cannot be undone."
  );
  
  if (confirmed) {
    // Proceed with deletion
  }
}
```

#### Success Messages
- "Category created successfully"
- "Note added"
- "Note updated"
- "Note deleted"

#### Error Messages
- "Failed to load categories"
- "Failed to save note"
- "Connection error, please try again"

### 7. State Management

**Reactive Updates:**
```javascript
// After any modification, refresh the notes list
async function refreshNotes() {
  if (selectedCategory) {
    notes = await fetchNotes(selectedCategory.id);
  }
}

// After category operations, refresh categories
async function refreshCategories() {
  categories = await fetchCategories();
}
```

**Optimistic Updates (Optional):**
- Update UI immediately
- Revert if API call fails
- Show loading indicator

### 8. API Service Layer

**Create `stashApi.js` service:**

```javascript
export const stashApi = {
  // Categories
  getCategories: () => fetch('/stash/categories'),
  createCategory: (data) => fetch('/stash/category', { method: 'POST', body: JSON.stringify(data) }),
  updateCategory: (id, data) => fetch(`/stash/category/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  
  // Notes
  getNotes: (categoryId) => fetch(`/stash/categories/${categoryId}`),
  createNote: (categoryId, content) => fetch(`/stash/categories/${categoryId}`, { method: 'POST', body: JSON.stringify({ content }) }),
  updateNote: (categoryId, noteId, content) => fetch(`/stash/categories/${categoryId}/note/${noteId}`, { method: 'PUT', body: JSON.stringify({ content }) }),
  deleteNote: (categoryId, noteId) => fetch(`/stash/categories/${categoryId}/note/${noteId}`, { method: 'DELETE' })
};
```

### 9. Markdown Rendering

**Use existing markdown renderer or add one:**
- Library: marked.js, markdown-it, or similar
- Sanitize HTML output for security
- Support basic markdown: headers, lists, code blocks, bold, italic

```javascript
import { marked } from 'marked';

function renderMarkdown(content) {
  return marked.parse(content);
}
```

### 10. Search/Filter Implementation

```javascript
function filterNotes(notes, searchTerm) {
  if (!searchTerm) return notes;
  
  const term = searchTerm.toLowerCase();
  
  return notes.filter(note => {
    const titleMatch = note.title?.toLowerCase().includes(term);
    const contentMatch = note.content.toLowerCase().includes(term);
    return titleMatch || contentMatch;
  });
}

$: filteredNotes = filterNotes(notes, searchTerm);
```

### 11. Implementation Steps

**Phase 1: Basic Structure**
1. Create route /stashes
2. Add menu entry
3. Create StashPage.svelte skeleton
4. Implement category loading and display

**Phase 2: Category Management**
1. Implement CategorySelector component
2. Add category creation dialog
3. Handle category selection

**Phase 3: Notes Display**
1. Implement NotesList component
2. Create NoteAccordion component
3. Add markdown rendering
4. Implement expand/collapse

**Phase 4: Notes Operations**
1. Add note creation
2. Implement note editing
3. Implement note deletion with confirmation
4. Handle conflicts (409 responses)

**Phase 5: Search & Polish**
1. Implement SearchBox component
2. Add search/filter logic
3. Add loading states
4. Add error handling and user feedback
5. Polish UI/UX
6. Add animations

**Phase 6: Testing**
1. Manual testing of all features
2. Test conflict scenarios
3. Test error handling
4. Mobile responsiveness check

### 12. Responsive Design

**Mobile Considerations:**
- Stack category selector and search vertically
- Full-width accordions
- Touch-friendly button sizes (min 44x44px)
- Swipe gestures for accordion (optional)
- Bottom action buttons for mobile keyboards

**Desktop:**
- Side panel for categories (optional)
- Keyboard shortcuts (optional)
  - Ctrl+N: New note
  - Ctrl+F: Focus search
  - Esc: Cancel edit/close dialogs

### 13. Accessibility

- Semantic HTML
- ARIA labels for icons
- Keyboard navigation support
- Focus management in modals
- Screen reader friendly
- Color contrast compliance

### 14. Performance Optimizations

- Lazy load notes (only when category selected)
- Debounce search input
- Virtual scrolling for large note lists (if needed)
- Cache category list
- Minimize re-renders

---

## Prochaines étapes

1. ✅ Spécification backend complète
2. ✅ Implémentation backend avec tests
3. ✅ Documentation API
4. ⏳ **Implémentation frontend (prochaine étape)**
   - Route et navigation
   - Composants Svelte
   - Intégration API
   - UI/UX polish
5. ⏳ Tests end-to-end
6. ⏳ Déploiement

