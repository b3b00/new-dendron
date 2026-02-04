using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace BackEnd
{

    public interface INotesService
    {
        Task<Result<(string content, string sha)>> GetContent(string name);

        Task<Result<Note>> SetContent(string noteName, Note note);

        Task<Result<List<string>>> GetNotes();

        Task<Result<Note>> GetNote(string noteName);
        
        Task<Result<Note>> DeleteNote(string noteName);

        Task<Result<INoteHierarchy>> GetHierarchy(List<string> notes, string filter, string currentNote, List<string> editedNotes);

        void SetRepository(string name, long id);

        void SetUser(string name, long id);
        
        void SetAccessToken(string token);

        Task<Result<Dendron>> GetDendron(bool includeContent = true);

        Task AddImage(IFormFile file, string fileName);

        Task<Result<IList<ImageAsset>>> GetImages(long repositoryId);

        bool IsFileSystemRepo();

        Task<List<(string name, long id)>> GetRepositories();

        Task<Stream> GetImageStream(string fileName);

    }
}