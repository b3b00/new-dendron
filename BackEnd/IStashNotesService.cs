using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd
{
    public interface IStashNotesService
{
    /// <summary>
    /// Sets the GitHub repository for the service
    /// </summary>
    void SetRepository(string name, long id);

    /// <summary>
    /// Sets the GitHub access token for the service
    /// </summary>
    void SetAccessToken(string token);

    /// <summary>
    /// Gets all available stash categories
    /// </summary>
    Task<List<CategoryListItemDto>> GetCategoriesAsync();

    /// <summary>
    /// Creates a new category
    /// </summary>
    Task<CategoryListItemDto> CreateCategoryAsync(string title, string description);

    /// <summary>
    /// Updates a category's title and/or description
    /// </summary>
    Task<CategoryListItemDto> UpdateCategoryAsync(string categoryId, string title, string description);

    /// <summary>
    /// Gets all notes in a category
    /// </summary>
    Task<List<NoteDto>> GetNotesAsync(string categoryId);

    /// <summary>
    /// Adds a new note to a category
    /// </summary>
    Task<NoteDto> AddNoteAsync(string categoryId, string content);

    /// <summary>
    /// Updates a note in a category. Returns Result with conflict information if hash conflict detected.
    /// </summary>
    Task<Result<NoteDto>> UpdateNoteAsync(string categoryId, string noteId, string content);

    /// <summary>
    /// Deletes a note from a category. Returns Result with conflict information if hash conflict detected.
    /// </summary>
    Task<Result<bool>> DeleteNoteAsync(string categoryId, string noteId);

    /// <summary>
    /// Deletes a category and all its notes
    /// </summary>
    Task<bool> DeleteCategoryAsync(string categoryId);

    /// <summary>
    /// Checks if a category exists
    /// </summary>
    Task<bool> CategoryExistsAsync(string categoryId);

    /// <summary>
    /// Gets the file hash for a category (GitHub SHA or file timestamp)
    /// </summary>
    Task<string> GetFileHashAsync(string categoryId);

    bool IsFileSystemRepo();
    }
}
