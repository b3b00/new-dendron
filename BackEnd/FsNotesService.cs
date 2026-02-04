
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BackEnd
{

    public class FsNotesService : AsbtractNotesService
    {

        public override bool IsFileSystemRepo() => true;

        public override async Task<string> GetRepositoryName()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(RootDirectory);
            return dirInfo.Name;
        }

        private (string name, long id) _currentRepository { get; set; } = ("", -1);
        
        private long _currentRepositoryId { get; set; } = -1;
        public string RootDirectory { get; set; }

        public FsNotesService(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }

        public override async Task<string> GetUserLogin() => "test";

        public override async Task AddImage(IFormFile file, string fileName)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", "assets", "images", fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        public override Task<Stream> GetImageStream(string fileName)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", "assets", "images", fileName);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(stream);
        }

        public override async Task<Result<IList<ImageAsset>>> GetImages(long repositoryId)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", "assets", "images");
            return Directory.GetFiles(path).Select(x => new ImageAsset(Path.GetFileName(x), $"/images/{_currentRepository.id}/{Path.GetFileName(x)}")).ToList();  
        }

        public override void SetRepository(string name, long id)
        {
            var repositories = _repositories ?? GetRepositories().GetAwaiter().GetResult();
            var rep = repositories.FirstOrDefault(r => r.id == id);
            _currentRepository = rep;
        }

        public override void SetAccessToken(string token)
        {
        }

        public override async Task<Result<(string content, string sha)>> GetContent(string name)
        {
            string content = "";
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", name + ".md");
            if (File.Exists(path))
            {
                content = File.ReadAllText(path);
            }

            return (content,"nope");
        }

        public override async Task<string> CreateNote(string noteName)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", noteName + ".md");
            if (!File.Exists(path))
            {
                Note note = new Note()
                {
                    Body = "*empty*",
                    Header = new NoteHeader(noteName)
                };
                File.WriteAllText(path, note.ToString());
                return note.ToString();
            }

            return "";
        }

        public override async Task<Result<Note>> SetContent(string noteName, Note note)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", noteName + ".md");
            if (File.Exists(path))
            {
                System.Console.WriteLine($"Writing content to {path}");
                File.WriteAllText(path, note.ToString());
            }
            else 
            {
                System.Console.WriteLine($"File {path} does not exist. Creating new file.");
                File.WriteAllText(path, note.ToString());
            }
            var newNote = await GetNote(noteName);            
            return newNote;
        }

        //C:\Users\olduh\DendronNotes
        public override async Task<Result<List<string>>> GetNotes()
        {
            var notedir = new DirectoryInfo(Path.Combine(RootDirectory, _currentRepository.name, "notes"));
            var files = notedir.GetFiles("*.md");
            var list = files.Select(x => x.Name.Replace(".md", "")).ToList();
            return list;
        }

        public override async Task<Result<Note>> DeleteNote(string noteName)
        {
            var path = Path.Combine(RootDirectory, _currentRepository.name, "notes", noteName + ".md");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Result<Note>.Ok();
        }

        private List<(string name, long id)> _repositories = new List<(string name, long id)>();

        public override Task<List<(string name, long id)>> GetRepositories()
        {
            var reposDir = new DirectoryInfo(Path.Combine(RootDirectory));
            var dirs = reposDir.GetDirectories();


            _repositories = new List<(string name, long id)>();
            int i = 100;
            foreach (var r in dirs)
            {                
                _repositories.Add((r.Name, i));
                i++;
            }


            return Task.FromResult(_repositories);
        }
    }
}