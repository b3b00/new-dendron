# Stash Notes API Documentation

## Base URL
All endpoints are prefixed with `/stash`

## Configuration
The stash notes path can be configured in `appsettings.json`:
```json
{
  "StashNotesPath": "c:\\path\\to\\stashes"
}
```

If not configured, defaults to `./stashes` directory.

---

## Endpoints

### Categories

#### GET /stash/categories
Get all available stash categories.

**Response**: `200 OK`
```json
[
  {
    "id": "category-uuid-123",
    "title": "Quick Notes",
    "description": "Collection of quick notes and reminders",
    "notesCount": 5
  }
]
```

---

#### POST /stash/category
Create a new category.

**Request Body**:
```json
{
  "title": "My New Category",
  "description": "Optional description"
}
```

**Response**: `201 Created`
```json
{
  "id": "generated-uuid",
  "title": "My New Category",
  "description": "Optional description",
  "notesCount": 0
}
```

**Errors**:
- `400 Bad Request` - Title is required or invalid
- `500 Internal Server Error` - File system error

---

#### PUT /stash/category/{categoryId}
Update a category's title and/or description.

**Request Body**:
```json
{
  "title": "Updated Title",
  "description": "Updated description"
}
```

**Response**: `200 OK`
```json
{
  "id": "category-uuid-123",
  "title": "Updated Title",
  "description": "Updated description",
  "notesCount": 5
}
```

**Errors**:
- `400 Bad Request` - Invalid category ID or at least one field required
- `404 Not Found` - Category not found

---

### Notes

#### GET /stash/categories/{categoryId}
Get all notes in a category.

**Response**: `200 OK`
```json
[
  {
    "id": "0:a1b2c3d4",
    "title": "Shopping List",
    "content": "# Shopping List\n\n- Milk\n- Eggs"
  },
  {
    "id": "1:e5f6g7h8",
    "title": null,
    "content": "Note without a title"
  }
]
```

**Note ID Format**: `{index}:{hash}`
- `index`: Position of the note in the file (0-based)
- `hash`: First 8 characters of SHA256 hash of the content

**Errors**:
- `400 Bad Request` - Invalid category ID (path traversal attempt)
- `404 Not Found` - Category not found

---

#### POST /stash/categories/{categoryId}
Add a new note to a category.

**Request Body**:
```json
{
  "content": "# My New Note\n\nNote content here"
}
```

**Response**: `201 Created`
```json
{
  "id": "2:i9j0k1l2",
  "title": "My New Note",
  "content": "# My New Note\n\nNote content here"
}
```

**Errors**:
- `400 Bad Request` - Content is required, invalid category ID, or content too large (>1MB)
- `404 Not Found` - Category not found

---

#### PUT /stash/categories/{categoryId}/note/{noteId}
Update a note's content.

**Request Body**:
```json
{
  "content": "# Updated Note\n\nUpdated content"
}
```

**Response**: `200 OK`
```json
{
  "id": "0:m3n4o5p6",
  "title": "Updated Note",
  "content": "# Updated Note\n\nUpdated content"
}
```

**Note**: The note ID will change because the hash is based on content.

**Errors**:
- `400 Bad Request` - Invalid note ID format, content required, or content too large
- `404 Not Found` - Category or note not found
- `409 Conflict` - Note has been modified by another user (hash mismatch)

**Conflict Response**:
```json
{
  "error": "Note has been modified by another user",
  "conflictCode": "Modified"
}
```

---

#### DELETE /stash/categories/{categoryId}/note/{noteId}
Delete a note from a category.

**Response**: `204 No Content`

**Errors**:
- `400 Bad Request` - Invalid note ID format or category ID
- `404 Not Found` - Category or note not found
- `409 Conflict` - Note has been modified by another user (hash mismatch)

---

## Note ID System

### Generation
Each note gets an ID in the format `{index}:{hash}`:
```
0:a1b2c3d4
```

- **Index**: Position in the file (0-based)
- **Hash**: First 8 chars of SHA256 hash of content

### Conflict Detection
The hash ensures concurrent modifications are detected:

1. User A reads note "0:abc123" with content "Hello"
2. User B updates to "Hi" → becomes "0:def456"
3. User A tries to update "0:abc123" → **409 Conflict** (hash doesn't match)

### Index Changes
After deleting a note, indexes shift:
```
Before:          After deleting note 1:
0:hash1          0:hash1
1:hash2          1:hash3  (was index 2)
2:hash3          2:hash4  (was index 3)
3:hash4
```

**Always refresh the note list after modifications!**

---

## File Format

Categories are stored as markdown files in the stashes directory:

```markdown
---
id: category-uuid
title: Quick Notes
desc: Collection of notes
updated: 1738108800
created: 1738022400
---

# First Note Title

Note content here.

______________

Second note without title.

______________

# Third Note

More content.
```

---

## Security

### Path Traversal Protection
Category IDs are validated to prevent path traversal attacks:
- ✅ Valid: `my-category`, `notes_123`
- ❌ Invalid: `../etc/passwd`, `path/to/file`, `..\\windows`

### Content Size Limit
Notes are limited to 1 MB to prevent abuse.

---

## Example Usage

### cURL Examples

**Create a category:**
```bash
curl -X POST http://localhost:5000/stash/category \
  -H "Content-Type: application/json" \
  -d '{"title":"My Notes","description":"Personal notes"}'
```

**Add a note:**
```bash
curl -X POST http://localhost:5000/stash/categories/category-uuid \
  -H "Content-Type: application/json" \
  -d '{"content":"# Shopping\n\n- Milk\n- Eggs"}'
```

**Update a note:**
```bash
curl -X PUT http://localhost:5000/stash/categories/category-uuid/note/0:abc123 \
  -H "Content-Type: application/json" \
  -d '{"content":"# Shopping\n\n- Milk\n- Eggs\n- Bread"}'
```

**Delete a note:**
```bash
curl -X DELETE http://localhost:5000/stash/categories/category-uuid/note/0:abc123
```

### JavaScript Example

```javascript
// Create category
const category = await fetch('/stash/category', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    title: 'My Notes',
    description: 'Personal notes'
  })
}).then(r => r.json());

// Add note
const note = await fetch(`/stash/categories/${category.id}`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    content: '# Task\n\nComplete the project'
  })
}).then(r => r.json());

// Update note
const updated = await fetch(`/stash/categories/${category.id}/note/${note.id}`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    content: '# Task\n\n✓ Complete the project'
  })
}).then(r => r.json());

// Handle conflicts
if (response.status === 409) {
  const conflict = await response.json();
  console.log('Conflict:', conflict.error);
  // Show user the current content or merge changes
}
```

---

## Testing

Run the integration tests:
```bash
dotnet test Tests/StashControllerIntegrationTests.cs
```

25 tests covering:
- ✓ Category CRUD operations
- ✓ Note CRUD operations  
- ✓ Conflict detection
- ✓ Hash verification
- ✓ Path traversal protection
- ✓ Error handling
- ✓ Full workflow scenarios
