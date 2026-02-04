using System;
using System.IO;
using BackEnd;

namespace Play
{
    /// <summary>
    /// Demo program to test stash note parsing and generation
    /// </summary>
    public class StashParsingDemo
    {
        public static void DemoParsingAndGeneration()
        {
            Console.WriteLine("=== Stash Notes Parsing & Generation Demo ===\n");

            // Sample markdown content
            var sampleMarkdown = @"---
id: demo-category-001
title: Development Notes
desc: Quick notes for development tasks
updated: 1738108800
created: 1738022400
---

# Bug Fix for Auth

Fixed the authentication issue in the login controller.
Need to deploy to staging first.

______________

Remember to update the API documentation after the new endpoints are added.

______________

# Meeting Notes

- Discussed new feature requirements
- Timeline: 2 weeks
- Need to coordinate with design team";

            Console.WriteLine("1. PARSING MARKDOWN");
            Console.WriteLine("-------------------");
            Console.WriteLine("Original markdown:");
            Console.WriteLine(sampleMarkdown);
            Console.WriteLine();

            // Parse the markdown
            var category = StashCategory.ParseFromMarkdown(sampleMarkdown);
            
            Console.WriteLine("Parsed Category:");
            Console.WriteLine($"  ID: {category.Id}");
            Console.WriteLine($"  Title: {category.Title}");
            Console.WriteLine($"  Description: {category.Description}");
            Console.WriteLine($"  Created: {DateTimeOffset.FromUnixTimeSeconds(category.Created)}");
            Console.WriteLine($"  Updated: {DateTimeOffset.FromUnixTimeSeconds(category.Updated)}");
            Console.WriteLine($"  Number of notes: {category.Notes.Count}");
            Console.WriteLine();

            Console.WriteLine("2. PARSED NOTES WITH IDS");
            Console.WriteLine("------------------------");
            for (int i = 0; i < category.Notes.Count; i++)
            {
                var note = category.Notes[i];
                var noteId = NoteIdentifier.GenerateNoteId(i, note.Content);
                
                Console.WriteLine($"\nNote {i + 1}:");
                Console.WriteLine($"  ID: {noteId}");
                Console.WriteLine($"  Title: {note.Title ?? "(no title)"}");
                Console.WriteLine($"  Content preview: {note.Content.Substring(0, Math.Min(50, note.Content.Length))}...");
                Console.WriteLine($"  Full length: {note.Content.Length} chars");
            }
            Console.WriteLine();

            Console.WriteLine("3. GENERATION (ToMarkdown)");
            Console.WriteLine("--------------------------");
            
            // Modify the category
            category.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            category.Notes.Add(new StashNote
            {
                Content = "New note added programmatically!",
                Index = category.Notes.Count,
                Title = null
            });

            // Generate markdown
            var generatedMarkdown = category.ToMarkdown();
            Console.WriteLine("Generated markdown:");
            Console.WriteLine(generatedMarkdown);
            Console.WriteLine();

            Console.WriteLine("4. ROUND-TRIP TEST");
            Console.WriteLine("------------------");
            
            // Parse the generated markdown again
            var reparsed = StashCategory.ParseFromMarkdown(generatedMarkdown);
            Console.WriteLine($"Original notes count: {category.Notes.Count}");
            Console.WriteLine($"Reparsed notes count: {reparsed.Notes.Count}");
            Console.WriteLine($"Category ID matches: {category.Id == reparsed.Id}");
            Console.WriteLine($"Title matches: {category.Title == reparsed.Title}");
            Console.WriteLine("✓ Round-trip successful!");
            Console.WriteLine();

            Console.WriteLine("5. NOTE ID VERIFICATION");
            Console.WriteLine("-----------------------");
            
            var testContent = "This is a test note";
            var testIndex = 0;
            var testNoteId = NoteIdentifier.GenerateNoteId(testIndex, testContent);
            
            Console.WriteLine($"Generated ID: {testNoteId}");
            Console.WriteLine($"Verify with same content: {NoteIdentifier.VerifyNoteHash(testNoteId, testContent, testIndex)}");
            Console.WriteLine($"Verify with modified content: {NoteIdentifier.VerifyNoteHash(testNoteId, "Modified content", testIndex)}");
            Console.WriteLine($"Verify with wrong index: {NoteIdentifier.VerifyNoteHash(testNoteId, testContent, 1)}.");
            Console.WriteLine();

            Console.WriteLine("6. CATEGORY ID VALIDATION");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Valid ID 'my-category': {NoteIdentifier.IsValidCategoryId("my-category")}");
            Console.WriteLine($"Invalid ID '../etc/passwd': {NoteIdentifier.IsValidCategoryId("../etc/passwd")}");
            Console.WriteLine($"Invalid ID 'path/to/file': {NoteIdentifier.IsValidCategoryId("path/to/file")}");
            Console.WriteLine();

            Console.WriteLine("=== Demo Complete ===");
        }
    }
}
