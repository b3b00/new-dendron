using System;
using System.Linq;
using BackEnd;
using Xunit;

namespace Tests
{
    public class StashCategoryTests
    {
        private const string SampleMarkdown = @"---
id: test-category-001
title: Quick Notes
desc: Collection of quick notes and reminders
updated: 1738108800
created: 1738022400
---

# First Note Title

This is the content of the first note.

______________

This is a second note without a title.

______________

# Another Note

With more content.";

        [Fact]
        public void ParseFromMarkdown_ShouldParseValidMarkdown()
        {
            // Act
            var category = StashCategory.ParseFromMarkdown(SampleMarkdown);

            // Assert
            Assert.Equal("test-category-001", category.Id);
            Assert.Equal("Quick Notes", category.Title);
            Assert.Equal("Collection of quick notes and reminders", category.Description);
            Assert.Equal(1738108800, category.Updated);
            Assert.Equal(1738022400, category.Created);
            Assert.Equal(3, category.Notes.Count);
        }

        [Fact]
        public void ParseFromMarkdown_ShouldExtractNoteTitles()
        {
            // Act
            var category = StashCategory.ParseFromMarkdown(SampleMarkdown);

            // Assert
            Assert.Equal("First Note Title", category.Notes[0].Title);
            Assert.Null(category.Notes[1].Title);
            Assert.Equal("Another Note", category.Notes[2].Title);
        }

        [Fact]
        public void ParseFromMarkdown_ShouldSetNoteIndexes()
        {
            // Act
            var category = StashCategory.ParseFromMarkdown(SampleMarkdown);

            // Assert
            Assert.Equal(0, category.Notes[0].Index);
            Assert.Equal(1, category.Notes[1].Index);
            Assert.Equal(2, category.Notes[2].Index);
        }

        [Fact]
        public void ParseFromMarkdown_ShouldTrimNoteContent()
        {
            // Act
            var category = StashCategory.ParseFromMarkdown(SampleMarkdown);

            // Assert
            Assert.DoesNotContain(category.Notes[0].Content, c => c.ToString() == "\r\n\r\n" || c.ToString() == "\n\n");
            Assert.StartsWith("# First Note Title", category.Notes[0].Content);
        }

        [Fact]
        public void ParseFromMarkdown_ShouldThrowOnMissingFrontMatter()
        {
            // Arrange
            var invalidMarkdown = "# Just some content without front matter";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                StashCategory.ParseFromMarkdown(invalidMarkdown));
        }

        [Fact]
        public void ParseFromMarkdown_ShouldHandleEmptyNotes()
        {
            // Arrange
            var markdownWithEmpty = @"---
id: test-001
title: Test
updated: 1000
created: 999
---

First note

______________

______________

Second note";

            // Act
            var category = StashCategory.ParseFromMarkdown(markdownWithEmpty);

            // Assert - Empty sections should be skipped
            Assert.Equal(2, category.Notes.Count);
        }

        [Fact]
        public void ToMarkdown_ShouldGenerateValidMarkdown()
        {
            // Arrange
            var category = new StashCategory
            {
                Id = "test-123",
                Title = "Test Category",
                Description = "Test Description",
                Updated = 2000,
                Created = 1000,
                Notes = new System.Collections.Generic.List<StashNote>
                {
                    new StashNote
                    {
                        Content = "# First Note\n\nContent here",
                        Index = 0,
                        Title = "First Note"
                    },
                    new StashNote
                    {
                        Content = "Second note content",
                        Index = 1,
                        Title = null
                    }
                }
            };

            // Act
            var markdown = category.ToMarkdown();

            // Assert
            Assert.Contains("id: test-123", markdown);
            Assert.Contains("title: Test Category", markdown);
            Assert.Contains("desc: Test Description", markdown);
            Assert.Contains("updated: 2000", markdown);
            Assert.Contains("created: 1000", markdown);
            Assert.Contains("# First Note", markdown);
            Assert.Contains("Second note content", markdown);
            Assert.Contains("______________", markdown);
        }

        [Fact]
        public void ToMarkdown_ShouldNotIncludeDescIfEmpty()
        {
            // Arrange
            var category = new StashCategory
            {
                Id = "test-123",
                Title = "Test Category",
                Description = "",
                Updated = 2000,
                Created = 1000,
                Notes = new System.Collections.Generic.List<StashNote>()
            };

            // Act
            var markdown = category.ToMarkdown();

            // Assert
            Assert.DoesNotContain("desc:", markdown);
        }

        [Fact]
        public void RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var original = StashCategory.ParseFromMarkdown(SampleMarkdown);

            // Act
            var markdown = original.ToMarkdown();
            var parsed = StashCategory.ParseFromMarkdown(markdown);

            // Assert
            Assert.Equal(original.Id, parsed.Id);
            Assert.Equal(original.Title, parsed.Title);
            Assert.Equal(original.Description, parsed.Description);
            Assert.Equal(original.Updated, parsed.Updated);
            Assert.Equal(original.Created, parsed.Created);
            Assert.Equal(original.Notes.Count, parsed.Notes.Count);
            
            for (int i = 0; i < original.Notes.Count; i++)
            {
                Assert.Equal(original.Notes[i].Title, parsed.Notes[i].Title);
                Assert.Equal(original.Notes[i].Content.Trim(), parsed.Notes[i].Content.Trim());
            }
        }
    }
}
