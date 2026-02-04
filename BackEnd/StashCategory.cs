using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace BackEnd
{
    public class StashCategory
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Updated { get; set; }
    public long Created { get; set; }
    public List<StashNote> Notes { get; set; } = new List<StashNote>();

    public const string NoteSeparator = "______________";

    /// <summary>
    /// Parses a stash category markdown file
    /// </summary>
    public static StashCategory ParseFromMarkdown(string fileContent)
    {
        var category = new StashCategory();
        
        // Parse front matter
        var frontMatterMatch = Regex.Match(
            fileContent, 
            @"^---\s*\n(.*?)\n---\s*\n", 
            RegexOptions.Singleline
        );
        
        if (!frontMatterMatch.Success)
            throw new InvalidOperationException("Invalid category file: missing front matter");
            
        var frontMatter = frontMatterMatch.Groups[1].Value;
        var lines = frontMatter.Split('\n');
        
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;
            
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            
            switch (key)
            {
                case "id":
                    category.Id = value;
                    break;
                case "title":
                    category.Title = value;
                    break;
                case "desc":
                    category.Description = value;
                    break;
                case "updated":
                    if (long.TryParse(value, out var updated))
                        category.Updated = updated;
                    break;
                case "created":
                    if (long.TryParse(value, out var created))
                        category.Created = created;
                    break;
            }
        }
        
        // Parse notes (content after front matter)
        var contentAfterFrontMatter = fileContent.Substring(frontMatterMatch.Length);
        
        // Split by any sequence of 3 or more underscores on its own line
        var noteParts = Regex.Split(contentAfterFrontMatter, @"\r?\n_{3,}\r?\n", RegexOptions.Multiline);
        
        int index = 0;
        foreach (var notePart in noteParts)
        {
            var trimmedContent = notePart.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedContent))
            {
                var note = new StashNote
                {
                    Content = trimmedContent,
                    Index = index,
                    Title = StashNote.ExtractTitle(trimmedContent)
                };
                category.Notes.Add(note);
                index++;
            }
        }
        
        return category;
    }

    /// <summary>
    /// Converts category to markdown file format
    /// </summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        
        // Write front matter
        sb.AppendLine("---");
        sb.AppendLine($"id: {Id}");
        sb.AppendLine($"title: {Title}");
        if (!string.IsNullOrWhiteSpace(Description))
            sb.AppendLine($"desc: {Description}");
        sb.AppendLine($"updated: {Updated}");
        sb.AppendLine($"created: {Created}");
        sb.AppendLine("---");
        sb.AppendLine();
        
        // Write notes with separators
        for (int i = 0; i < Notes.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine();
                sb.AppendLine(NoteSeparator);
                sb.AppendLine();
            }
            sb.Append(Notes[i].Content);
        }
        
        return sb.ToString();
    }
    }
}
