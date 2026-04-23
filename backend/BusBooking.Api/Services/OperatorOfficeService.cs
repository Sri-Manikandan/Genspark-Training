using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class OperatorOfficeService : IOperatorOfficeService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogWriter _audit;

    public OperatorOfficeService(AppDbContext db, IAuditLogWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<OperatorOfficeDto>> ListAsync(Guid operatorUserId, CancellationToken ct)
    {
        return await _db.OperatorOffices
            .AsNoTracking()
            .Where(o => o.OperatorUserId == operatorUserId && o.IsActive)
            .Include(o => o.City)
            .OrderBy(o => o.City!.Name)
            .Select(o => new OperatorOfficeDto(
                o.Id, o.CityId, o.City!.Name, o.AddressLine, o.Phone, o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<OperatorOfficeDto> CreateAsync(
        Guid operatorUserId, CreateOperatorOfficeRequest body, CancellationToken ct)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == body.CityId, ct)
            ?? throw new NotFoundException("City not found");
        if (!city.IsActive)
            throw new BusinessRuleException("CITY_INACTIVE", "City is not active");

        var existing = await _db.OperatorOffices
            .FirstOrDefaultAsync(o => o.OperatorUserId == operatorUserId && o.CityId == body.CityId, ct);
        if (existing != null)
        {
            if (existing.IsActive)
                throw new ConflictException("OFFICE_ALREADY_EXISTS",
                    "An office for this city already exists");
            existing.IsActive = true;
            existing.AddressLine = body.AddressLine.Trim();
            existing.Phone = body.Phone.Trim();
            await _db.SaveChangesAsync(ct);
            await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeCreated,
                "operator_office", existing.Id, new { cityId = city.Id, reactivated = true }, ct);
            return new OperatorOfficeDto(existing.Id, city.Id, city.Name,
                existing.AddressLine, existing.Phone, existing.IsActive);
        }

        var office = new OperatorOffice
        {
            Id = Guid.NewGuid(),
            OperatorUserId = operatorUserId,
            CityId = body.CityId,
            AddressLine = body.AddressLine.Trim(),
            Phone = body.Phone.Trim(),
            IsActive = true
        };
        _db.OperatorOffices.Add(office);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeCreated,
            "operator_office", office.Id, new { cityId = city.Id }, ct);

        return new OperatorOfficeDto(office.Id, city.Id, city.Name,
            office.AddressLine, office.Phone, office.IsActive);
    }

    public async Task DeleteAsync(Guid operatorUserId, Guid officeId, CancellationToken ct)
    {
        var office = await _db.OperatorOffices.FirstOrDefaultAsync(o => o.Id == officeId, ct)
            ?? throw new NotFoundException("Office not found");
        if (office.OperatorUserId != operatorUserId)
            throw new ForbiddenException("Cannot delete another operator's office");

        office.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(operatorUserId, AuditAction.OperatorOfficeDeleted,
            "operator_office", office.Id, null, ct);
    }
}
