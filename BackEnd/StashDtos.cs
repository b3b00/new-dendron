using System.Collections.Generic;

namespace BackEnd
{
    public class CategoryListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int NotesCount { get; set; }
}

    public class NoteDto
    {
        public string Id { get; set; } = string.Empty;          // Format: "index:hash"
        public string Title { get; set; }                       // null if no title (no #)
        public string Content { get; set; } = string.Empty;      // Full markdown content
    }

    public class CreateNoteRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateNoteRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CreateCategoryRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; }
    }

    public class UpdateCategoryRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class ConflictResult
    {
        public string Error { get; set; } = "Note has been modified";
        public string CurrentNoteId { get; set; } = string.Empty;  // New ID with current hash
        public string CurrentContent { get; set; } = string.Empty;  // Current content
    }

    public class CategoryWithNotesDto
    {
        public CategoryListItemDto Category { get; set; } = new CategoryListItemDto();
        public List<NoteDto> Notes { get; set; } = new List<NoteDto>();
    }
}
