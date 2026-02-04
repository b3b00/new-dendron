# Filesystem Mode for Notes Service

## Overview

The dendrOnlineSPA application can now run in two modes:

1. **GitHub Mode** (default) - Uses `GithubNotesService` to read/write notes from GitHub repositories
2. **Filesystem Mode** - Uses `FsNotesService` to read/write notes from a local filesystem directory

## Usage

### GitHub Mode (Default)

Simply run the application without any arguments:

```bash
dotnet run --project dendrOnlineSPA
```

The application will use GitHub OAuth and the `GithubNotesService`.

### Filesystem Mode

Specify the path to a local notes repository using the `--fs-repo-path` argument:

```bash
dotnet run --project dendrOnlineSPA -- --fs-repo-path "C:\Users\username\my-notes"
```

Or using configuration:

```bash
dotnet run --project dendrOnlineSPA --fs-repo-path "C:\Users\username\my-notes"
```

**Note:** The path must exist. If the directory doesn't exist, the application will fall back to GitHub mode.

## Configuration Options

### Command-Line Argument

```bash
--fs-repo-path="C:\path\to\notes"
```

### appsettings.json

Add to `appsettings.json` or `appsettings.Development.json`:

```json
{
  "fs-repo-path": "C:\\path\\to\\notes"
}
```

### Environment Variable

```bash
# Windows PowerShell
$env:fs-repo-path="C:\path\to\notes"
dotnet run --project dendrOnlineSPA

# Linux/Mac
export fs-repo-path="/path/to/notes"
dotnet run --project dendrOnlineSPA
```

## Examples

### Development with Local Notes

```bash
# Use a local test repository
dotnet run --project dendrOnlineSPA -- --fs-repo-path "C:\dev\test-notes"
```

### Production with GitHub

```bash
# Production deployment (no fs-repo-path specified)
dotnet run --project dendrOnlineSPA
```

## Implementation Details

### Service Registration

The `Program.cs` checks for the `fs-repo-path` configuration at startup:

```csharp
var fsRepoPath = builder.Configuration["fs-repo-path"] ?? 
    args.FirstOrDefault(arg => arg.StartsWith("--fs-repo-path="))
        ?.Substring("--fs-repo-path=".Length);

if (!string.IsNullOrEmpty(fsRepoPath) && Directory.Exists(fsRepoPath))
{
    // Filesystem mode
    builder.Services.AddScoped<INotesService>(sp => new FsNotesService(fsRepoPath));
}
else
{
    // GitHub mode
    builder.Services.AddScoped<INotesService, GithubNotesService>();
}
```

### Stash Notes Service

The stash notes service selection is independent and based on session state (see StashController):
- Uses `GithubStashNotesService` when a GitHub repository is active in the session
- Falls back to `FsStashNotesService` otherwise

## Benefits

- **Development**: Test locally without GitHub authentication
- **Demo**: Run demonstrations with sample note repositories
- **Offline**: Work with notes when offline
- **Performance**: Faster for local development and testing

## Limitations

In Filesystem Mode:
- No GitHub OAuth required (no authentication)
- No cloud sync
- No collaboration features
- Repository selector will show the local directory name
- Images and assets may not work identically to GitHub mode

## Troubleshooting

### Path Not Found

If you see "Using GitHub notes service" even though you specified a path:

1. Verify the path exists: `Test-Path "C:\path\to\notes"`
2. Check for typos in the path
3. Ensure the path is absolute, not relative
4. On Windows, use quotes around paths with spaces

### Permission Issues

Ensure the application has read/write permissions to the specified directory:

```powershell
# Check directory permissions
Get-Acl "C:\path\to\notes" | Format-List
```

## See Also

- [Stash Notes Implementation](GITHUB_STASH_IMPLEMENTATION.md)
- [API Documentation](../documentation/api.md)
