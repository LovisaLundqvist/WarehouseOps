using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthUserAuthenticator _authenticator;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IAuthUserAuthenticator authenticator,
        IJwtTokenService jwtTokenService)
    {
        _authenticator = authenticator;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _authenticator.AuthenticateAsync(email, request.Password);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role);

        return new LoginResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };
    }
}
