using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class BusService : IBusService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;
    private readonly INotificationSender _notifier;

    public BusService(AppDbContext db, IAuditLogWriter audit, INotificationSender notifier)
    {
        _db = db;
        _audit = audit;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<BusDto>> ListForOperatorAsync(Guid operatorUserId, CancellationToken ct)
    {
        return await _db.Buses.AsNoTracking()
            .Where(b => b.OperatorUserId == operatorUserId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => ToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BusDto> CreateAsync(Guid operatorUserId, CreateBusRequest body, CancellationToken ct)
    {
        var reg = body.RegistrationNumber.Trim();
        if (await _db.Buses.AnyAsync(b => b.RegistrationNumber == reg, ct))
            throw new ConflictException("REGISTRATION_TAKEN",
                "A bus with that registration number already exists");

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            RegistrationNumber = reg,
            BusName = body.BusName.Trim(),
            BusType = body.BusType,
            Capacity = body.Rows * body.Columns,
            ApprovalStatus = BusApprovalStatus.Pending,
            OperationalStatus = BusOperationalStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var seats = SeatLayoutGenerator.Generate(bus.Id, body.Rows, body.Columns);
        foreach (var s in seats) bus.Seats.Add(s);

        _db.Buses.Add(bus);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusCreated,
            "bus", bus.Id,
            new { bus.RegistrationNumber, bus.BusName, bus.BusType, bus.Capacity }, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> UpdateOperationalStatusAsync(
        Guid operatorUserId, Guid busId, string newStatus, CancellationToken ct)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot modify another operator's bus");
        if (bus.OperationalStatus == BusOperationalStatus.Retired)
            throw new BusinessRuleException("BUS_RETIRED", "Retired buses cannot change status");

        bus.OperationalStatus = newStatus;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusStatusChanged,
            "bus", bus.Id, new { newStatus }, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> RetireAsync(Guid operatorUserId, Guid busId, CancellationToken ct)
    {
        var bus = await _db.Buses.FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot retire another operator's bus");

        bus.OperationalStatus = BusOperationalStatus.Retired;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.BusStatusChanged,
            "bus", bus.Id, new { newStatus = BusOperationalStatus.Retired }, ct);

        return ToDto(bus);
    }

    public async Task<IReadOnlyList<BusDto>> ListByApprovalStatusAsync(string? status, CancellationToken ct)
    {
        var query = _db.Buses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status))
        {
            if (!BusApprovalStatus.All.Contains(status))
                throw new BusinessRuleException("INVALID_STATUS", "Unknown approval status filter");
            query = query.Where(b => b.ApprovalStatus == status);
        }
        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => ToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BusDto> ApproveAsync(Guid adminId, Guid busId, CancellationToken ct)
    {
        var bus = await _db.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.ApprovalStatus != BusApprovalStatus.Pending)
            throw new BusinessRuleException("BUS_NOT_PENDING", "Bus is not pending approval");

        bus.ApprovalStatus = BusApprovalStatus.Approved;
        bus.ApprovedAt = DateTime.UtcNow;
        bus.ApprovedByAdminId = adminId;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.BusApproved,
            "bus", bus.Id, new { bus.RegistrationNumber }, ct);
        if (bus.Operator is not null)
            await _notifier.SendBusApprovedAsync(bus.Operator, bus, ct);

        return ToDto(bus);
    }

    public async Task<BusDto> RejectAsync(Guid adminId, Guid busId, string reason, CancellationToken ct)
    {
        var bus = await _db.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == busId, ct)
            ?? throw new NotFoundException("Bus not found");
        if (bus.ApprovalStatus != BusApprovalStatus.Pending)
            throw new BusinessRuleException("BUS_NOT_PENDING", "Bus is not pending approval");

        bus.ApprovalStatus = BusApprovalStatus.Rejected;
        bus.RejectReason = reason.Trim();
        bus.ApprovedAt = DateTime.UtcNow;
        bus.ApprovedByAdminId = adminId;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(adminId, AuditAction.BusRejected,
            "bus", bus.Id, new { reason = bus.RejectReason }, ct);
        if (bus.Operator is not null)
            await _notifier.SendBusRejectedAsync(bus.Operator, bus, bus.RejectReason, ct);

        return ToDto(bus);
    }

    private static BusDto ToDto(Bus b) => new(
        b.Id, b.OperatorUserId, b.RegistrationNumber, b.BusName, b.BusType,
        b.Capacity, b.ApprovalStatus, b.OperationalStatus,
        b.CreatedAt, b.ApprovedAt, b.RejectReason);
}
