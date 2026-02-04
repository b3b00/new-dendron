using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace BackEnd
{
    public class GithubStashNotesService : IStashNotesService
    {
        private const int MaxContentSize = 1024 * 1024; // 1 MB
        private const string StashesPath = "stashes";
        private GitHubClient _gitHubClient;
        private long _repositoryId;
        private string _repositoryName;

        public GithubStashNotesService()
        {
        }

        public void SetRepository(string name, long id)
        {
            _repositoryId = id;
            _repositoryName = name;
        }

        public void SetAccessToken(string token)
        {
            _gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("dendrOnline"), new Uri("https://github.com/"));
            _gitHubClient.Credentials = new Octokit.Credentials(token);
        }

        private async Task<string> GetFileContentAsync(string path)
        {
            // Fetch the file content individually to ensure we get the full content
            var fileContents = await _gitHubClient.Repository.Content.GetAllContents(_repositoryId, path);
            return fileContents[0].Content;
        }

        public async Task<List<CategoryListItemDto>> GetCategoriesAsync()
        {
            var categories = new List<CategoryListItemDto>();

            try
            {
                var contents = await _gitHubClient.Repository.Content.GetAllContents(_repositoryId, StashesPath);
                var mdFiles = contents.Where(x => x.Name.EndsWith(".md"));

                foreach (var file in mdFiles)
                {
                    try
                    {
                        var content = await GetFileContentAsync(file.Path);
                        var category = StashCategory.ParseFromMarkdown(content);
                        categories.Add(new CategoryListItemDto
                        {
                            Id = category.Id,
                            Title = category.Title,
                            Description = string.IsNullOrWhiteSpace(category.Description) ? category.Title : category.Description,
                            NotesCount = category.Notes.Count
                        });
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"Error parsing stash category file {file.Name}: {e.Message}");
                    }
                }
            }
            catch (NotFoundException)
            {
                // stashes directory doesn't exist yet, return empty list
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
            var filePath = $"{StashesPath}/{fileName}";

            // Ensure unique filename
            int counter = 1;
            while (await FileExistsAsync(filePath))
            {
                fileName = $"{SanitizeFileName(title)}_{counter}.md";
                filePath = $"{StashesPath}/{fileName}";
                counter++;
            }

            var createRequest = new CreateFileRequest(
                $"DendrOnline: Create stash category {title}",
                category.ToMarkdown(),
                "main");

            await _gitHubClient.Repository.Content.CreateFile(_repositoryId, filePath, createRequest);

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

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                throw new Exception($"Category {categoryId} not found");

            var content = await GetFileContentAsync(file.Path);
            var category = StashCategory.ParseFromMarkdown(content);

            if (!string.IsNullOrWhiteSpace(title))
                category.Title = title;

            if (!string.IsNullOrWhiteSpace(description))
                category.Description = description;

            category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var updateRequest = new UpdateFileRequest(
                $"DendrOnline: Update stash category {category.Title}",
                category.ToMarkdown(),
                file.Sha);

            await _gitHubClient.Repository.Content.UpdateFile(_repositoryId, file.Path, updateRequest);

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

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                throw new Exception($"Category {categoryId} not found");

            var content = await GetFileContentAsync(file.Path);
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

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                throw new Exception($"Category {categoryId} not found");

            var fileContent = await GetFileContentAsync(file.Path);
            var category = StashCategory.ParseFromMarkdown(fileContent);

            var newNote = new StashNote
            {
                Content = content.Trim(),
                Index = category.Notes.Count,
                Title = StashNote.ExtractTitle(content.Trim())
            };

            category.Notes.Add(newNote);
            category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var updateRequest = new UpdateFileRequest(
                $"DendrOnline: Add note to {category.Title}",
                category.ToMarkdown(),
                file.Sha);

            await _gitHubClient.Repository.Content.UpdateFile(_repositoryId, file.Path, updateRequest);

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

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                throw new Exception($"Category {categoryId} not found");

            var identifier = NoteIdentifier.Parse(noteId);
            var fileContent = await GetFileContentAsync(file.Path);
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

            var updateRequest = new UpdateFileRequest(
                $"DendrOnline: Update note in {category.Title}",
                category.ToMarkdown(),
                file.Sha);

            await _gitHubClient.Repository.Content.UpdateFile(_repositoryId, file.Path, updateRequest);

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

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                throw new Exception($"Category {categoryId} not found");

            var identifier = NoteIdentifier.Parse(noteId);
            var fileContent = await GetFileContentAsync(file.Path);
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

            var updateRequest = new UpdateFileRequest(
                $"DendrOnline: Delete note from {category.Title}",
                category.ToMarkdown(),
                file.Sha);

            await _gitHubClient.Repository.Content.UpdateFile(_repositoryId, file.Path, updateRequest);

            return true;
        }

        public async Task<bool> CategoryExistsAsync(string categoryId)
        {
            if (!NoteIdentifier.IsValidCategoryId(categoryId))
                return false;

            var file = await FindCategoryFileAsync(categoryId);
            return file != null;
        }

        private async Task<RepositoryContent> FindCategoryFileAsync(string categoryId)
        {
            try
            {
                var contents = await _gitHubClient.Repository.Content.GetAllContents(_repositoryId, StashesPath);
                var mdFiles = contents.Where(x => x.Name.EndsWith(".md"));

                foreach (var file in mdFiles)
                {
                    try
                    {
                        var content = await GetFileContentAsync(file.Path);
                        var match = Regex.Match(
                            content,
                            @"^---\s*\n.*?id:\s*(\S+)",
                            RegexOptions.Singleline
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
            }
            catch (NotFoundException)
            {
                // stashes directory doesn't exist
            }

            return null;
        }

        private async Task<bool> FileExistsAsync(string path)
        {
            try
            {
                await _gitHubClient.Repository.Content.GetAllContents(_repositoryId, path);
                return true;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(string categoryId)
        {
            if (!NoteIdentifier.IsValidCategoryId(categoryId))
                throw new ArgumentException("Invalid category ID", nameof(categoryId));

            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
                return false;

            var deleteRequest = new DeleteFileRequest(
                $"DendrOnline: Delete stash category",
                file.Sha);

            await _gitHubClient.Repository.Content.DeleteFile(_repositoryId, file.Path, deleteRequest);
            return true;
        }

    private static string SanitizeFileName(string fileName)
    {
      var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
      var sanitized = new string(fileName
          .Where(c => !invalidChars.Contains(c))
          .ToArray());

      return string.IsNullOrWhiteSpace(sanitized) ? "category" : sanitized;
    }

        public async Task<string> GetFileHashAsync(string categoryId)
        {
            var file = await FindCategoryFileAsync(categoryId);
            if (file == null)
            {
                throw new System.IO.FileNotFoundException($"Category file not found: {categoryId}");
            }
            return file.Sha; // GitHub's SHA for the file
        }
        
        public bool IsFileSystemRepo() => false;
    }
}
