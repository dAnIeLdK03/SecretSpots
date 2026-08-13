using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Common.Email;

public class ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey) },
            Content = JsonContent.Create(new
            {
                from = options.Value.FromEmail,
                to = new[] { toEmail },
                subject,
                html = htmlBody,
            }),
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
