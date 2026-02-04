# Configuration Guide

This application supports multiple configuration sources in the following priority order (highest to lowest):
1. Command line arguments
2. Environment variables
3. User secrets (Development only)
4. appsettings.{Environment}.json
5. appsettings.json

## Configuration Keys

### File System Mode
- `fs-repo-path`: Path to local filesystem notes repository

### GitHub OAuth Settings
- `github_tokenUrl`: GitHub OAuth token endpoint (default: https://github.com/login/oauth/access_token)
- `github_authorizeUrl`: GitHub OAuth authorization endpoint (default: https://github.com/login/oauth/authorize)
- `github_clientId`: GitHub OAuth Client ID
- `github_clientSecret`: GitHub OAuth Client Secret
- `github_startUrl`: Application start URL
- `github_redirectUrl`: OAuth redirect URL
- `github_LogoutUrl`: Logout URL

## Configuration Methods

### 1. appsettings.json
Edit `appsettings.json` and add your configuration:
```json
{
  "fs-repo-path": "C:\\path\\to\\notes",
  "github_clientId": "your-client-id",
  "github_clientSecret": "your-client-secret"
}
```

### 2. Environment Variables
Set environment variables using the configuration key name:

**Windows (PowerShell):**
```powershell
$env:fs__repo__path = "C:\path\to\notes"
$env:github_clientId = "your-client-id"
$env:github_clientSecret = "your-client-secret"
```

**Windows (CMD):**
```cmd
set fs__repo__path=C:\path\to\notes
set github_clientId=your-client-id
set github_clientSecret=your-client-secret
```

**Linux/Mac:**
```bash
export fs__repo__path="/path/to/notes"
export github_clientId="your-client-id"
export github_clientSecret="your-client-secret"
```

Note: For hierarchical keys, use double underscore (`__`) as separator in environment variables.

### 3. User Secrets (Development Only)
User secrets are the recommended way to store sensitive data during development:

```bash
# Initialize user secrets (already configured)
cd dendrOnlineSPA

# Set individual secrets
dotnet user-secrets set "github_clientId" "your-client-id"
dotnet user-secrets set "github_clientSecret" "your-client-secret"
dotnet user-secrets set "fs-repo-path" "C:\path\to\notes"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "github_clientSecret"

# Clear all secrets
dotnet user-secrets clear
```

### 4. Command Line Arguments
Pass configuration via command line:

```bash
dotnet run --fs-repo-path "C:\path\to\notes"
dotnet run --github_clientId "your-client-id"
```

## Example: Running in File System Mode

Using user secrets (recommended for development):
```bash
dotnet user-secrets set "fs-repo-path" "C:\Users\YourName\Documents\Notes"
dotnet run
```

Using environment variable:
```powershell
$env:fs__repo__path = "C:\Users\YourName\Documents\Notes"
dotnet run
```

Using command line:
```bash
dotnet run --fs-repo-path "C:\Users\YourName\Documents\Notes"
```

## Example: Running in GitHub Mode

Using user secrets (recommended):
```bash
dotnet user-secrets set "github_clientId" "your-github-app-client-id"
dotnet user-secrets set "github_clientSecret" "your-github-app-client-secret"
dotnet user-secrets set "github_startUrl" "http://localhost:5000"
dotnet user-secrets set "github_redirectUrl" "http://localhost:5000/oauth/callback"
dotnet run
```

## Security Best Practices

1. **Never commit secrets to source control**
   - User secrets are stored outside the project directory
   - Add `appsettings.*.json` files with secrets to `.gitignore`

2. **Use user secrets for development**
   - Secrets are stored in your user profile directory
   - Safe from accidental commits

3. **Use environment variables for production**
   - Set via hosting platform (Azure, AWS, Docker, etc.)
   - Can be configured through CI/CD pipelines

4. **Keep appsettings.json clean**
   - Store only non-sensitive default values
   - Use empty strings as placeholders for secrets
