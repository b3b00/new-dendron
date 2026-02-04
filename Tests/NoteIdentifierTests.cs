using System;
using BackEnd;
using Xunit;

namespace Tests
{
    public class NoteIdentifierTests
    {
        [Fact]
        public void GenerateNoteId_ShouldReturnCorrectFormat()
        {
            // Arrange
            var content = "This is test content";
            var index = 0;

            // Act
            var noteId = NoteIdentifier.GenerateNoteId(index, content);

            // Assert
            Assert.Contains(":", noteId);
            var parts = noteId.Split(':');
            Assert.Equal(2, parts.Length);
            Assert.Equal("0", parts[0]);
            Assert.Equal(8, parts[1].Length); // Hash should be 8 characters
        }

        [Fact]
        public void GenerateNoteId_ShouldBeConsistent()
        {
            // Arrange
            var content = "Test content";
            var index = 5;

            // Act
            var id1 = NoteIdentifier.GenerateNoteId(index, content);
            var id2 = NoteIdentifier.GenerateNoteId(index, content);

            // Assert
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateNoteId_ShouldChangWithDifferentContent()
        {
            // Arrange
            var content1 = "Content A";
            var content2 = "Content B";
            var index = 0;

            // Act
            var id1 = NoteIdentifier.GenerateNoteId(index, content1);
            var id2 = NoteIdentifier.GenerateNoteId(index, content2);

            // Assert
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void VerifyNoteHash_ShouldReturnTrueForMatchingContent()
        {
            // Arrange
            var content = "Test note content";
            var index = 3;
            var noteId = NoteIdentifier.GenerateNoteId(index, content);

            // Act
            var isValid = NoteIdentifier.VerifyNoteHash(noteId, content, index);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyNoteHash_ShouldReturnFalseForModifiedContent()
        {
            // Arrange
            var originalContent = "Original content";
            var modifiedContent = "Modified content";
            var index = 1;
            var noteId = NoteIdentifier.GenerateNoteId(index, originalContent);

            // Act
            var isValid = NoteIdentifier.VerifyNoteHash(noteId, modifiedContent, index);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyNoteHash_ShouldReturnFalseForWrongIndex()
        {
            // Arrange
            var content = "Test content";
            var originalIndex = 2;
            var wrongIndex = 3;
            var noteId = NoteIdentifier.GenerateNoteId(originalIndex, content);

            // Act
            var isValid = NoteIdentifier.VerifyNoteHash(noteId, content, wrongIndex);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Parse_ShouldExtractIndexAndHash()
        {
            // Arrange
            var noteId = "5:a1b2c3d4";

            // Act
            var identifier = NoteIdentifier.Parse(noteId);

            // Assert
            Assert.Equal(5, identifier.Index);
            Assert.Equal("a1b2c3d4", identifier.Hash);
        }

        [Fact]
        public void Parse_ShouldThrowOnInvalidFormat()
        {
            // Arrange
            var invalidId = "invalid-format";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => NoteIdentifier.Parse(invalidId));
        }

        [Fact]
        public void Parse_ShouldThrowOnEmptyString()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => NoteIdentifier.Parse(""));
            Assert.Throws<ArgumentException>(() => NoteIdentifier.Parse(null));
        }

        [Fact]
        public void Parse_ShouldThrowOnNonNumericIndex()
        {
            // Arrange
            var invalidId = "abc:12345678";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => NoteIdentifier.Parse(invalidId));
        }

        [Fact]
        public void Parse_ShouldThrowOnNegativeIndex()
        {
            // Arrange
            var invalidId = "-1:12345678";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => NoteIdentifier.Parse(invalidId));
        }

        [Fact]
        public void IsValidCategoryId_ShouldReturnTrueForValidIds()
        {
            // Assert
            Assert.True(NoteIdentifier.IsValidCategoryId("category-123"));
            Assert.True(NoteIdentifier.IsValidCategoryId("MyCategory"));
            Assert.True(NoteIdentifier.IsValidCategoryId("test_category_01"));
        }

        [Fact]
        public void IsValidCategoryId_ShouldReturnFalseForPathTraversal()
        {
            // Assert
            Assert.False(NoteIdentifier.IsValidCategoryId("../etc/passwd"));
            Assert.False(NoteIdentifier.IsValidCategoryId("..\\windows\\system32"));
            Assert.False(NoteIdentifier.IsValidCategoryId("category/../other"));
        }

        [Fact]
        public void IsValidCategoryId_ShouldReturnFalseForPathSeparators()
        {
            // Assert
            Assert.False(NoteIdentifier.IsValidCategoryId("path/to/category"));
            Assert.False(NoteIdentifier.IsValidCategoryId("path\\to\\category"));
        }

        [Fact]
        public void IsValidCategoryId_ShouldReturnFalseForEmptyOrNull()
        {
            // Assert
            Assert.False(NoteIdentifier.IsValidCategoryId(""));
            Assert.False(NoteIdentifier.IsValidCategoryId(null));
            Assert.False(NoteIdentifier.IsValidCategoryId("   "));
        }

        [Fact]
        public void RoundTrip_ShouldWorkCorrectly()
        {
            // Arrange
            var content = "This is a test note with some content.\nMultiple lines too.";
            var index = 7;

            // Act
            var noteId = NoteIdentifier.GenerateNoteId(index, content);
            var identifier = NoteIdentifier.Parse(noteId);
            var isValid = NoteIdentifier.VerifyNoteHash(noteId, content, index);

            // Assert
            Assert.Equal(index, identifier.Index);
            Assert.True(isValid);
        }
    }
}
