using System.Text.Json;

namespace dendrOnlinSPA.model;

public static class Extensions
{
        public static string GetRepositoryName(this HttpContext httpContext)
        {
            return httpContext.Session.GetString("repositoryName");
        }
        
        public static long GetRepositoryId(this HttpContext httpContext)
        {
            bool parsed = long.TryParse(httpContext.Session.GetString("repositoryId"), out long id);
            if (parsed)
            {
                return id;
            }

            return -1;
        }

        public static void SetRepositoryId(this HttpContext httpContext, long repositoryId)
        {
            httpContext.Session.SetString("repositoryId",repositoryId.ToString());
        }
        
        public static string GetUserName(this HttpContext httpContext)
        {
            return httpContext.Session.GetString("userName");
        }
        
        public static string GetCurrentNote(this HttpContext httpContext)
        {
            return httpContext.Session.GetString("currentNote");
        }

        public static void SetCurrentNote(this HttpContext httpContext, string currentNote)
        {
            httpContext.Session.SetString("currentNote",currentNote);
        }
        
        public static long GetUserId(this HttpContext httpContext)
        {
            var val = httpContext.Session.GetString("userId");
            if (!string.IsNullOrEmpty(val))
            {
                bool parsed = long.TryParse(val, out long id);
                if (parsed)
                {
                    return id;
                }
                return -1;
            }
            return -1;
        }
        
        public static Dictionary<string,string> GetEditedNotes(this HttpContext httpContext)
        {
            var editedNotes = httpContext.Session.GetObject<Dictionary<string, string>>("editedNotes");
            return editedNotes ?? new Dictionary<string, string>();
        }
        
        public static void SetEditedNotes(this HttpContext httpContext, Dictionary<string,string> editedNotes)
        {
            httpContext.Session.SetObject<Dictionary<string, string>>("editedNotes",editedNotes);
        }
        
        public static T? GetObject<T>(this ISession session, string key) {
            var rawData = session.GetString(key);
            if (!string.IsNullOrEmpty(rawData))
            {
                var data = JsonSerializer.Deserialize<T>(rawData);
                return data;
            }

            return default;
        }
        
        public static void SetObject<T>(this ISession session, string key, T value) {
            var rawData = JsonSerializer.Serialize(value);
            session.SetString(key,rawData);
        }

        public static bool HasRepository(this HttpContext httpContext)
        {
            return httpContext.GetRepositoryId() != -1;
        }
        
        

        public static bool? GetBool(this ISession session, string key)
        {
            if (session.TryGetValue(key, out byte[] value))
            {
                bool storedValue = value[0] == 1;
                
                return storedValue;
            }
            return null;
        }

        public static void SetBool(this ISession session, string key, bool value)
        {
            session.Set(key, new byte[]{(byte)(value ? 1 : 0)});
        }
        
        // Stash cache extension methods
        private const string StashCategoriesKey = "stash_categories";
        private const string StashCategoriesTimestampKey = "stash_categories_timestamp";
        private const string StashNotesPrefix = "stash_notes_";
        private const string StashFileHashPrefix = "stash_file_hash_";
        private const string StashNotesTimestampPrefix = "stash_notes_timestamp_";
        
        public static List<BackEnd.CategoryListItemDto>? GetCachedCategories(this HttpContext httpContext)
        {
            return httpContext.Session.GetObject<List<BackEnd.CategoryListItemDto>>(StashCategoriesKey);
        }

        public static void SetCachedCategories(this HttpContext httpContext, List<BackEnd.CategoryListItemDto> categories)
        {
            httpContext.Session.SetObject(StashCategoriesKey, categories);
            httpContext.Session.SetString(StashCategoriesTimestampKey, DateTime.UtcNow.ToString("O"));
        }

        public static void InvalidateCategoriesCache(this HttpContext httpContext)
        {
            httpContext.Session.Remove(StashCategoriesKey);
            httpContext.Session.Remove(StashCategoriesTimestampKey);
        }

        public static List<BackEnd.NoteDto>? GetCachedNotes(this HttpContext httpContext, string categoryId)
        {
            return httpContext.Session.GetObject<List<BackEnd.NoteDto>>($"{StashNotesPrefix}{categoryId}");
        }

        public static void SetCachedNotes(this HttpContext httpContext, string categoryId, 
            List<BackEnd.NoteDto> notes, string fileHash)
        {
            httpContext.Session.SetObject($"{StashNotesPrefix}{categoryId}", notes);
            httpContext.Session.SetString($"{StashFileHashPrefix}{categoryId}", fileHash);
            httpContext.Session.SetString($"{StashNotesTimestampPrefix}{categoryId}", 
                DateTime.UtcNow.ToString("O"));
        }

        public static string? GetCachedFileHash(this HttpContext httpContext, string categoryId)
        {
            return httpContext.Session.GetString($"{StashFileHashPrefix}{categoryId}");
        }

        public static void InvalidateNotesCache(this HttpContext httpContext, string categoryId)
        {
            httpContext.Session.Remove($"{StashNotesPrefix}{categoryId}");
            httpContext.Session.Remove($"{StashFileHashPrefix}{categoryId}");
            httpContext.Session.Remove($"{StashNotesTimestampPrefix}{categoryId}");
        }
}