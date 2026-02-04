namespace BackEnd
{
    public class StashNote
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Index { get; set; }
        
        /// <summary>
        /// Extracts title from content if it starts with # (level 1 header)
        /// </summary>
        public static string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
            
        var lines = content.TrimStart().Split('\n');
        if (lines.Length > 0 && lines[0].TrimStart().StartsWith("# "))
        {
            return lines[0].TrimStart().Substring(2).Trim();
        }
        
        return null;
        }
    }
}
