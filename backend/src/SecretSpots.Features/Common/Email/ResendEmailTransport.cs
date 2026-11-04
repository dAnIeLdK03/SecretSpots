using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Common.Email;

public class ResendEmailTransport(HttpClient httpClient, IOptions<ResendOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey) },
            Content = JsonContent.Create(new
            {
                from = options.Value.FromEmail,
                to = new[] { message.ToEmail },
                subject = message.Subject,
                html = message.HtmlBody,
            }),
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
