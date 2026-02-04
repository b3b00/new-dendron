# Logout Feature Implementation

## Overview

The logout feature allows users to log out from DendrOnline by clicking a logout button in the navigation menu. The implementation clears the user's session, revokes the GitHub OAuth token, and clears cached data.

## Implementation Summary

### Components Implemented

1. **Backend Controller** - [LogoutController.cs](../../../dendrOnlineSPA/Controllers/LogoutController.cs)
2. **Frontend API Client** - [dendronClient.ts](../../../dendrOnlineSPA/wwwroot/scripts/dendronClient.ts) (logout method)
3. **UI Component** - [App.svelte](../../../dendrOnlineSPA/wwwroot/App.svelte) (logout button and handler)
4. **Middleware Logging** - [GitHubOAuthMiddleware.cs](../../../GitHubOAuthMiddleWare/GitHubOAuthMiddleware.cs) (diagnostic logs)

## Backend Implementation

### LogoutController.cs

**Location**: `dendrOnlineSPA/Controllers/LogoutController.cs`

**Endpoint**: `POST /logout`

**Functionality**:
1. Retrieves the current GitHub access token from session
2. Revokes the token using GitHub's OAuth API
3. Clears the ASP.NET Core session
4. Deletes the session cookie
5. Returns success response

**GitHub App Grant Revocation**:
- **Endpoint**: `DELETE /applications/{client_id}/grant`
- **Authentication**: Basic Auth using `client_id:client_secret`
- **Body**: JSON with `{"access_token": "token"}`
- **Documentation**: [GitHub OAuth Apps - Delete an app authorization for an app](https://docs.github.com/en/rest/apps/oauth-applications#delete-an-app-authorization-for-an-app)

**Code Highlights**:
```csharp
// Revoke GitHub app grant using DELETE /applications/{client_id}/grant
var revokeUrl = $"https://api.github.com/applications/{clientId}/grant";
HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, revokeUrl);
var authBytes = System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
var authHeader = Convert.ToBase64String(authBytes);
request.Headers.Add("Authorization", $"Basic {authHeader}");
request.Headers.Add("User-Agent", "dendrOnline");
request.Headers.Add("Accept", "application/vnd.github+json");
var jsonBody = System.Text.Json.JsonSerializer.Serialize(new { access_token = accessToken });
request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
```

**Session & Cookie Management**:
```csharp
// Clear session
HttpContext.Session.Clear();

// Delete cookie with matching options
var cookieOptions = new CookieOptions
{
    Path = "/",
    HttpOnly = true,
    Secure = HttpContext.Request.IsHttps,
    SameSite = SameSiteMode.Lax,
    IsEssential = true
};
HttpContext.Response.Cookies.Delete("dendrOnline.Session", cookieOptions);
```

## Frontend Implementation

### DendronClient.logout()

**Location**: `dendrOnlineSPA/wwwroot/scripts/dendronClient.ts`

**Method**: `logout: async (): Promise<BackEndResult<void>>`

**Functionality**:
- Makes POST request to `/logout` endpoint
- Returns standard BackEndResult response
- Handles errors gracefully

### App.svelte - Logout Button

**Location**: `dendrOnlineSPA/wwwroot/App.svelte`

**UI Element**: Logout button in burger menu navigation

**Icon**: FontAwesome `faRightFromBracket`

**Handler Implementation**:
```typescript
const handleLogout = async () => {
    // Clear stash cache (categories and notes)
    StashCache.clearAll();
    
    // Call logout API to clear session and revoke GitHub token
    await DendronClient.logout();
    
    // Small delay to ensure cookie deletion is processed by browser
    await new Promise(resolve => setTimeout(resolve, 200));
    
    // Redirect to repositories endpoint which requires authentication
    window.location.replace('/repositories');
}
```

**Key Points**:
- Clears localStorage stash cache (categories and notes)
- Preserves favorite repository (stored in separate cookie)
- 200ms delay ensures cookie deletion is processed
- Uses `location.replace()` to prevent back button issues
- Redirects to `/repositories` to trigger OAuth flow

## Configuration Requirements

### User Secrets / App Settings

The following configuration is required in `appsettings.json` or user secrets:

```json
{
  "github_LogoutUrl": "https://api.github.com/applications/",
  "github_clientId": "your_client_id",
  "github_clientSecret": "your_client_secret"
}
```

**Important**: The `github_LogoutUrl` must be set to `https://api.github.com/applications/` for token revocation to work.

## How It Works - Step by Step

1. **User clicks "Logout" button** in navigation menu
2. **Frontend clears cache**: StashCache.clearAll() removes localStorage data
3. **API call**: POST request to `/logout` endpoint
4. **Token revocation**: Backend calls GitHub API to revoke the access token
   - GitHub returns 204 No Content on success
5. **Session cleared**: ASP.NET Core session is cleared
6. **Cookie deleted**: Session cookie is removed from browser
7. **Delay**: 200ms pause to ensure browser processes cookie deletion
8. **Redirect**: Browser redirects to `/repositories`
9. **OAuth triggered**: Middleware detects no token, redirects to GitHub OAuth
10. **GitHub auto-approves**: User is automatically re-authenticated (see below)
11. **New session created**: User receives new token and session

## GitHub OAuth Behavior

### Why Users Aren't Prompted for Login

After logout, users are automatically re-authenticated without seeing GitHub's login screen. This is **standard GitHub OAuth behavior**, not a bug.

**Reason**: GitHub OAuth Apps maintain application authorization separate from individual tokens. When you revoke a token and trigger OAuth again:

1. GitHub checks if the user is logged into GitHub ✓
2. GitHub checks if the app is authorized ✓
3. GitHub automatically issues a new token without prompting

**From GitHub Documentation**:
> "OAuth applications act on behalf of the user, making OAuth requests with the user's access token after the user authorizes the app." - [About OAuth Apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)

### To Force Re-Login

Users would need to either:
1. **Log out of GitHub** entirely in their browser
2. **Revoke app authorization** manually at: GitHub Settings → Applications → Authorized OAuth Apps
3. **Use GitHub Apps** instead of OAuth Apps (different authorization model)

### Token vs Authorization

- **Token Revocation** (what we do): Invalidates the specific access token
  - Old token cannot be used anymore ✓
  - New token is automatically issued if app is still authorized ✓
  
- **App Authorization Revocation**: Completely removes app authorization
  - Requires user action in GitHub settings
  - Would force full re-authorization flow

**Reference**: [Revoking authorization of GitHub Apps](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/reviewing-your-authorized-applications-oauth)

## Testing & Verification

### Logs to Verify Logout

When testing logout, check the console output for:

```
[OAuth Middleware] Path: /logout, Has Token: True, Token Length: 40
Logout called. Has token: True
Token revocation response: NoContent
Session cleared and cookie deleted
[OAuth Middleware] Path: /repositories, Has Token: False, Token Length: 0
[OAuth Middleware] Path: /auth, Has Token: False, Token Length: 0
[OAuth Middleware] Path: /Index, Has Token: True, Token Length: 40
```

**This confirms**:
- ✅ Logout was called with existing token
- ✅ GitHub token was successfully revoked (204 NoContent)
- ✅ Session was cleared
- ✅ Next request has NO token (Token Length: 0)
- ✅ OAuth flow was triggered (/auth endpoint)
- ✅ New token was issued (new Token Length: 40)

### What Changes

| Item | Before Logout | After Logout |
|------|---------------|--------------|
| Access Token | Old token (40 chars) | New token (40 chars) |
| Session Cookie | dendrOnline.Session=xyz | dendrOnline.Session=new |
| Stash Cache | Cached data in localStorage | Cleared |
| Favorite Repo | Preserved | Preserved ✓ |
| User Auth State | Authenticated | Re-authenticated automatically |

## Security Considerations

### Token Security
- Old token is properly revoked and cannot be reused
- Token revocation uses secure Basic Auth
- Client secret is never exposed to frontend

### Session Security
- Session cookie is HttpOnly (not accessible via JavaScript)
- Session cookie is Secure (HTTPS only in production)
- Session cookie uses SameSite=Lax to prevent CSRF

### Cache Clearing
- Sensitive cached data (stash notes) is cleared
- Favorite repository ID is preserved (non-sensitive)
- No PII remains in localStorage after logout

## Files Modified

### Created
- `dendrOnlineSPA/Controllers/LogoutController.cs` - Logout endpoint

### Modified
- `dendrOnlineSPA/wwwroot/App.svelte` - Added logout button and handler
- `dendrOnlineSPA/wwwroot/scripts/dendronClient.ts` - Added logout() method
- `GitHubOAuthMiddleWare/GitHubOAuthMiddleware.cs` - Added diagnostic logging
- `GitHubOAuthMiddleWare/Extensions.cs` - Updated Logout extension method with proper GitHub API call

## Known Limitations

1. **No forced re-login**: Users are automatically re-authenticated if still logged into GitHub
2. **App authorization persists**: The OAuth app authorization is not revoked, only the token
3. **Browser dependency**: Cookie deletion requires browser cooperation (200ms delay)

## Future Improvements

1. **Add logout confirmation dialog**: Warn users if they have unsaved work
2. **Show logout feedback**: Display a toast/message confirming logout
3. **Support for GitHub Apps**: Implement GitHub Apps for better authorization control
4. **Revoke app authorization**: Add option to fully revoke app (requires user consent in GitHub UI)

## References

- [GitHub OAuth Apps Documentation](https://docs.github.com/en/apps/oauth-apps)
- [Delete an app token API](https://docs.github.com/en/rest/apps/oauth-applications#delete-an-app-token)
- [Authorizing OAuth Apps](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [ASP.NET Core Session Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state#session-state)
