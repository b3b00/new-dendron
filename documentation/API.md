# API Documentation

## Authentication

The application uses GitHub OAuth for authentication. When a user is not authenticated, the behavior depends on how the request is made:

- **Browser navigation**: Redirects to GitHub OAuth authorization page
- **API calls (fetch/XHR)**: Returns `401 Unauthorized` with JSON error response

## Custom Headers

### X-Dendron-API-Call

**Purpose**: Indicates that a request is an API call (fetch/XHR) rather than a browser navigation.

**Usage**: Include this header in all fetch/AJAX requests to ensure proper error handling for unauthenticated requests.

**Behavior**:
- When this header is present and the user is not authenticated, the middleware returns a `401` status with a JSON error response instead of redirecting to the OAuth flow.
- This prevents CORS errors that would occur if a fetch request followed a redirect to GitHub's OAuth endpoint.

**Example**:
```javascript
fetch('/api/endpoint', {
  headers: {
    'X-Dendron-API-Call': 'true'
  }
})
.then(response => {
  if (response.status === 401) {
    // Redirect the page to initiate OAuth flow
    window.location.href = '/';
  }
  return response.json();
})
```

**Implementation Details**:
- Defined in: `GitHubOAuthMiddleWare/GitHubOAuthMiddleware.cs`
- Check location: Line 112
- Without this header, unauthenticated requests trigger a redirect to GitHub OAuth, which causes CORS errors when initiated from JavaScript fetch calls.

## Response Format

### Success Responses
API endpoints return JSON responses with appropriate HTTP status codes (200, 201, 204, etc.).

### Error Responses
Error responses are returned as JSON with the following structure:

```json
{
  "error": "Error type or title",
  "message": "Detailed error message"
}
```

Common HTTP status codes:
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `404 Not Found` - Resource not found
- `409 Conflict` - Concurrent modification conflict
- `500 Internal Server Error` - Server error

## API Endpoints

See specific documentation for different API sections:
- [Stash Notes API](features/stash%20notes/api.md) - Endpoints for managing stash categories and notes
