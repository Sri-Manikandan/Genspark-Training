using BusBooking.Api.Models;

namespace BusBooking.Api.Services;

public interface INotificationSender
{
    Task SendOperatorApprovedAsync(User user, CancellationToken ct = default);
    Task SendOperatorRejectedAsync(User user, string reason, CancellationToken ct = default);
    Task SendBusApprovedAsync(User operatorUser, Bus bus, CancellationToken ct = default);
    Task SendBusRejectedAsync(User operatorUser, Bus bus, string reason, CancellationToken ct = default);
}
