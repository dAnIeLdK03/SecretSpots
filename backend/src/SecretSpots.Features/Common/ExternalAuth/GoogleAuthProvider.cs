using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Common.ExternalAuth;

public class GoogleAuthProvider(
    HttpClient httpClient,
    IOptions<GoogleAuthOptions> googleOptions,
    IOptions<ExternalAuthOptions> externalAuthOptions) : IExternalAuthProvider
{
    public ExternalAuthProvider Provider => ExternalAuthProvider.Google;

    private string RedirectUri => $"{externalAuthOptions.Value.ApiBaseUrl}/auth/google/callback";

    public string GetAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = googleOptions.Value.ClientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
        };
        return QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", query);
    }

    public async Task<ExternalAuthUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = googleOptions.Value.ClientId,
                ["client_secret"] = googleOptions.Value.ClientSecret,
                ["redirect_uri"] = RedirectUri,
                ["grant_type"] = "authorization_code",
            }), cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var tokens = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);

        var userInfoResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "https://openidconnect.googleapis.com/v1/userinfo")
        {
            Headers = { Authorization = new("Bearer", tokens!.AccessToken) },
        }, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken);

        return new ExternalAuthUserInfo(userInfo!.Sub, userInfo.Email, userInfo.EmailVerified, userInfo.Name);
    }

    private record GoogleTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

    private record GoogleUserInfoResponse(
        string Sub,
        string Email,
        [property: JsonPropertyName("email_verified")] bool EmailVerified,
        string Name);
}
