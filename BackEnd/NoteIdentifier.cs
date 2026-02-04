using System;
using System.Security.Cryptography;
using System.Text;

namespace BackEnd
{
    public class NoteIdentifier
{
    public int Index { get; set; }
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Generates a note ID in format "index:hash"
    /// </summary>
    public static string GenerateNoteId(int index, string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        var shortHash = BitConverter.ToString(hashBytes)
            .Replace("-", "")
            .Substring(0, 8)
            .ToLower();

        return $"{index}:{shortHash}";
    }

    /// <summary>
    /// Verifies if the provided note ID matches the current content
    /// </summary>
    public static bool VerifyNoteHash(string noteId, string currentContent, int expectedIndex)
    {
        var parts = noteId.Split(':');
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], out var index)) return false;
        if (index != expectedIndex) return false;
        
        var providedHash = parts[1];
        var currentHash = GenerateNoteId(index, currentContent).Split(':')[1];
        
        return providedHash == currentHash;
    }

    /// <summary>
    /// Parses a note ID string into its components
    /// </summary>
    public static NoteIdentifier Parse(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            throw new ArgumentException("Note ID cannot be empty", nameof(noteId));
            
        var parts = noteId.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid note ID format. Expected 'index:hash'", nameof(noteId));

        if (!int.TryParse(parts[0], out var index))
            throw new ArgumentException("Invalid index in note ID", nameof(noteId));
            
        if (index < 0)
            throw new ArgumentException("Index cannot be negative", nameof(noteId));

        return new NoteIdentifier
        {
            Index = index,
            Hash = parts[1]
        };
    }

    /// <summary>
    /// Validates that a category ID doesn't contain path traversal characters
    /// </summary>
    public static bool IsValidCategoryId(string categoryId)
    {
        return !string.IsNullOrWhiteSpace(categoryId)
               && !categoryId.Contains("..")
               && !categoryId.Contains("/")
               && !categoryId.Contains("\\");
    }
    }
}
