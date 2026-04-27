using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class OperatorRequestService : IOperatorRequestService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;

    public OperatorRequestService(AppDbContext db, IAuditLogWriter audit, INotificationSender notifier)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<OperatorRequestDto> SubmitAsync(Guid userId, BecomeOperatorRequest body, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException("UNAUTHORIZED", "User no longer exists");

        if (user.Roles.Any(r => r.Role == Roles.Operator))
            throw new BusinessRuleException("ALREADY_OPERATOR", "User already has the operator role");

        var pending = await _db.OperatorRequests
            .AnyAsync(r => r.UserId == userId && r.Status == OperatorRequestStatus.Pending, ct);
        if (pending)
            throw new BusinessRuleException("REQUEST_ALREADY_PENDING", "You already have a pending operator request");

        var req = new OperatorRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OperatorRequestStatus.Pending,
            CompanyName = body.CompanyName.Trim(),
            RequestedAt = DateTime.UtcNow
        };
        _db.OperatorRequests.Add(req);
        await _db.SaveChangesAsync(ct);

        return ToDto(req, user);
    }

    public async Task<IReadOnlyList<OperatorRequestDto>> ListAsync(string? status, CancellationToken ct)
    {
        var query = _db.OperatorRequests
            .AsNoTracking()
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (!OperatorRequestStatus.All.Contains(status))
                throw new BusinessRuleException("INVALID_STATUS", "Unknown status filter");
            query = query.Where(r => r.Status == status);
        }

        var rows = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        return rows.Select(r => ToDto(r, r.User!)).ToList();
    }

    public async Task<OperatorRequestDto> ApproveAsync(Guid adminId, Guid requestId, CancellationToken ct)
    {
        var req = await _db.OperatorRequests
            .Include(r => r.User).ThenInclude(u => u!.Roles)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("Operator request not found");

        if (req.Status != OperatorRequestStatus.Pending)
            throw new BusinessRuleException("REQUEST_NOT_PENDING", "Request is not pending");

        req.Status = OperatorRequestStatus.Approved;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewedByAdminId = adminId;

        var user = req.User!;
        if (!user.Roles.Any(r => r.Role == Roles.Operator))
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Roles.Operator });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorRequestApproved,
            "operator_request", req.Id, new { req.CompanyName }, ct);
        await _notifier.SendOperatorApprovedAsync(user, ct);

        return ToDto(req, user);
    }

    public async Task<OperatorRequestDto> RejectAsync(Guid adminId, Guid requestId, string reason, CancellationToken ct)
    {
        var req = await _db.OperatorRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("Operator request not found");

        if (req.Status != OperatorRequestStatus.Pending)
            throw new BusinessRuleException("REQUEST_NOT_PENDING", "Request is not pending");

        req.Status = OperatorRequestStatus.Rejected;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewedByAdminId = adminId;
        req.RejectReason = reason.Trim();

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.OperatorRequestRejected,
            "operator_request", req.Id, new { reason = req.RejectReason }, ct);
        await _notifier.SendOperatorRejectedAsync(req.User!, req.RejectReason, ct);

        return ToDto(req, req.User!);
    }

    private static OperatorRequestDto ToDto(OperatorRequest r, User u) => new(
        r.Id, r.UserId, u.Email, u.Name, r.CompanyName, r.Status,
        r.RequestedAt, r.ReviewedAt, r.ReviewedByAdminId, r.RejectReason);
}
