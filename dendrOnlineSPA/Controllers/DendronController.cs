using BackEnd;
using GitHubOAuthMiddleWare;
using Microsoft.AspNetCore.Mvc;
using dendrOnlineSPA.Model;
using dendrOnlinSPA.model;
using Octokit;

namespace dendrOnlineSPA.Controllers;

public class DendronController : ControllerBase
{
    
    
    
    private readonly ILogger<DendronController> _logger;

    protected ILogger<DendronController> Logger => _logger;

    private readonly IConfiguration _configuration;

    protected IConfiguration Configuration => _configuration;

    protected readonly INotesService _notesService;

    protected readonly IStashNotesService _stashService;

    protected INotesService NotesService
    {
        get
        {
            SetClient();
            return _notesService;
        }
    }

    protected IStashNotesService StashService
    {
        get
        {
            SetClient();
            return _stashService;
        }
    }

    public DendronController(ILogger<DendronController> logger, IConfiguration configuration, INotesService notesService, IStashNotesService stashService)
    {
        _logger = logger;
        _configuration = configuration;
        if (notesService.IsFileSystemRepo() && notesService is FsNotesService fs)
        {
            if (string.IsNullOrEmpty(fs.RootDirectory))    
            {
                ;
            }
        }
        _notesService = notesService;
        _stashService = stashService;
    }
    
    protected void SetClient()
    {
        if (HttpContext.HasRepository())
        {
            _notesService.SetRepository(HttpContext.GetRepositoryName(),HttpContext.GetRepositoryId());
            _stashService.SetRepository(HttpContext.GetRepositoryName(),HttpContext.GetRepositoryId());
        }
        _notesService.SetUser(HttpContext.GetUserName(),HttpContext.GetUserId());
        _notesService.SetAccessToken(HttpContext.GetGithubAccessToken());
        _stashService.SetAccessToken(HttpContext.GetGithubAccessToken());
    }
    
}