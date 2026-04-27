namespace BusBooking.Api.Infrastructure.Resend;

public record ResendAttachment(string Filename, byte[] Content);
public record ResendSendResult(string? MessageId, bool Success, string? Error);

public interface IResendEmailClient
{
    Task<ResendSendResult> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        IReadOnlyList<ResendAttachment> attachments,
        CancellationToken ct);
}

