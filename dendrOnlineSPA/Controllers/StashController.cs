using Microsoft.AspNetCore.Mvc;
using dendrOnlinSPA.model;
using GitHubOAuthMiddleWare;
using Octokit;
using BackEnd;

namespace dendrOnlineSPA.Controllers;

[ApiController]
[Route("stash")]
public class StashController : DendronController
{
    public StashController(ILogger<RepositoryController> logger, IConfiguration configuration,
        INotesService notesService, IStashNotesService stashService) : base(logger, configuration, notesService, stashService)
    {        
    }

    

    /// <summary>
    /// GET /stash/categories - Returns a list of available categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<BackEnd.CategoryListItemDto>>> GetCategories()
    {
        // Check cache first
        var cached = HttpContext.GetCachedCategories();
        if (cached != null)
        {
            return Ok(cached);
        }
        
        // Cache miss - fetch from service
        var categories = await StashService.GetCategoriesAsync();
        
        // Store in cache
        HttpContext.SetCachedCategories(categories);
        
        return Ok(categories);
    }

    /// <summary>
    /// GET /stash/categories/with-notes - Returns all categories with their notes in one request
    /// </summary>
    [HttpGet("categories/with-notes")]
    public async Task<ActionResult<List<BackEnd.CategoryWithNotesDto>>> GetCategoriesWithNotes([FromQuery] bool force = false)
    {
        try
        {
            Logger.LogInformation("GetCategoriesWithNotes called with force={Force}", force);
            
            // Check if we have cached categories (skip if force=true)
            var cachedCategories = force ? null : HttpContext.GetCachedCategories();
            
            // If we have all categories cached with their notes, return them
            if (cachedCategories != null)
            {
                Logger.LogInformation("Found {Count} cached categories", cachedCategories.Count);
                var allCached = true;
                var result = new List<BackEnd.CategoryWithNotesDto>();
                
                foreach (var category in cachedCategories)
                {
                    var cachedNotes = HttpContext.GetCachedNotes(category.Id);
                    if (cachedNotes == null)
                    {
                        allCached = false;
                        break;
                    }
                    result.Add(new BackEnd.CategoryWithNotesDto
                    {
                        Category = category,
                        Notes = cachedNotes
                    });
                }
                
                if (allCached)
                {
                    Logger.LogInformation("Returning {Count} categories with notes from cache", result.Count);
                    return Ok(result);
                }
            }
            
            // Cache miss - fetch all data
            Logger.LogInformation("Cache miss - fetching categories from service");
            var categories = await StashService.GetCategoriesAsync();
            Logger.LogInformation("Got {Count} categories from service", categories.Count);
            
            var categoriesWithNotes = new List<BackEnd.CategoryWithNotesDto>();
            
            foreach (var category in categories)
            {
                var notes = await StashService.GetNotesAsync(category.Id);
                var fileHash = await StashService.GetFileHashAsync(category.Id);
                
                Logger.LogInformation("Category {Id}: {Count} notes", category.Id, notes.Count);
                
                // Cache each category's notes
                HttpContext.SetCachedNotes(category.Id, notes, fileHash);
                
                categoriesWithNotes.Add(new BackEnd.CategoryWithNotesDto
                {
                    Category = category,
                    Notes = notes
                });
            }
            
            // Cache categories list
            HttpContext.SetCachedCategories(categories);
            
            Logger.LogInformation("Returning {Count} categories with notes", categoriesWithNotes.Count);
            return Ok(categoriesWithNotes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in GetCategoriesWithNotes");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /stash/category - Create a new category
    /// </summary>
    [HttpPost("category")]
    public async Task<ActionResult<BackEnd.CategoryListItemDto>> CreateCategory([FromBody] BackEnd.CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required" });

        try
        {
            var category = await StashService.CreateCategoryAsync(request.Title, request.Description);
            
            // Invalidate categories cache
            HttpContext.InvalidateCategoriesCache();
            
            return CreatedAtAction(nameof(GetNotes), new { categoryId = category.Id }, category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /stash/category/{categoryId} - Update a category
    /// </summary>
    [HttpPut("category/{categoryId}")]
    public async Task<ActionResult<BackEnd.CategoryListItemDto>> UpdateCategory(
        string categoryId, 
        [FromBody] BackEnd.UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && request.Description == null)
            return BadRequest(new { error = "At least one field (title or description) must be provided" });

        try
        {
            var category = await StashService.UpdateCategoryAsync(categoryId, request.Title, request.Description);
            
            // Invalidate categories cache
            HttpContext.InvalidateCategoriesCache();
            
            return Ok(category);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /stash/category/{categoryId} - Delete a category and all its notes
    /// </summary>
    [HttpDelete("category/{categoryId}")]
    public async Task<ActionResult> DeleteCategory(string categoryId)
    {
        try
        {
            var deleted = await StashService.DeleteCategoryAsync(categoryId);
            if (!deleted)
                return NotFound(new { error = $"Category {categoryId} not found" });
            
            // Invalidate both categories and notes cache
            HttpContext.InvalidateCategoriesCache();
            HttpContext.InvalidateNotesCache(categoryId);
            
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /stash/category/{categoryId}/reload - Force reload a category from GitHub and return category + notes
    /// </summary>
    [HttpPost("category/{categoryId}/reload")]
    public async Task<ActionResult<BackEnd.CategoryWithNotesDto>> ReloadCategory(string categoryId)
    {
        try
        {
            Logger.LogInformation("ReloadCategory called for {CategoryId}", categoryId);
            // Invalidate caches first
            HttpContext.InvalidateCategoriesCache();
            HttpContext.InvalidateNotesCache(categoryId);
            Logger.LogInformation("Caches invalidated for {CategoryId}", categoryId);
            
            // Fetch fresh data from GitHub (bypassing cache) - makes single read of category file
            var categories = await StashService.GetCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Id == categoryId);
            
            if (category == null)
            {
                Logger.LogWarning("Category {CategoryId} not found during reload", categoryId);
                return NotFound(new { error = $"Category {categoryId} not found" });
            }
            
            // Read notes from the same file
            var notes = await StashService.GetNotesAsync(categoryId);
            var fileHash = await StashService.GetFileHashAsync(categoryId);
            
            Logger.LogInformation("Reloaded {CategoryId}: {NoteCount} notes, hash={Hash}", categoryId, notes.Count, fileHash);
            
            // Store fresh data in backend cache
            HttpContext.SetCachedCategories(categories);
            HttpContext.SetCachedNotes(categoryId, notes, fileHash);
            
            // Return combined result
            var result = new BackEnd.CategoryWithNotesDto
            {
                Category = category,
                Notes = notes
            };
            
            return Ok(result);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /stash/categories/{categoryId} - Get all notes in a category
    /// </summary>
    [HttpGet("categories/{categoryId}")]
    public async Task<ActionResult<List<BackEnd.NoteDto>>> GetNotes(string categoryId)
    {
        try
        {
            // Check cache first
            var cachedNotes = HttpContext.GetCachedNotes(categoryId);
            if (cachedNotes != null)
            {
                // Optional: Verify file hasn't changed using hash
                var cachedHash = HttpContext.GetCachedFileHash(categoryId);
                var currentHash = await StashService.GetFileHashAsync(categoryId);
                
                if (cachedHash == currentHash)
                {
                    return Ok(cachedNotes);
                }
                // Hash mismatch - cache is stale, continue to fetch
            }
            
            // Cache miss or stale - fetch from service
            var notes = await StashService.GetNotesAsync(categoryId);
            var fileHash = await StashService.GetFileHashAsync(categoryId);
            
            // Store in cache
            HttpContext.SetCachedNotes(categoryId, notes, fileHash);
            
            return Ok(notes);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /stash/categories/{categoryId} - Add a new note to a category
    /// </summary>
    [HttpPost("categories/{categoryId}")]
    public async Task<ActionResult<BackEnd.NoteDto>> AddNote(
        string categoryId, 
        [FromBody] BackEnd.CreateNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        try
        {
            var note = await StashService.AddNoteAsync(categoryId, request.Content);
            
            // Invalidate both caches (category count changed, notes list changed)
            HttpContext.InvalidateCategoriesCache();
            HttpContext.InvalidateNotesCache(categoryId);
            
            return CreatedAtAction(nameof(GetNotes), new { categoryId }, note);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /stash/categories/{categoryId}/note/{noteId} - Update a note
    /// </summary>
    [HttpPut("categories/{categoryId}/note/{noteId}")]
    public async Task<ActionResult<BackEnd.NoteDto>> UpdateNote(
        string categoryId,
        string noteId,
        [FromBody] BackEnd.UpdateNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        try
        {
            var result = await StashService.UpdateNoteAsync(categoryId, noteId, request.Content);
            
            if (!result.IsOk)
            {
                return Conflict(new { 
                    error = result.ErrorMessage,
                    conflictCode = result.ConflictCode.ToString()
                });
            }
            
            // Invalidate notes cache only (category count unchanged)
            HttpContext.InvalidateNotesCache(categoryId);
            
            return Ok(result.TheResult);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("out of range"))
                return NotFound(new { error = "Note not found" });
            
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /stash/categories/{categoryId}/note/{noteId} - Delete a note
    /// </summary>
    [HttpDelete("categories/{categoryId}/note/{noteId}")]
    public async Task<ActionResult> DeleteNote(string categoryId, string noteId)
    {
        try
        {
            var result = await StashService.DeleteNoteAsync(categoryId, noteId);
            
            if (!result.IsOk)
            {
                return Conflict(new { 
                    error = result.ErrorMessage,
                    conflictCode = result.ConflictCode.ToString()
                });
            }
            
            // Invalidate both caches (category count changed, notes list changed)
            HttpContext.InvalidateCategoriesCache();
            HttpContext.InvalidateNotesCache(categoryId);
            
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Category {categoryId} not found" });
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("out of range"))
                return NotFound(new { error = "Note not found" });
            
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /stash/search - Search across all stash categories and notes
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<BackEnd.Note>>> SearchStashes([FromQuery] string pattern, [FromQuery] bool searchInContent = false)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
                return Ok(new List<BackEnd.Note>());

            var categories = await StashService.GetCategoriesAsync();
            var allNotes = new List<BackEnd.Note>();

            foreach (var category in categories)
            {
                var notes = await StashService.GetNotesAsync(category.Id);
                foreach (var noteDto in notes)
                {
                    bool titleMatch = !string.IsNullOrEmpty(noteDto.Title) && noteDto.Title.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                    bool idMatch = !string.IsNullOrEmpty(noteDto.Id) && noteDto.Id.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                    bool contentMatch = false;

                    if (searchInContent && !string.IsNullOrEmpty(noteDto.Content))
                    {
                        contentMatch = noteDto.Content.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                    }

                    if (titleMatch || idMatch || contentMatch)
                    {
                        // Map StashNote to Dendron Note for consistent display in Command Palette
                        var note = new BackEnd.Note
                        {
                            Header = new BackEnd.NoteHeader
                            {
                                Id = noteDto.Id,
                                Name = noteDto.Id, // Stash notes use ID:hash for navigation
                                Title = noteDto.Title ?? "Untitled Note",
                                Description = $"Category: {category.Title}",
                                CategoryId = category.Id,
                                CategoryTitle = category.Title
                            },
                            Body = noteDto.Content
                        };
                        allNotes.Add(note);
                    }
                }
            }

            return Ok(allNotes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in SearchStashes");
            return BadRequest(new { error = ex.Message });
        }
    }
}
