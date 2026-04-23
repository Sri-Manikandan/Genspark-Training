using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.Infrastructure.Resend;

public class ResendEmailClient : IResendEmailClient
{
    public const string HttpClientName = "resend";
    private readonly HttpClient _http;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailClient> _log;

    public ResendEmailClient(
        IHttpClientFactory httpFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailClient> log)
    {
        _options = options.Value;
        _log = log;
        _http = httpFactory.CreateClient(HttpClientName);
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<ResendSendResult> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        IReadOnlyList<ResendAttachment> attachments,
        CancellationToken ct)
    {
        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromAddress
            : $"{_options.FromName} <{_options.FromAddress}>";

        var body = new
        {
            from,
            to = new[] { toAddress },
            subject,
            html = htmlBody,
            attachments = attachments.Select(a => new
            {
                filename = a.Filename,
                content = Convert.ToBase64String(a.Content)
            }).ToArray()
        };

        try
        {
            var resp = await _http.PostAsJsonAsync("/emails", body, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync(ct);
                _log.LogWarning("Resend email failed {Status} {Body}", resp.StatusCode, text);
                return new ResendSendResult(null, false, $"{(int)resp.StatusCode}: {text}");
            }

            var dto = await resp.Content.ReadFromJsonAsync<SendResponse>(cancellationToken: ct);
            return new ResendSendResult(dto?.id, true, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Resend send exception");
            return new ResendSendResult(null, false, ex.Message);
        }
    }

    private record SendResponse(string? id);
}

