using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BackEnd;
using Xunit;

namespace Tests
{
    public class StashControllerIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly FsStashNotesService _service;

        public StashControllerIntegrationTests()
        {
            // Create a temporary directory for test files
            _testDirectory = Path.Combine(Path.GetTempPath(), "StashTests_" + Guid.NewGuid().ToString());
            var stashRepo = Path.Combine(_testDirectory, "stashRepo");
            Directory.CreateDirectory(_testDirectory);
            Directory.CreateDirectory(stashRepo);
            
            _service = new FsStashNotesService(_testDirectory);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task GetCategories_ShouldReturnEmptyList_WhenNoCategories()
        {
            _service.SetRepository("stashRepo",100);
            // Act
            var categories = await _service.GetCategoriesAsync();

            // Assert
            Assert.Empty(categories);
        }

        [Fact]
        public async Task CreateCategory_ShouldCreateNewCategory()
        {
            _service.SetRepository("stashRepo",100);
            // Act
            var category = await _service.CreateCategoryAsync("Test Category", "Test Description");

            // Assert
            Assert.NotNull(category);
            Assert.Equal("Test Category", category.Title);
            Assert.Equal("Test Description", category.Description);
            Assert.Equal(0, category.NotesCount);
        }

        [Fact]
        public async Task CreateCategory_ShouldThrow_WhenTitleIsEmpty()
        {
            _service.SetRepository("stashRepo",100);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateCategoryAsync("", "Description"));
        }

        [Fact]
        public async Task GetCategories_ShouldReturnCreatedCategories()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            await _service.CreateCategoryAsync("Category 1", "Desc 1");
            await _service.CreateCategoryAsync("Category 2", "Desc 2");

            // Act
            var categories = await _service.GetCategoriesAsync();

            // Assert
            Assert.Equal(2, categories.Count);
            Assert.Contains(categories, c => c.Title == "Category 1");
            Assert.Contains(categories, c => c.Title == "Category 2");
        }

        [Fact]
        public async Task UpdateCategory_ShouldUpdateTitle()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Original Title", "Description");

            // Act
            var updated = await _service.UpdateCategoryAsync(category.Id, "New Title", null);

            // Assert
            Assert.Equal("New Title", updated.Title);
            Assert.Equal("Description", updated.Description);
        }

        [Fact]
        public async Task UpdateCategory_ShouldUpdateDescription()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Title", "Original Desc");

            // Act
            var updated = await _service.UpdateCategoryAsync(category.Id, null, "New Desc");

            // Assert
            Assert.Equal("Title", updated.Title);
            Assert.Equal("New Desc", updated.Description);
        }

        [Fact]
        public async Task GetNotes_ShouldReturnEmptyList_WhenNoNotes()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");

            // Act
            var notes = await _service.GetNotesAsync(category.Id);

            // Assert
            Assert.Empty(notes);
        }

        [Fact]
        public async Task AddNote_ShouldAddNoteToCategory()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");

            // Act
            var note = await _service.AddNoteAsync(category.Id, "# Test Note\n\nThis is a test note.");

            // Assert
            Assert.NotNull(note);
            Assert.Equal("Test Note", note.Title);
            Assert.Contains("This is a test note", note.Content);
            Assert.Contains(":", note.Id); // Should have format index:hash
        }

        [Fact]
        public async Task AddNote_ShouldThrow_WhenContentIsEmpty()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.AddNoteAsync(category.Id, ""));
        }

        [Fact]
        public async Task GetNotes_ShouldReturnAllNotes()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");
            await _service.AddNoteAsync(category.Id, "# Note 1\n\nFirst note");
            await _service.AddNoteAsync(category.Id, "Note 2 without title");
            await _service.AddNoteAsync(category.Id, "# Note 3\n\nThird note");

            // Act
            var notes = await _service.GetNotesAsync(category.Id);

            // Assert
            Assert.Equal(3, notes.Count);
            Assert.Equal("Note 1", notes[0].Title);
            Assert.Null(notes[1].Title);
            Assert.Equal("Note 3", notes[2].Title);
        }

        [Fact]
        public async Task UpdateNote_ShouldUpdateNoteContent()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            _service.SetRepository("stashRepo",100);
            var category = await _service.CreateCategoryAsync("Test Category", "Description");
            var note = await _service.AddNoteAsync(category.Id, "Original content");

            // Act
            var result = await _service.UpdateNoteAsync(category.Id, note.Id, "Updated content");

            // Assert
            Assert.True(result.IsOk);
            Assert.Equal("Updated content", result.TheResult.Content);
            Assert.NotEqual(note.Id, result.TheResult.Id); // Hash should change
        }

        [Fact]
        public async Task UpdateNote_ShouldReturnConflict_WhenHashMismatch()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");
            var note = await _service.AddNoteAsync(category.Id, "Original content");
            
            // Simulate concurrent update by using old note ID
            await _service.UpdateNoteAsync(category.Id, note.Id, "First update");

            // Act - Try to update with old note ID (hash mismatch)
            var result = await _service.UpdateNoteAsync(category.Id, note.Id, "Second update");

            // Assert
            Assert.False(result.IsOk);
            Assert.Equal(ResultCode.Conflict, result.Code);
            Assert.Equal(ConflictCode.Modified, result.ConflictCode);
        }

        [Fact]
        public async Task DeleteNote_ShouldRemoveNote()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");
            var note1 = await _service.AddNoteAsync(category.Id, "Note 1");
            var note2 = await _service.AddNoteAsync(category.Id, "Note 2");
            var note3 = await _service.AddNoteAsync(category.Id, "Note 3");

            // Act
            var result = await _service.DeleteNoteAsync(category.Id, note2.Id);

            // Assert
            Assert.True(result.IsOk);
            
            var notes = await _service.GetNotesAsync(category.Id);
            Assert.Equal(2, notes.Count);
            Assert.Equal("Note 1", notes[0].Content);
            Assert.Equal("Note 3", notes[1].Content);
        }

        [Fact]
        public async Task DeleteNote_ShouldReturnConflict_WhenHashMismatch()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");
            var note = await _service.AddNoteAsync(category.Id, "Original content");
            
            // Simulate concurrent update
            await _service.UpdateNoteAsync(category.Id, note.Id, "Updated content");

            // Act - Try to delete with old note ID (hash mismatch)
            var result = await _service.DeleteNoteAsync(category.Id, note.Id);

            // Assert
            Assert.False(result.IsOk);
            Assert.Equal(ResultCode.Conflict, result.Code);
        }

        [Fact]
        public async Task CategoryExists_ShouldReturnTrue_WhenCategoryExists()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");

            // Act
            var exists = await _service.CategoryExistsAsync(category.Id);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task CategoryExists_ShouldReturnFalse_WhenCategoryDoesNotExist()
        {
            _service.SetRepository("stashRepo",100);
            // Act
            var exists = await _service.CategoryExistsAsync("non-existent-id");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task UpdateCategory_ShouldThrow_WhenCategoryNotFound()
        {
            _service.SetRepository("stashRepo",100);
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _service.UpdateCategoryAsync("non-existent-id", "Title", "Desc"));
        }

        [Fact]
        public async Task GetNotes_ShouldThrow_WhenCategoryNotFound()
        {
            _service.SetRepository("stashRepo",100);
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _service.GetNotesAsync("non-existent-id"));
        }

        [Fact]
        public async Task AddNote_ShouldThrow_WhenCategoryNotFound()
        {
            _service.SetRepository("stashRepo",100);
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _service.AddNoteAsync("non-existent-id", "Content"));
        }

        [Fact]
        public async Task UpdateNote_ShouldThrow_WhenInvalidNoteId()
        {
            _service.SetRepository("stashRepo",100);
            // Arrange
            var category = await _service.CreateCategoryAsync("Test Category", "Description");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.UpdateNoteAsync(category.Id, "invalid-format", "Content"));
        }

        [Fact]
        public async Task CategoryIdValidation_ShouldThrow_WhenPathTraversal()
        {
            _service.SetRepository("stashRepo",100);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GetNotesAsync("../etc/passwd"));
        }

        [Fact]
        public async Task FullWorkflow_ShouldWork()
        {
            _service.SetRepository("stashRepo",100);
            // 1. Create category
            var category = await _service.CreateCategoryAsync("My Notes", "Personal notes");
            
            // 2. Add notes
            var note1 = await _service.AddNoteAsync(category.Id, "# Shopping List\n\n- Milk\n- Eggs");
            var note2 = await _service.AddNoteAsync(category.Id, "Remember to call dentist");
            
            // 3. Get all notes
            var notes = await _service.GetNotesAsync(category.Id);
            Assert.Equal(2, notes.Count);
            
            // 4. Update a note
            var updated = await _service.UpdateNoteAsync(category.Id, note1.Id, "# Shopping List\n\n- Milk\n- Eggs\n- Bread");
            Assert.True(updated.IsOk);
            
            // 5. Delete a note
            var deleted = await _service.DeleteNoteAsync(category.Id, note2.Id);
            Assert.True(deleted.IsOk);
            
            // 6. Verify final state
            notes = await _service.GetNotesAsync(category.Id);
            Assert.Single(notes);
            Assert.Contains("Bread", notes[0].Content);
            
            // 7. Update category
            var updatedCat = await _service.UpdateCategoryAsync(category.Id, "My Updated Notes", null);
            Assert.Equal("My Updated Notes", updatedCat.Title);
            
            // 8. Verify in category list
            var categories = await _service.GetCategoriesAsync();
            Assert.Single(categories);
            Assert.Equal(1, categories[0].NotesCount);
        }
    }
}
