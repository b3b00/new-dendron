using System.Reflection.Metadata;
using BackEnd;
using GitHubOAuthMiddleWare;

namespace dendrOnlineSPA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"DendrOnline is starting with {string.Join(", ", args)}");
            var builder = WebApplication.CreateBuilder(args);

            // Configure configuration sources
            builder.Configuration
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            // Add user secrets in development
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(1); });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("cors",
                    policy => { policy.WithOrigins("*"); });
            });
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = "dendrOnline.Session";
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.IsEssential = true;
            });

            // Check for filesystem repository path from configuration
            // Can be set via:
            // 1. appsettings.json: "fs-repo-path": "C:\\path\\to\\notes"
            // 2. Environment variable: fs__repo__path=C:\path\to\notes
            // 3. User secrets: dotnet user-secrets set "fs-repo-path" "C:\path\to\notes"
            // 4. Command line: dotnet run --fs-repo-path "C:\path\to\notes"
            var fsRepoPath = builder.Configuration["fs-repo-path"];
            Console.WriteLine($"Configuration 'fs-repo-path' value: '{fsRepoPath}'");

            bool dirExists = !string.IsNullOrEmpty(fsRepoPath) && Directory.Exists(fsRepoPath);
            bool isFSRepo = dirExists;
            Console.WriteLine($"Directory exists: {dirExists}");
            Console.WriteLine($"Is FS repo: {isFSRepo}");
            if (isFSRepo)
            {
                Console.WriteLine($"Using filesystem notes service at {fsRepoPath}");
                // using singleton as the filesystem service is for debug only purposes. And so no multi-user concurrency is expected.
                builder.Services.AddSingleton<INotesService>(sp => new FsNotesService(fsRepoPath));
                builder.Services.AddSingleton<IStashNotesService>(sp => new FsStashNotesService(fsRepoPath));
            }
            else
            {
                Console.WriteLine($"Using github notes service");
                builder.Services.AddScoped<INotesService, GithubNotesService>();
                builder.Services.AddScoped<IStashNotesService, GithubStashNotesService>();
            }
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                Console.WriteLine("Using GitHub notes service");
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.UseSession();
            app.UseStaticFiles();
            app.UseCors();
            if (!isFSRepo)
            {
                app.UseGHOAuth(options =>
                {
                    options.TokenEndpoint = app.Configuration[Constants.TokenUrlParameter];
                    options.AuthorizationEndpoint = app.Configuration[Constants.AuthorizeUrlParameter];
                    options.ClientId = app.Configuration[Constants.ClientIdParameter];
                    options.ClientSecret = app.Configuration[Constants.ClientSecretParameter];
                    options.ReturnUrlParameter = app.Configuration[Constants.StartUrlParameter];
                    options.RedirectUri = app.Configuration[Constants.RedirectUrlParameter];
                    options.ExcludePath = "/health";
                });
            }

            app.UseNoCors();

            if (app.Environment.IsDevelopment())
            {
                app.Run();
            }
            else
            {
                var port = Environment.GetEnvironmentVariable("PORT");
                Console.WriteLine("DendrOnline is listening to http://*:" + port);
                app.Run("http://*:" + port);
            }
        }
    }
}