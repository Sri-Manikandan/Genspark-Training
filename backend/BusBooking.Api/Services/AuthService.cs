using BusBooking.Api.Dtos;
using BusBooking.Api.Infrastructure;
using BusBooking.Api.Infrastructure.Auth;
using BusBooking.Api.Infrastructure.Errors;
using BusBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;

    public AuthService(AppDbContext db, IPasswordHasher hasher, IJwtTokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            throw new ConflictException("EMAIL_IN_USE", "Email is already registered");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        user.Roles.Add(new UserRole { UserId = user.Id, Role = Roles.Customer });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.Name, user.Email, user.Phone, new[] { Roles.Customer });
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("INVALID_CREDENTIALS");

        var roleNames = user.Roles.Select(r => r.Role).ToArray();
        var token = _tokens.Generate(user, roleNames);

        var dto = new UserDto(user.Id, user.Name, user.Email, user.Phone, roleNames);
        return new LoginResponse(token.Token, token.ExpiresAtUtc, dto);
    }
}
