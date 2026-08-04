using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Common.ExternalAuth;

public class FacebookAuthProvider(
    HttpClient httpClient,
    IOptions<FacebookAuthOptions> facebookOptions,
    IOptions<ExternalAuthOptions> externalAuthOptions) : IExternalAuthProvider
{
    public ExternalAuthProvider Provider => ExternalAuthProvider.Facebook;

    private string RedirectUri => $"{externalAuthOptions.Value.ApiBaseUrl}/auth/facebook/callback";

    public string GetAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = facebookOptions.Value.AppId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "email,public_profile",
            ["state"] = state,
        };
        return QueryHelpers.AddQueryString("https://www.facebook.com/v19.0/dialog/oauth", query);
    }

    public async Task<ExternalAuthUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var tokenQuery = new Dictionary<string, string?>
        {
            ["client_id"] = facebookOptions.Value.AppId,
            ["client_secret"] = facebookOptions.Value.AppSecret,
            ["redirect_uri"] = RedirectUri,
            ["code"] = code,
        };

        var tokenUrl = QueryHelpers.AddQueryString("https://graph.facebook.com/v19.0/oauth/access_token", tokenQuery);
        
        var tokenResponse = await httpClient.GetAsync(tokenUrl, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<FacebookTokenResponse>(cancellationToken);

        var userInfoQuery = new Dictionary<string, string?>{
            ["fields"] = "id,name,email",
            ["access_token"] = tokens!.AccessToken,
        };

        var userInfoUrl = QueryHelpers.AddQueryString("https://graph.facebook.com/me", userInfoQuery);
        var userInfoResponse = await httpClient.GetAsync(userInfoUrl, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<FacebookUserInfoResponse>(cancellationToken);

        var isEmailVerified = !string.IsNullOrWhiteSpace(userInfo!.Email);

        return new ExternalAuthUserInfo(
            userInfo.Id,
            userInfo.Email,
            isEmailVerified,
            userInfo.Name
        );
    }

    private record FacebookTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record FacebookUserInfoResponse(string Id, string Email, string Name);
}
