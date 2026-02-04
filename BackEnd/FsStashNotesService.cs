using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BackEnd
{
    public class FsStashNotesService : IStashNotesService
    {
        private string _stashesPath;
        private const int MaxContentSize = 1024 * 1024; // 1 MB

        private List<(string name, long id)> _repositories = new List<(string name, long id)>();

        private string _rootDirectory { get; set; }



        public FsStashNotesService(string repositoryPath)
        {
            _rootDirectory = repositoryPath;

            _repositories = GetRepositories();
        }

        private List<(string name, long id)> GetRepositories()
        {
            var reposDir = new DirectoryInfo(Path.Combine(_rootDirectory));
            var dirs = reposDir.GetDirectories();


            _repositories = new List<(string name, long id)>();
            int i = 100;
            foreach (var r in dirs)
            {                
                _repositories.Add((r.Name, i));
                i++;
            }


            return _repositories;
        }
    
    
    public bool IsFileSystemRepo() => true;

    // These methods are no-op for filesystem service
    public void SetRepository(string name, long id)
        {
            var rep = _repositories.FirstOrDefault(r => r.id == id);

            _stashesPath = Path.Combine(_rootDirectory, rep.name, "stashes");

            // Ensure stashes directory exists
            if (!Directory.Exists(_stashesPath))
            {
                Directory.CreateDirectory(_stashesPath);
            }

        }
    public void SetAccessToken(string token) { }

    public async Task<List<CategoryListItemDto>> GetCategoriesAsync()
    {
        var categories = new List<CategoryListItemDto>();
        
        if (!Directory.Exists(_stashesPath))
            return categories;

        var files = Directory.GetFiles(_stashesPath, "*.md");
        
        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var category = StashCategory.ParseFromMarkdown(content);
                
                categories.Add(new CategoryListItemDto
                {
                    Id = category.Id,
                    Title = category.Title,
                    Description = string.IsNullOrWhiteSpace(category.Description) ? category.Title : category.Description,
                    NotesCount = category.Notes.Count
                });
            }
            catch
            {
                // Skip invalid files
            }
        }
        
        return categories;
    }

    public async Task<CategoryListItemDto> CreateCategoryAsync(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        var categoryId = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var category = new StashCategory
        {
            Id = categoryId,
            Title = title,
            Description = description ?? string.Empty,
            Created = timestamp,
            Updated = timestamp,
            Notes = new List<StashNote>()
        };

        var fileName = SanitizeFileName(title) + ".md";
        var filePath = Path.Combine(_stashesPath, fileName);
        
        // Ensure unique filename
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{SanitizeFileName(title)}_{counter}.md";
            filePath = Path.Combine(_stashesPath, fileName);
            counter++;
        }

        await File.WriteAllTextAsync(filePath, category.ToMarkdown());

        return new CategoryListItemDto
        {
            Id = category.Id,
            Title = category.Title,
            Description = string.IsNullOrWhiteSpace(category.Description) ? category.Title : category.Description,
            NotesCount = 0
        };
    }

    public async Task<CategoryListItemDto> UpdateCategoryAsync(string categoryId, string title, string description)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            throw new FileNotFoundException($"Category {categoryId} not found");

        var content = await File.ReadAllTextAsync(filePath);
        var category = StashCategory.ParseFromMarkdown(content);

        if (!string.IsNullOrWhiteSpace(title))
            category.Title = title;
        
        if (!string.IsNullOrWhiteSpace(description))
            category.Description = description;

        category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await File.WriteAllTextAsync(filePath, category.ToMarkdown());

        return new CategoryListItemDto
        {
            Id = category.Id,
            Title = category.Title,
            Description = string.IsNullOrWhiteSpace(category.Description) ? category.Title : category.Description,
            NotesCount = category.Notes.Count
        };
    }

    public async Task<List<NoteDto>> GetNotesAsync(string categoryId)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            throw new FileNotFoundException($"Category {categoryId} not found");

        var content = await File.ReadAllTextAsync(filePath);
        var category = StashCategory.ParseFromMarkdown(content);

        var notes = new List<NoteDto>();
        for (int i = 0; i < category.Notes.Count; i++)
        {
            var note = category.Notes[i];
            notes.Add(new NoteDto
            {
                Id = NoteIdentifier.GenerateNoteId(i, note.Content),
                Title = note.Title,
                Content = note.Content
            });
        }

        return notes;
    }

    public async Task<NoteDto> AddNoteAsync(string categoryId, string content)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        if (content.Length > MaxContentSize)
            throw new ArgumentException($"Content exceeds maximum size of {MaxContentSize} bytes", nameof(content));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            throw new FileNotFoundException($"Category {categoryId} not found");

        var fileContent = await File.ReadAllTextAsync(filePath);
        var category = StashCategory.ParseFromMarkdown(fileContent);

        var newNote = new StashNote
        {
            Content = content.Trim(),
            Index = category.Notes.Count,
            Title = StashNote.ExtractTitle(content.Trim())
        };

        category.Notes.Add(newNote);
        category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await File.WriteAllTextAsync(filePath, category.ToMarkdown());

        return new NoteDto
        {
            Id = NoteIdentifier.GenerateNoteId(newNote.Index, newNote.Content),
            Title = newNote.Title,
            Content = newNote.Content
        };
    }

    public async Task<Result<NoteDto>> UpdateNoteAsync(string categoryId, string noteId, string content)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        if (content.Length > MaxContentSize)
            throw new ArgumentException($"Content exceeds maximum size of {MaxContentSize} bytes", nameof(content));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            throw new FileNotFoundException($"Category {categoryId} not found");

        var identifier = NoteIdentifier.Parse(noteId);
        var fileContent = await File.ReadAllTextAsync(filePath);
        var category = StashCategory.ParseFromMarkdown(fileContent);

        if (identifier.Index < 0 || identifier.Index >= category.Notes.Count)
            throw new ArgumentException("Note index out of range", nameof(noteId));

        var currentNote = category.Notes[identifier.Index];
        
        // Verify hash to detect concurrent modifications
        if (!NoteIdentifier.VerifyNoteHash(noteId, currentNote.Content, identifier.Index))
        {
            var result = new Result<NoteDto>();
            result.Code = ResultCode.Conflict;
            result.ConflictCode = ConflictCode.Modified;
            result.ErrorMessage = "Note has been modified by another user";
            return result;
        }

        // Update the note
        category.Notes[identifier.Index] = new StashNote
        {
            Content = content.Trim(),
            Index = identifier.Index,
            Title = StashNote.ExtractTitle(content.Trim())
        };

        category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await File.WriteAllTextAsync(filePath, category.ToMarkdown());

        var updatedNote = category.Notes[identifier.Index];
        return new NoteDto
        {
            Id = NoteIdentifier.GenerateNoteId(identifier.Index, updatedNote.Content),
            Title = updatedNote.Title,
            Content = updatedNote.Content
        };
    }

    public async Task<Result<bool>> DeleteNoteAsync(string categoryId, string noteId)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            throw new FileNotFoundException($"Category {categoryId} not found");

        var identifier = NoteIdentifier.Parse(noteId);
        var fileContent = await File.ReadAllTextAsync(filePath);
        var category = StashCategory.ParseFromMarkdown(fileContent);

        if (identifier.Index < 0 || identifier.Index >= category.Notes.Count)
            throw new ArgumentException("Note index out of range", nameof(noteId));

        var currentNote = category.Notes[identifier.Index];
        
        // Verify hash to detect concurrent modifications
        if (!NoteIdentifier.VerifyNoteHash(noteId, currentNote.Content, identifier.Index))
        {
            var result = new Result<bool>();
            result.Code = ResultCode.Conflict;
            result.ConflictCode = ConflictCode.Modified;
            result.ErrorMessage = "Note has been modified by another user";
            return result;
        }

        // Delete the note
        category.Notes.RemoveAt(identifier.Index);
        category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await File.WriteAllTextAsync(filePath, category.ToMarkdown());

        return true;
    }

    public async Task<bool> CategoryExistsAsync(string categoryId)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            return false;

        var filePath = await FindCategoryFileAsync(categoryId);
        return filePath != null;
    }

    public async Task<bool> DeleteCategoryAsync(string categoryId)
    {
        if (!NoteIdentifier.IsValidCategoryId(categoryId))
            throw new ArgumentException("Invalid category ID", nameof(categoryId));

        var filePath = await FindCategoryFileAsync(categoryId);
        if (filePath == null)
            return false;

        File.Delete(filePath);
        return true;
    }

    private async Task<string> FindCategoryFileAsync(string categoryId)
    {
        if (!Directory.Exists(_stashesPath))
            return null;

        var files = Directory.GetFiles(_stashesPath, "*.md");
        
        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var match = Regex.Match(
                    content,
                    @"^---\s*\n.*?id:\s*(\S+)",
                    RegexOptions.Singleline,
                    TimeSpan.FromMilliseconds(500)    
                );
                
                if (match.Success && match.Groups[1].Value == categoryId)
                {
                    return file;
                }
            }
            catch
            {
                // Skip invalid files
            }
        }
        
        return null;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());
        
        return string.IsNullOrWhiteSpace(sanitized) ? "category" : sanitized;
    }

        public async Task<string> GetFileHashAsync(string categoryId)
        {
            var filePath = await FindCategoryFileAsync(categoryId);
            
            if (filePath == null || !File.Exists(filePath))
                throw new FileNotFoundException($"Category file not found: {categoryId}");
            
            // Use file's last write time as a simple hash alternative for filesystem
            var lastWrite = File.GetLastWriteTimeUtc(filePath);
            return lastWrite.Ticks.ToString();
        }
    }
}
