# Stash Notes Caching Plan

## Overview

This plan addresses the caching requirements for stash notes to minimize GitHub API round trips and improve performance, similar to the existing dendron notes caching strategy.

## Requirements Summary

From [description.md](description.md#caching):
1. **ASP.NET Session Cache** - Categories and notes must be stored in server-side session
2. **Frontend LocalStorage** - Categories and stashes must be stored client-side
3. **Consistency** - Maintain consistency between frontend, backend session, and GitHub

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │            LocalStorage Cache                       │    │
│  │  - Categories (list + timestamps)                   │    │
│  │  - Notes by category (content + metadata)           │    │
│  │  - Last sync timestamps                             │    │
│  └────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            ↕ HTTP
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Backend                           │
│  ┌────────────────────────────────────────────────────┐    │
│  │            Session Cache                            │    │
│  │  - StashCategories: List<CategoryListItemDto>       │    │
│  │  - StashNotes_{categoryId}: List<StashNote>         │    │
│  │  - StashFileHashes_{categoryId}: string (SHA)       │    │
│  │  - StashCategoriesTimestamp: DateTime               │    │
│  └────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            ↕ Octokit
┌─────────────────────────────────────────────────────────────┐
│                      GitHub Repository                       │
│                       stashes/*.md                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Backend Implementation

### 1. Session Cache Keys

Add to `dendrOnlineSPA/Model/Extensions.cs`:

```csharp
// Stash cache keys
public const string StashCategoriesKey = "stash_categories";
public const string StashCategoriesTimestampKey = "stash_categories_timestamp";
public const string StashNotesPrefix = "stash_notes_"; // + categoryId
public const string StashFileHashPrefix = "stash_file_hash_"; // + categoryId
public const string StashNotesTimestampPrefix = "stash_notes_timestamp_"; // + categoryId
```

### 2. Cache Extension Methods

Add these extension methods to `Extensions.cs`:

```csharp
// Category cache
public static List<CategoryListItemDto>? GetCachedCategories(this HttpContext httpContext)
{
    return httpContext.Session.GetObject<List<CategoryListItemDto>>(StashCategoriesKey);
}

public static void SetCachedCategories(this HttpContext httpContext, List<CategoryListItemDto> categories)
{
    httpContext.Session.SetObject(StashCategoriesKey, categories);
    httpContext.Session.SetString(StashCategoriesTimestampKey, DateTime.UtcNow.ToString("O"));
}

public static void InvalidateCategoriesCache(this HttpContext httpContext)
{
    httpContext.Session.Remove(StashCategoriesKey);
    httpContext.Session.Remove(StashCategoriesTimestampKey);
}

// Notes cache
public static List<StashNote>? GetCachedNotes(this HttpContext httpContext, string categoryId)
{
    return httpContext.Session.GetObject<List<StashNote>>($"{StashNotesPrefix}{categoryId}");
}

public static void SetCachedNotes(this HttpContext httpContext, string categoryId, 
    List<StashNote> notes, string fileHash)
{
    httpContext.Session.SetObject($"{StashNotesPrefix}{categoryId}", notes);
    httpContext.Session.SetString($"{StashFileHashPrefix}{categoryId}", fileHash);
    httpContext.Session.SetString($"{StashNotesTimestampPrefix}{categoryId}", 
        DateTime.UtcNow.ToString("O"));
}

public static string? GetCachedFileHash(this HttpContext httpContext, string categoryId)
{
    return httpContext.Session.GetString($"{StashFileHashPrefix}{categoryId}");
}

public static void InvalidateNotesCache(this HttpContext httpContext, string categoryId)
{
    httpContext.Session.Remove($"{StashNotesPrefix}{categoryId}");
    httpContext.Session.Remove($"{StashFileHashPrefix}{categoryId}");
    httpContext.Session.Remove($"{StashNotesTimestampPrefix}{categoryId}");
}

public static void InvalidateAllStashCache(this HttpContext httpContext)
{
    // Get all session keys and remove stash-related ones
    httpContext.InvalidateCategoriesCache();
    // Note: To invalidate all notes caches, we'd need to track category IDs
    // Alternative: Clear entire session or implement a registry
}
```

### 3. Update StashController

Modify `Controllers/StashController.cs` to use cache:

```csharp
[HttpGet("categories")]
public async Task<ActionResult<List<CategoryListItemDto>>> GetCategories()
{
    // Check cache first
    var cached = HttpContext.GetCachedCategories();
    if (cached != null)
    {
        return Ok(cached);
    }
    
    // Cache miss - fetch from GitHub
    var categories = await StashService.GetCategoriesAsync();
    
    // Store in cache
    HttpContext.SetCachedCategories(categories);
    
    return Ok(categories);
}

[HttpGet("categories/{categoryId}")]
public async Task<ActionResult<List<StashNote>>> GetNotes(string categoryId)
{
    // Check cache first
    var cachedNotes = HttpContext.GetCachedNotes(categoryId);
    if (cachedNotes != null)
    {
        // Optional: Verify file hasn't changed on GitHub using SHA
        var cachedHash = HttpContext.GetCachedFileHash(categoryId);
        var currentHash = await StashService.GetFileHashAsync(categoryId);
        
        if (cachedHash == currentHash)
        {
            return Ok(cachedNotes);
        }
        // Hash mismatch - cache is stale, continue to fetch
    }
    
    // Cache miss or stale - fetch from GitHub
    var notes = await StashService.GetNotesAsync(categoryId);
    var fileHash = await StashService.GetFileHashAsync(categoryId);
    
    // Store in cache
    HttpContext.SetCachedNotes(categoryId, notes, fileHash);
    
    return Ok(notes);
}

[HttpPost("category")]
public async Task<ActionResult<CategoryListItemDto>> CreateCategory([FromBody] CreateCategoryRequest request)
{
    // ... existing validation ...
    
    var category = await StashService.CreateCategoryAsync(request.Title, request.Description);
    
    // Invalidate categories cache
    HttpContext.InvalidateCategoriesCache();
    
    return CreatedAtAction(nameof(GetNotes), new { categoryId = category.Id }, category);
}

[HttpPut("category/{categoryId}")]
public async Task<ActionResult<CategoryListItemDto>> UpdateCategory(
    string categoryId, [FromBody] UpdateCategoryRequest request)
{
    // ... existing logic ...
    
    var category = await StashService.UpdateCategoryAsync(categoryId, request.Title, request.Description);
    
    // Invalidate categories cache
    HttpContext.InvalidateCategoriesCache();
    
    return Ok(category);
}

[HttpDelete("category/{categoryId}")]
public async Task<ActionResult> DeleteCategory(string categoryId)
{
    // ... existing logic ...
    
    await StashService.DeleteCategoryAsync(categoryId);
    
    // Invalidate both categories and notes cache for this category
    HttpContext.InvalidateCategoriesCache();
    HttpContext.InvalidateNotesCache(categoryId);
    
    return NoContent();
}

[HttpPost("categories/{categoryId}")]
public async Task<ActionResult<StashNote>> CreateNote(
    string categoryId, [FromBody] CreateNoteRequest request)
{
    // ... existing validation ...
    
    var note = await StashService.CreateNoteAsync(categoryId, request.Content);
    
    // Invalidate both caches (category count changed, notes list changed)
    HttpContext.InvalidateCategoriesCache();
    HttpContext.InvalidateNotesCache(categoryId);
    
    return CreatedAtAction(nameof(GetNotes), new { categoryId }, note);
}

[HttpPut("categories/{categoryId}/note/{noteId}")]
public async Task<ActionResult<StashNote>> UpdateNote(
    string categoryId, string noteId, [FromBody] UpdateNoteRequest request)
{
    // ... existing logic ...
    
    var note = await StashService.UpdateNoteAsync(categoryId, noteId, request.Content);
    
    // Invalidate notes cache only (category count unchanged)
    HttpContext.InvalidateNotesCache(categoryId);
    
    return Ok(note);
}

[HttpDelete("categories/{categoryId}/note/{noteId}")]
public async Task<ActionResult> DeleteNote(string categoryId, string noteId)
{
    // ... existing logic ...
    
    await StashService.DeleteNoteAsync(categoryId, noteId);
    
    // Invalidate both caches (category count changed, notes list changed)
    HttpContext.InvalidateCategoriesCache();
    HttpContext.InvalidateNotesCache(categoryId);
    
    return NoContent();
}
```

### 4. Add File Hash Support

Update `IStashNotesService.cs` and implementations:

```csharp
public interface IStashNotesService
{
    // ... existing methods ...
    
    Task<string> GetFileHashAsync(string categoryId);
}
```

In `GithubStashNotesService.cs`:

```csharp
public async Task<string> GetFileHashAsync(string categoryId)
{
    var fileName = GetFileName(categoryId);
    var path = $"{StashesPath}/{fileName}";
    
    try
    {
        var fileInfo = await _gitHubClient.Repository.Content.GetAllContents(_repositoryId, path);
        return fileInfo[0].Sha; // GitHub's SHA for the file
    }
    catch (NotFoundException)
    {
        throw new FileNotFoundException($"Category file not found: {categoryId}");
    }
}
```

In `FsStashNotesService.cs`:

```csharp
public async Task<string> GetFileHashAsync(string categoryId)
{
    var filePath = GetFilePath(categoryId);
    
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"Category file not found: {categoryId}");
    
    // Use file's last write time as a simple hash alternative
    var lastWrite = File.GetLastWriteTimeUtc(filePath);
    return lastWrite.Ticks.ToString();
}
```

---

## Frontend Implementation

### 1. LocalStorage Cache Structure

Create `wwwroot/scripts/stashCache.ts`:

```typescript
interface CachedCategories {
    data: StashCategory[];
    timestamp: number;
    expiresAt: number;
}

interface CachedNotes {
    data: StashNote[];
    categoryId: string;
    timestamp: number;
    expiresAt: number;
}

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes
const CATEGORIES_KEY = 'stash_categories';
const NOTES_KEY_PREFIX = 'stash_notes_';

export const StashCache = {
    // Categories
    getCategories(): StashCategory[] | null {
        try {
            const cached = localStorage.getItem(CATEGORIES_KEY);
            if (!cached) return null;
            
            const data: CachedCategories = JSON.parse(cached);
            
            // Check expiration
            if (Date.now() > data.expiresAt) {
                this.clearCategories();
                return null;
            }
            
            return data.data;
        } catch (error) {
            console.error('Error reading categories cache:', error);
            return null;
        }
    },
    
    setCategories(categories: StashCategory[]): void {
        try {
            const cached: CachedCategories = {
                data: categories,
                timestamp: Date.now(),
                expiresAt: Date.now() + CACHE_TTL
            };
            localStorage.setItem(CATEGORIES_KEY, JSON.stringify(cached));
        } catch (error) {
            console.error('Error setting categories cache:', error);
        }
    },
    
    clearCategories(): void {
        localStorage.removeItem(CATEGORIES_KEY);
    },
    
    // Notes
    getNotes(categoryId: string): StashNote[] | null {
        try {
            const key = `${NOTES_KEY_PREFIX}${categoryId}`;
            const cached = localStorage.getItem(key);
            if (!cached) return null;
            
            const data: CachedNotes = JSON.parse(cached);
            
            // Check expiration
            if (Date.now() > data.expiresAt) {
                this.clearNotes(categoryId);
                return null;
            }
            
            return data.data;
        } catch (error) {
            console.error('Error reading notes cache:', error);
            return null;
        }
    },
    
    setNotes(categoryId: string, notes: StashNote[]): void {
        try {
            const key = `${NOTES_KEY_PREFIX}${categoryId}`;
            const cached: CachedNotes = {
                data: notes,
                categoryId,
                timestamp: Date.now(),
                expiresAt: Date.now() + CACHE_TTL
            };
            localStorage.setItem(key, JSON.stringify(cached));
        } catch (error) {
            console.error('Error setting notes cache:', error);
        }
    },
    
    clearNotes(categoryId: string): void {
        const key = `${NOTES_KEY_PREFIX}${categoryId}`;
        localStorage.removeItem(key);
    },
    
    clearAllNotes(): void {
        // Remove all notes from localStorage
        const keysToRemove: string[] = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key?.startsWith(NOTES_KEY_PREFIX)) {
                keysToRemove.push(key);
            }
        }
        keysToRemove.forEach(key => localStorage.removeItem(key));
    },
    
    clearAll(): void {
        this.clearCategories();
        this.clearAllNotes();
    }
};
```

### 2. Update StashApi to Use Cache

Modify `wwwroot/scripts/stashApi.ts`:

```typescript
import { StashCache } from './stashCache';

export const StashApi = {
    // Categories
    getCategories: async (): Promise<BackEndResult<StashCategory[]>> => {
        // Check cache first
        const cached = StashCache.getCategories();
        if (cached) {
            return {
                theResult: cached,
                code: 200,
                conflictCode: 'NoConflict',
                errorMessage: '',
                isOk: true
            };
        }
        
        // Cache miss - fetch from API
        try {
            const response = await fetch('/stash/categories', {
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<StashCategory[]>(response);
            
            // Cache the result if successful
            if (result.isOk && result.theResult) {
                StashCache.setCategories(result.theResult);
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory[]>(error);
        }
    },
    
    createCategory: async (request: CreateCategoryRequest): Promise<BackEndResult<StashCategory>> => {
        try {
            const response = await fetch('/stash/category', {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                body: JSON.stringify(request)
            });
            const result = await handleResponse<StashCategory>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory>(error);
        }
    },
    
    updateCategory: async (categoryId: string, request: UpdateCategoryRequest): Promise<BackEndResult<StashCategory>> => {
        try {
            const response = await fetch(`/stash/category/${categoryId}`, {
                method: 'PUT',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                body: JSON.stringify(request)
            });
            const result = await handleResponse<StashCategory>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
            }
            
            return result;
        } catch (error) {
            return handleError<StashCategory>(error);
        }
    },
    
    deleteCategory: async (categoryId: string): Promise<BackEndResult<void>> => {
        try {
            const response = await fetch(`/stash/category/${categoryId}`, {
                method: 'DELETE',
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<void>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories();
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<void>(error);
        }
    },
    
    // Notes
    getNotes: async (categoryId: string): Promise<BackEndResult<StashNote[]>> => {
        // Check cache first
        const cached = StashCache.getNotes(categoryId);
        if (cached) {
            return {
                theResult: cached,
                code: 200,
                conflictCode: 'NoConflict',
                errorMessage: '',
                isOk: true
            };
        }
        
        // Cache miss - fetch from API
        try {
            const response = await fetch(`/stash/categories/${categoryId}`, {
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<StashNote[]>(response);
            
            // Cache the result if successful
            if (result.isOk && result.theResult) {
                StashCache.setNotes(categoryId, result.theResult);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote[]>(error);
        }
    },
    
    createNote: async (categoryId: string, request: CreateNoteRequest): Promise<BackEndResult<StashNote>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                body: JSON.stringify(request)
            });
            const result = await handleResponse<StashNote>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories(); // Note count changed
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote>(error);
        }
    },
    
    updateNote: async (categoryId: string, noteId: string, request: UpdateNoteRequest): Promise<BackEndResult<StashNote>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}/note/${noteId}`, {
                method: 'PUT',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Dendron-API-Call': 'true'
                },
                body: JSON.stringify(request)
            });
            const result = await handleResponse<StashNote>(response);
            
            // Invalidate notes cache on success
            if (result.isOk) {
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<StashNote>(error);
        }
    },
    
    deleteNote: async (categoryId: string, noteId: string): Promise<BackEndResult<void>> => {
        try {
            const response = await fetch(`/stash/categories/${categoryId}/note/${noteId}`, {
                method: 'DELETE',
                credentials: 'include',
                headers: { 'X-Dendron-API-Call': 'true' }
            });
            const result = await handleResponse<void>(response);
            
            // Invalidate cache on success
            if (result.isOk) {
                StashCache.clearCategories(); // Note count changed
                StashCache.clearNotes(categoryId);
            }
            
            return result;
        } catch (error) {
            return handleError<void>(error);
        }
    }
};
```

---

## Cache Invalidation Strategy

### Backend

**When to Invalidate:**
- **Categories cache:** On any category modification (create, update, delete) or note count change (create/delete note)
- **Notes cache:** On any note modification within that category (create, update, delete)

**Automatic Invalidation:**
- Session timeout (ASP.NET default: 20 minutes)
- Session cleanup on logout

### Frontend

**When to Invalidate:**
- **Automatic expiration:** 5 minutes TTL (configurable)
- **Manual invalidation:** After any mutation operation (create, update, delete)
- **Force refresh:** User-triggered refresh button (optional)

**Cache Clear Triggers:**
- Logout
- Session expired (401 response)
- Repository change

---

## Consistency Guarantees

### Read-After-Write Consistency

After any write operation (create, update, delete):
1. Backend invalidates its session cache
2. Frontend invalidates its localStorage cache
3. Next read will fetch fresh data from GitHub

### Optimistic Hash Checking

For GET requests, backend can optionally check if GitHub file SHA matches cached SHA:
- If match: serve from cache (fast)
- If mismatch: fetch from GitHub and update cache

### Conflict Resolution

Using the existing note ID format (`index:hash`):
- Hash mismatch on update → 409 Conflict
- Frontend handles 409 by showing conflict dialog
- User chooses: view current, overwrite, or cancel

---

## Performance Considerations

### Cache Hit Rates

**Expected scenarios:**
- Category list: High hit rate (rarely changes)
- Notes within category: Medium hit rate (edited occasionally)
- Individual note: Low hit rate (edited frequently)

### Storage Limits

**LocalStorage:**
- Typical limit: 5-10 MB per origin
- Mitigation: Set TTL, clean up expired entries
- Monitor storage usage

**ASP.NET Session:**
- Configurable in `appsettings.json`
- Default: In-memory (cleared on app restart)
- Consider Redis for production/scaling

### Cache Size Management

Implement cleanup strategies:
```typescript
// Remove expired entries periodically
setInterval(() => {
    StashCache.cleanupExpired();
}, 60000); // Every minute
```

---

## Testing Strategy

### Backend Tests

1. **Cache Hit Tests:**
   - Verify cache returns data without GitHub call
   - Verify cache miss triggers GitHub call

2. **Cache Invalidation Tests:**
   - Verify invalidation after mutations
   - Verify cache survives read operations

3. **Hash Verification Tests:**
   - Verify stale cache detection
   - Verify fresh cache validation

### Frontend Tests

1. **Cache Storage Tests:**
   - Verify data persists in localStorage
   - Verify expiration logic

2. **Cache Invalidation Tests:**
   - Verify cache cleared after mutations
   - Verify force refresh

3. **Offline Behavior:**
   - Verify cache serves data when offline
   - Verify graceful degradation

---

## Monitoring & Debugging

### Backend

Add logging:
```csharp
_logger.LogInformation("Stash cache hit: {CacheKey}", cacheKey);
_logger.LogInformation("Stash cache miss: {CacheKey}, fetching from GitHub", cacheKey);
```

### Frontend

Add debug mode:
```typescript
const DEBUG_CACHE = localStorage.getItem('debug_stash_cache') === 'true';

if (DEBUG_CACHE) {
    console.log('Cache hit:', key, data);
}
```

---

## Configuration

### Backend (`appsettings.json`)

```json
{
  "Session": {
    "IdleTimeout": "00:20:00",
    "Cookie": {
      "IsEssential": true
    }
  },
  "StashCache": {
    "EnableCaching": true,
    "EnableHashValidation": true
  }
}
```

### Frontend

```typescript
// In stashCache.ts
export const CACHE_CONFIG = {
    ENABLED: true,
    TTL: 5 * 60 * 1000, // 5 minutes
    DEBUG: false
};
```

---

## Migration Path

### Phase 1: Backend Caching
1. Add session cache extension methods
2. Update StashController to use cache
3. Add file hash support
4. Test thoroughly

### Phase 2: Frontend Caching
1. Create stashCache.ts
2. Update stashApi.ts to use cache
3. Test cache invalidation
4. Monitor performance

### Phase 3: Optimization
1. Add hash validation
2. Fine-tune TTL values
3. Add monitoring/metrics
4. Consider Redis for session storage

---

## Security Considerations

1. **Session Security:**
   - Ensure session cookies are HttpOnly and Secure
   - Use SameSite=Strict
   - Regenerate session ID on login

2. **Cache Poisoning:**
   - Validate all data before caching
   - Sanitize markdown content
   - Use HTTPS only

3. **Privacy:**
   - Clear cache on logout
   - Don't cache sensitive data beyond session

---

## Summary

This caching strategy provides:
- ✅ **Performance:** Minimize GitHub API calls
- ✅ **Consistency:** Proper invalidation on mutations
- ✅ **Reliability:** Fallback to API on cache miss
- ✅ **Scalability:** Two-tier caching (frontend + backend)
- ✅ **User Experience:** Fast page loads, offline support

**Next Steps:**
1. Review and approve this plan
2. Implement backend session caching
3. Implement frontend localStorage caching
4. Test end-to-end
5. Monitor and optimize
