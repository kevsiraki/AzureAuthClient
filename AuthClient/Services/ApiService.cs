using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace AuthClient.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _config;

    public ApiService(HttpClient http, ITokenAcquisition tokenAcquisition, IConfiguration config)
    {
        _http = http;
        _tokenAcquisition = tokenAcquisition;
        _config = config;
    }

    public async Task<UserInfo?> GetUserAsync()
    {
        try
        {
            var scopes = _config["AzureAd:Scopes"]?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes!);

            var request = new HttpRequestMessage(HttpMethod.Get, "/whoami");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);

            Console.WriteLine($"API Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<UserInfo>();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return null;
        }
    }
}

public class UserInfo
{
    public string? Email { get; set; }
    public string? Name { get; set; }

    public string? Role { get; set; }
}