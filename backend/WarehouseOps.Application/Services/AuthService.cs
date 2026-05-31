using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class AuthService : IAuthService
{
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;

    private static readonly List<AuthUser> Users =
    [
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "admin@warehouseops.local",
            "Admin User",
            UserRole.Admin,
            "PBKDF2$100000$rLkLPXxznm0BwCoT0D3heA==$Sf1cNZjezy17jE2b034AnAtANPWxpOw7YazCoVw2Y98="),

        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "staff@warehouseops.local",
            "Warehouse Staff",
            UserRole.WarehouseStaff,
            "PBKDF2$100000$nNnlH46AF5Gtb1Gpavc4eQ==$koXDbOS144OcqQOwazGL6lQeczNpXNBnN2m9kxgqVYg="),

        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "manager@warehouseops.local",
            "Manager User",
            UserRole.Manager,
            "PBKDF2$100000$ZVySmSb1g9fL9xxfy4pmlA==$J88zRczKLOXCsDF5eQarq8l6fcuoXqoKrqIpUdMK8KQ=")
    ];

    public AuthService(
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService)
    {
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = Users.FirstOrDefault(currentUser =>
            currentUser.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var passwordIsValid = _passwordHashingService.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!passwordIsValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var tokenResult = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role);

        var response = new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };

        return Task.FromResult(response);
    }

    private sealed record AuthUser(
        Guid Id,
        string Email,
        string DisplayName,
        UserRole Role,
        string PasswordHash);
}
