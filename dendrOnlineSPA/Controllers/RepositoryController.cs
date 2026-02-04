        

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using BackEnd;
using dendrOnlineSPA.Model;
using dendrOnlinSPA.model; // For HttpContext extension methods
using GitHubOAuthMiddleWare;
using System.Text.Json;
using Octokit;

namespace dendrOnlineSPA.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RepositoryController : DendronController
    {
        public RepositoryController(ILogger<RepositoryController> logger, IConfiguration configuration,
            INotesService notesService, IStashNotesService stashService) : base(logger, configuration, notesService, stashService)
        {
        }

        [HttpGet("/notes/{repositoryId}/children/{nodeId}")]
        public async Task<IActionResult> GetNodeChildren(long repositoryId, string nodeId)
        {
            // Example logic: get all notes, filter for direct children of nodeId
            var allNotes = await NotesService.GetNotes();
            if (!allNotes.IsOk)
            {
                return NotFound();
            }
            // Find direct children (one level below nodeId)
            var childrenIds = allNotes.TheResult
                .Where(x => x.StartsWith(nodeId + ".") && x.Count(c => c == '.') == nodeId.Count(c => c == '.') + 1)
                .ToList();
            var children = new List<Note>();
            foreach (var childId in childrenIds)
            {
                var note = await NotesService.GetNote(childId);
                if (note.IsOk)
                    children.Add(note.TheResult);
            }
            return Ok(children);
        }

        // ...existing code...
        [HttpGet("/Index")]
        public async Task Index()
        {
            HttpContext.Response.Redirect("/index.html");
        }

        [HttpGet("/user")]
        [ActionName("GetUser")]
        public async Task<Result<GHUser>> GetUser()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("dendrOnline"), new Uri("https://github.com/"));
            var accessToken = HttpContext.GetGithubAccessToken();
            client.Credentials = new Credentials(accessToken);
            var user = await client.User.Current();
            return new GHUser(user);
        }

        [HttpGet("/repositories")]
        public async Task<Result<IList<GhRepository>>> Get()
        {
            GitHubClient client = null;
            if (!_notesService.IsFileSystemRepo())
            {
                client = new GitHubClient(new ProductHeaderValue("dendrOnline"), new Uri("https://github.com/"));
                var accessToken = HttpContext.GetGithubAccessToken();
                client.Credentials = new Credentials(accessToken);
            }
            return await GetRepositories(client);
        }

        [HttpGet("/notes/{repositoryId}")]
        public async Task<Result<INoteHierarchy>> GetNotesHierarchy(long repositoryId)
        {
            HttpContext.SetRepositoryId(repositoryId);
            var notes = await NotesService.GetNotes();
            var hierarchy = await NotesService.GetHierarchy(notes, null, HttpContext.GetCurrentNote(), HttpContext.GetEditedNotes().Keys.ToList());
            return hierarchy;
        }

        [HttpGet("/dendron/{repositoryId}")]
        public async Task<Result<Dendron>> GetDendron(long repositoryId, [FromQuery] bool includeContent = true)
        {
            HttpContext.SetRepositoryId(repositoryId);
            var dendron = await NotesService.GetDendron(includeContent);
            if (dendron.IsOk)
            {
                dendron.TheResult.RepositoryId = repositoryId;
                var favorite = HttpContext.GetFavorite();
                dendron.TheResult.IsFavoriteRepository = favorite != null && favorite.Repository == repositoryId;
            }
            return dendron;
        }

        [HttpGet("/favorite/dendron/")]
        public async Task<Result<Dendron>> GetFavoriteDendron()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("dendrOnline"), new Uri("https://github.com/"));
            var accessToken = HttpContext.GetGithubAccessToken();
            client.Credentials = new Credentials(accessToken);
            var currentUser = await client.User.Current();
            var userId = currentUser.Id;
            var favorite = HttpContext.GetFavorite();
            HttpContext.Session.SetString("userId", userId.ToString());

            if (favorite != null)
            {
                var repository = await client.Repository.Get(favorite.Repository);
                Logger.LogInformation($"loading favorite repository {repository.Name} for user {currentUser.Name}");
                HttpContext.SetRepositoryId(favorite.Repository);
                var dendron = await NotesService.GetDendron();
                dendron.TheResult.RepositoryId = favorite.Repository;
                dendron.TheResult.IsFavoriteRepository = true;
                dendron.TheResult.RepositoryName = repository.Name;
                var user = await client.User.Current();
                dendron.TheResult.RepositoryOwner = user.Login;
                return dendron;
            }
            else
            {
                return Result<Dendron>.Error(ResultCode.NotFound, ConflictCode.NoConflict,
                    $"no favorite repository found");
            }
        }

        [HttpGet("/note/{repositoryId}/{noteId}")]
        public async Task<Result<Note>> GetNote(string repositoryId, string noteId)
        {
            var note = await NotesService.GetNote(noteId);
            return note;
        }

        [HttpPut("/note/{repositoryId}/{noteId}")]
        public async Task<Result<HierarchyAndSha>> SaveNote(string repositoryId, string noteId, [FromBody] Note note)
        {
            var setted = await NotesService.SetContent(noteId, note);
            if (!setted.IsOk)
            {
                return Result<HierarchyAndSha>.TransformError<Note, HierarchyAndSha>(setted);
            }

            var tree = await GetNotesHierarchy(long.Parse(repositoryId));
            if (!tree.IsOk)
            {
                return Result<HierarchyAndSha>.TransformError<INoteHierarchy, HierarchyAndSha>(tree);
            }

            var treeAndSha = new HierarchyAndSha()
            {
                Hierarchy = tree.TheResult,
                Sha = setted.TheResult.Sha
            };

            return treeAndSha;
        }

        [HttpDelete("/note/{repositoryId}/{noteId}")]
        public async Task<Result<INoteHierarchy>> DeleteNote(string repositoryId, string noteId, [FromQuery] bool recurse = false)
        {
            if (recurse)
            {
                var allNotes = await NotesService.GetNotes();
                if (!allNotes.IsOk)
                {
                    return Result<INoteHierarchy>.TransformError<List<string>, INoteHierarchy>(allNotes);
                }
                var children = allNotes.TheResult.Where(x => x.StartsWith(noteId) && x != noteId);
                foreach (var child in children)
                {
                    await NotesService.DeleteNote(child);
                }
            }
            var deleted = await NotesService.DeleteNote(noteId);
            if (!deleted.IsOk)
            {
                return Result<INoteHierarchy>.TransformError<Note, INoteHierarchy>(deleted);
            }
            Logger.LogDebug($"after note deletion {repositoryId} - {noteId} - {recurse}");
            var tree = await GetNotesHierarchy(long.Parse(repositoryId));
            Logger.LogDebug($" new tree : {tree.IsOk} - {tree.ErrorMessage}");
            if (tree.TheResult != null)
            {
                Logger.LogDebug("new tree :: ");
                Logger.LogDebug(tree.TheResult.Dump("  "));
            }
            return tree;
        }

        [HttpPost("/image/{repositoryId}")]
        public async Task<Result<string>> AddImage(long repositoryId, IFormFile image)
        {
            var name = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string extension = ".png";
            if (image.ContentType == "image/jpeg")
            {
                extension = ".jpg";
            }

            if (image.ContentType == "image/png")
            {
                extension = ".png";
            }

            if (image.ContentType == "image/bmp")
            {
                extension = ".bmp";
            }
            var fileName = name + extension;

            Logger.LogInformation($"received file {name} withe fileName {fileName} of type {image.ContentType}");
            try
            {
                await NotesService.AddImage(image, fileName);

                return fileName;
            }
            catch (Exception ex)
            {
                return Result<string>.Error(ResultCode.InternalError, ex.Message);
            }
        }

        [HttpGet("/images/{repositoryId}")]
        public async Task<Result<IList<ImageAsset>>> GetImages(long repositoryId)
        {
            var result = await NotesService.GetImages(repositoryId);
            return Result<IList<ImageAsset>>.Ok(result.TheResult);
        }

        [HttpGet("/notes/{repositoryId}/search")]
        public async Task<IActionResult> SearchNotes(long repositoryId, [FromQuery] string pattern, [FromQuery] bool searchInContent = false)
        {
            var allNotes = await NotesService.GetNotes();
            if (!allNotes.IsOk)
            {
                return NotFound();
            }
            var results = await Task.WhenAll(allNotes.TheResult.Select(async noteId => 
            {
                var noteResult = await NotesService.GetNote(noteId);
                if (noteResult.IsOk)
                {
                    noteResult.TheResult.Header.Name = noteId;
                }
                return noteResult;
            }));

            var matches = results
                .Where(r => r.IsOk)
                .Select(r => r.TheResult)
                .Where(note => 
                {
                    Console.WriteLine($"search {pattern} in note {note.Header.Id} - {note.Header?.Title} - {note.Header?.Description}");
                    if (string.IsNullOrEmpty(pattern)) return false;

                    bool match = (note.Header?.Title?.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                                 (note.Header?.Description?.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                                 (note.Header?.Id?.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

                    if (match) return true;

                    if (searchInContent && (note.Body?.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                    {
                        Console.WriteLine($"\t found {pattern} in content of {note.Header.Id} - {note.Header?.Title}");
                        return true;
                    }
                    return false;
                })
                .ToList();

            return Ok(matches);
        }

        [HttpGet("/images/{repositoryId}/{imageName}")]
        public async Task<IActionResult> GetImage(long repositoryId, string imageName)
        {
            var stream = await NotesService.GetImageStream(imageName);
            if (stream == null)
            {
                return NotFound();
            }
            return File(stream, "application/octet-stream", imageName);
        }

        public async Task<Result<IList<GhRepository>>> GetRepositories(GitHubClient client)
        {
            if (_notesService.IsFileSystemRepo())
            {
                var repos = await _notesService.GetRepositories();
                List<GhRepository> repositories = new List<GhRepository>();
                int i = 0;
                foreach (var r in repos)
                {
                    repositories.Add(new GhRepository(r.id, r.name, "%-debug-user-dendron-$"));
                    i++;
                }
                return repositories;
            }
            string repositoryList = HttpContext?.Session?.GetString("repositories");
            if (string.IsNullOrEmpty(repositoryList))
            {
                var repos = await client.Repository.GetAllForCurrent();
                var user = await client.User.Current();
                var owner = user.Login;
                var repositories = repos.Select(x => new GhRepository(x.Id, x.Name, owner)).ToList();
                repositoryList = JsonSerializer.Serialize(repositories);
                HttpContext?.Session?.SetString("repositories", repositoryList);
                return repositories;
            }
            else
            {
                var repositories = JsonSerializer.Deserialize<IList<GhRepository>>(repositoryList).ToList();
                return repositories;
            }
        }
    }
}