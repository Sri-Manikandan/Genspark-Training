using System.Text;
using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Resend;
using BusBooking.Api.Models;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api.Services;

public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _log;
    private readonly IResendEmailClient _email;
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public LoggingNotificationSender(
        ILogger<LoggingNotificationSender> log,
        IResendEmailClient email,
        AppDbContext db,
        TimeProvider time)
    {
        _log = log;
        _email = email;
        _db = db;
        _time = time;
    }

    public Task SendOperatorApprovedAsync(User user, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY operator-approved to={Email} name={Name}", user.Email, user.Name);
        return Task.CompletedTask;
    }

    public Task SendOperatorRejectedAsync(User user, string reason, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY operator-rejected to={Email} reason={Reason}", user.Email, reason);
        return Task.CompletedTask;
    }

    public Task SendBusApprovedAsync(User operatorUser, Bus bus, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY bus-approved to={Email} bus={Reg}", operatorUser.Email, bus.RegistrationNumber);
        return Task.CompletedTask;
    }

    public Task SendBusRejectedAsync(User operatorUser, Bus bus, string reason, CancellationToken ct = default)
    {
        _log.LogInformation("NOTIFY bus-rejected to={Email} bus={Reg} reason={Reason}",
            operatorUser.Email, bus.RegistrationNumber, reason);
        return Task.CompletedTask;
    }

    public async Task SendBookingConfirmedAsync(User user, BookingDetailDto booking, byte[] pdfTicket, CancellationToken ct = default)
    {
        var subject = $"Booking confirmed — {booking.BookingCode}";
        var html = BuildBookingConfirmedHtml(user, booking);
        var result = await _email.SendAsync(
            user.Email,
            subject,
            html,
            new[] { new ResendAttachment($"ticket-{booking.BookingCode}.pdf", pdfTicket) },
            ct);

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = NotificationType.BookingConfirmed,
            Channel = NotificationChannel.Email,
            ToAddress = user.Email,
            Subject = subject,
            ResendMessageId = result.MessageId,
            Status = result.Success ? "sent" : "failed",
            Error = result.Error,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        });
        await _db.SaveChangesAsync(ct);

        if (!result.Success)
            _log.LogWarning("Booking confirmation email failed for {BookingCode}: {Error}",
                booking.BookingCode, result.Error);
    }

    private static string BuildBookingConfirmedHtml(User user, BookingDetailDto b)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,sans-serif\">");
        sb.Append($"<h2>Booking confirmed: {b.BookingCode}</h2>");
        sb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>");
        sb.Append("<p>Your ticket is attached as a PDF.</p>");
        sb.Append("<hr/>");
        sb.Append($"<p><b>Trip:</b> {System.Net.WebUtility.HtmlEncode(b.SourceCity)} → {System.Net.WebUtility.HtmlEncode(b.DestinationCity)}</p>");
        sb.Append($"<p><b>Date:</b> {b.TripDate}</p>");
        sb.Append($"<p><b>Bus:</b> {System.Net.WebUtility.HtmlEncode(b.BusName)} (Operator: {System.Net.WebUtility.HtmlEncode(b.OperatorName)})</p>");
        sb.Append($"<p><b>Time:</b> {b.DepartureTime} – {b.ArrivalTime}</p>");
        sb.Append($"<p><b>Seats:</b> {string.Join(", ", b.Seats.Select(s => System.Net.WebUtility.HtmlEncode(s.SeatNumber)))}</p>");
        sb.Append($"<p><b>Total paid:</b> ₹{b.TotalAmount:0.00}</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
