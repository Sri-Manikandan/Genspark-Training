using BusBooking.Api.Models;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api.Services;

// M3 stub. M5 replaces with Resend-backed implementation.
public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _log;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> log)
    {
        _log = log;
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
}
