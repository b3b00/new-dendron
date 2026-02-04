# Logout Feature Implementation Plan

## Overview
Implement a logout button in the burger menu that allows users to logout from DendrOnline. The logout process should revoke session cookies, clear localStorage cache (except favorite repo), and revoke the GitHub OAuth token.

## Architecture Analysis

### Current Authentication System
- **Middleware**: `GitHubOAuthMiddleWare` handles OAuth flow and session management
- **Session Storage**: Access token stored in `HttpContext.Session` with key `"accessToken"`
- **Cookies**: 
  - Session cookie: `"dendrOnline.Session"` (1-day timeout)
  - Favorite repo cookie: `"dendron-favorite"` (persistent)
- **LocalStorage Cache**:
  - `stash_categories` - cached stash categories
  - `stash_notes_*` - cached stash notes by category ID
  - No favorite repo stored in localStorage (uses cookie instead)

### Existing Logout Infrastructure
The `Extensions.cs` file already contains a `Logout` method that:
- Clears session data
- Revokes GitHub token via API
- Uses the GitHub app's client ID and logout URL

## Implementation Plan

### 1. Backend: Create Logout Controller Endpoint

**File**: `dendrOnlineSPA/Controllers/LogoutController.cs` (new file)

**Implementation**:
- Create new controller with `[HttpPost("/logout")]` endpoint
- Inject `IConfiguration` to access GitHub client ID and logout URL
- Use existing `Logout` extension method from `GitHubOAuthMiddleWare.Extensions`
- Clear session via `HttpContext.Session.Clear()`
- Revoke GitHub token using DELETE request to GitHub API
- Return 200 OK on success

**Key steps**:
```csharp
- Get access token from session
- Get clientId from configuration (Constants.ClientIdParameter)
- Get logoutUrl from configuration (Constants.LogoutUrlParameter)  
- Call gitHubClient.Logout(context, accessToken, clientId, logoutUrl)
- Return Ok()
```

### 2. Frontend: Create Logout API Client Method

**File**: `dendrOnlineSPA/wwwroot/scripts/dendronClient.ts`

**Implementation**:
- Add `logout` method to `DendronClient` object
- Make POST request to `/logout` endpoint
- Return success/failure result

**Method signature**:
```typescript
logout: async (): Promise<BackEndResult<void>>
```

### 3. Frontend: Create Cache Clearing Utility

**File**: `dendrOnlineSPA/wwwroot/scripts/stashCache.ts`

**Implementation**:
- Add new method `clearAllExceptFavorite()` to `StashCache` object
- Clear stash categories using existing `clearCategories()`
- Clear all stash notes using existing `clearAllNotes()`
- Do NOT clear favorite repo (it's in cookies, not localStorage)

**Note**: The existing `clearAll()` method already clears stash cache appropriately.

### 4. Frontend: Add Logout Button to Navigation Menu

**File**: `dendrOnlineSPA/wwwroot/App.svelte`

**Implementation**:
- Import logout icon from FontAwesome: `faRightFromBracket` or `faSignOutAlt`
- Add logout button as last item in `<nav><ul>` menu (lines 100-118)
- Create `handleLogout` async function
- On click:
  1. Clear stash cache: `StashCache.clearAll()`
  2. Call logout API: `await DendronClient.logout()`
  3. Redirect to home: `window.location.href = '/'`
  4. Browser will be redirected to GitHub OAuth as session is cleared

**Menu position**: Add after the stash menu item, before closing `</ul>`

**Button structure**:
```svelte
<li>
  <a href="#" on:click|preventDefault={handleLogout}>
    <Fa icon="{faRightFromBracket}"/>
    <span style="margin-left: 5px">Logout</span>
  </a>
</li>
```

### 5. State Management Considerations

**File**: `dendrOnlineSPA/wwwroot/scripts/dendronStore.ts`

**Optional Enhancement**: Reset all stores on logout
- Clear `repository`, `tree`, `loadedNotes`, `draftNotes` stores
- Reset `isFavoriteRepository` to false
- This provides cleaner state but is not strictly necessary as page will reload

## Testing Plan

### Manual Testing Steps
1. **Login Flow**: Login to DendrOnline using GitHub OAuth
2. **Load Data**: Navigate to repositories and load a dendron
3. **Verify Cache**: Check localStorage for stash cache entries
4. **Set Favorite**: Mark a repository as favorite
5. **Click Logout**: Click logout button in burger menu
6. **Verify Logout**:
   - Redirected to home page
   - Session cleared (redirected to GitHub OAuth on API call)
   - LocalStorage stash cache cleared
   - Favorite repo cookie preserved
7. **Re-login**: Login again with GitHub OAuth
8. **Verify Favorite**: Confirm favorite repository still loads

### Edge Cases to Test
- Logout with unsaved draft notes (should we warn user?)
- Logout while API call is in progress
- Network failure during logout
- Logout in filesystem mode (OAuth not enabled)

## Configuration Requirements

Ensure `appsettings.json` or environment variables include:
- `github_clientId`: GitHub OAuth app client ID
- `github_LogoutUrl`: GitHub OAuth revocation endpoint (typically `https://api.github.com/applications/`)

## Implementation Order

1. **Backend Controller** - Create logout endpoint (independent, testable via API)
2. **Frontend API Client** - Add logout method to DendronClient
3. **Frontend UI** - Add logout button to menu
4. **Integration Test** - Verify end-to-end flow

## Security Considerations

- **CSRF Protection**: Current middleware uses CSRF state tokens for OAuth flow; logout doesn't require CSRF as it's a destructive operation
- **Token Revocation**: Properly revoke GitHub token to prevent unauthorized access
- **Session Cleanup**: Ensure complete session cleanup on server
- **Favorite Persistence**: Cookie-based favorite repo survives logout (as required)

## Alternative Considerations

### Filesystem Mode
The application supports a filesystem mode where GitHub OAuth is not used. In this case:
- Logout endpoint should detect filesystem mode
- Skip GitHub token revocation
- Only clear session and cache
- Consider showing/hiding logout button based on mode

## Files to Modify/Create

### New Files
1. `dendrOnlineSPA/Controllers/LogoutController.cs`

### Modified Files
1. `dendrOnlineSPA/wwwroot/App.svelte` - Add logout button and handler
2. `dendrOnlineSPA/wwwroot/scripts/dendronClient.ts` - Add logout API method
3. `dendrOnlineSPA/wwwroot/scripts/stashCache.ts` - Optional: add clearAllExceptFavorite method (or use existing clearAll)

## Estimated Effort

- Backend Controller: 30 minutes
- Frontend API Client: 15 minutes  
- Frontend UI Integration: 30 minutes
- Testing: 45 minutes
- **Total**: ~2 hours

## Success Criteria

- ✅ Logout button visible in burger menu
- ✅ Clicking logout clears session cookies
- ✅ Clicking logout clears localStorage stash cache
- ✅ Clicking logout preserves favorite repo cookie
- ✅ Clicking logout revokes GitHub OAuth token
- ✅ User must re-authenticate with GitHub to access notes
- ✅ No errors in console during logout flow
- ✅ Favorite repository auto-loads after re-authentication
