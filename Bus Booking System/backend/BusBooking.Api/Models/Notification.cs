namespace BusBooking.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public string Channel { get; set; } = NotificationChannel.Email;
    public required string ToAddress { get; set; }
    public required string Subject { get; set; }
    public string? ResendMessageId { get; set; }
    public string Status { get; set; } = "sent"; // sent | failed
    public DateTime CreatedAt { get; set; }
    public string? Error { get; set; }

    public User User { get; set; } = null!;
}

