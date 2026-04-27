using System.Collections.Concurrent;
using BusBooking.Api.Infrastructure.Resend;

namespace BusBooking.Api.Tests.Support;

public record SentEmail(string To, string Subject, string Html, int AttachmentCount);

public class FakeResendEmailClient : IResendEmailClient
{
    public readonly ConcurrentQueue<SentEmail> Sent = new();

    public Task<ResendSendResult> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        IReadOnlyList<ResendAttachment> attachments,
        CancellationToken ct)
    {
        Sent.Enqueue(new SentEmail(toAddress, subject, htmlBody, attachments.Count));
        return Task.FromResult(new ResendSendResult($"msg_{Guid.NewGuid():N}", true, null));
    }
}
