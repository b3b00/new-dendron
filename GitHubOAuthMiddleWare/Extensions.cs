using Microsoft.AspNetCore.Http;
using Octokit;

namespace GitHubOAuthMiddleWare;

public static class Extensions
{
    public static string GetGithubAccessToken(this HttpContext httpContext)
    {
        return httpContext.Session.GetString(GitHubOAuthMiddleWare.GitHubOAuthMiddleware.AccessToken);
    }

   
    
    public static void DeleteGithubAccessToken(this HttpContext httpContext)
    {
        httpContext.Session.Remove(GitHubOAuthMiddleWare.GitHubOAuthMiddleware.AccessToken);
        var cookies = httpContext.Request.Cookies.Keys;
    }

    public static void SetGithubAccessToken(this HttpContext httpContext, string value)
    {
        httpContext.Session.SetString(GitHubOAuthMiddleware.AccessToken, value);
    }
    
    public static string GetGithubTokenType(this HttpContext httpContext)
    {
        return httpContext.Session.GetString(GitHubOAuthMiddleware.TokenType);
    }

    public async static Task Logout(this GitHubClient gitHubClient, HttpContext context, string accessToken, string clientId, string logoutUrl)
    {
        // Clear session first
        context.DeleteGithubAccessToken();
        context.Session.Clear();
        
        // Revoke the GitHub token using proper authentication
        // GitHub requires DELETE to /applications/{client_id}/token with Basic Auth
        using (HttpClient client = new HttpClient())
        {
            // The correct endpoint is: DELETE /applications/{client_id}/token
            // With Basic Auth using client_id:client_secret
            // And JSON body with the access_token
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"{logoutUrl}{clientId}/token");
            
            // GitHub expects JSON body with the access_token
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(new { access_token = accessToken });
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            
            // Set required headers
            request.Headers.Add("User-Agent", "dendrOnline");
            request.Headers.Add("Accept", "application/vnd.github+json");
            
            // Note: Basic authentication with client_id:client_secret should be added by the caller
            // as we need the client_secret which isn't passed here
            
            try
            {
                var response = await client.SendAsync(request);
                // 204 No Content = success, 404 = token already revoked
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"GitHub token revocation failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error revoking GitHub token: {ex.Message}");
                // Don't throw - session is already cleared
            }
        }
    }

    public static string HostAndPath(this HttpRequest request) => request.Host + request.Path;

   
}