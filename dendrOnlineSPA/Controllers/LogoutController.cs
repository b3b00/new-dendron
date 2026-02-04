using BackEnd;
using GitHubOAuthMiddleWare;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace dendrOnlineSPA.Controllers;

public class LogoutController : DendronController
{
    public LogoutController(ILogger<LogoutController> logger, IConfiguration configuration,
        INotesService notesService, IStashNotesService stashService) 
        : base(logger, configuration, notesService, stashService)
    {
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var accessToken = HttpContext.GetGithubAccessToken();
            Logger.LogInformation($"Logout called. Has token: {!string.IsNullOrEmpty(accessToken)}");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                var clientId = Configuration[Constants.ClientIdParameter];
                var clientSecret = Configuration[Constants.ClientSecretParameter];
                // Use GitHub's grant revocation endpoint
                var revokeUrl = $"https://api.github.com/applications/{clientId}/grant";

                Logger.LogInformation($"GitHub config - ClientId: {!string.IsNullOrEmpty(clientId)}, Secret: {!string.IsNullOrEmpty(clientSecret)}, RevokeUrl: '{revokeUrl}'");

                if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, revokeUrl);
                            // GitHub requires Basic Auth with client_id:client_secret
                            var authBytes = System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
                            var authHeader = Convert.ToBase64String(authBytes);
                            request.Headers.Add("Authorization", $"Basic {authHeader}");
                            request.Headers.Add("User-Agent", "dendrOnline");
                            request.Headers.Add("Accept", "application/vnd.github+json");
                            // JSON body with access_token
                            var jsonBody = System.Text.Json.JsonSerializer.Serialize(new { access_token = accessToken });
                            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                            Logger.LogInformation($"Revoking app grant at: {revokeUrl}");
                            var response = await client.SendAsync(request);
                            Logger.LogInformation($"App grant revocation response: {response.StatusCode}");
                            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                            {
                                var errorBody = await response.Content.ReadAsStringAsync();
                                Logger.LogWarning($"GitHub app grant revocation failed: {response.StatusCode} - {errorBody}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to revoke GitHub app grant, continuing with logout");
                    }
                }
                else
                {
                    Logger.LogWarning("Cannot revoke GitHub app grant - missing configuration");
                }
            }
            
            // Always clear session and delete cookie
            HttpContext.Session.Clear();
            
            // Delete the session cookie with matching options to ensure removal
            var cookieOptions = new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            };
            HttpContext.Response.Cookies.Delete("dendrOnline.Session", cookieOptions);
            
            Logger.LogInformation("Session cleared and cookie deleted");

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during logout");
            HttpContext.Session.Clear();
            var cookieOptions = new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                IsEssential = true
            };
            HttpContext.Response.Cookies.Delete("dendrOnline.Session", cookieOptions);
            return Ok(new { message = "Logged out" });
        }
    }
}
