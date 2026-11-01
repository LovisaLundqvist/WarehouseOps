using Microsoft.AspNetCore.Identity;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Identity;

public sealed class IdentityAuthUserAuthenticator : IAuthUserAuthenticator
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityAuthUserAuthenticator(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        // Identity enforces account lockout, email confirmation and password hashing.
        // Checking credentials does not create an authentication cookie.
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: true);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count != 1)
        {
            return null;
        }

        var roleName = roles[0];
        if (roleName is not ("Admin" or "WarehouseStaff" or "Manager") ||
            !Enum.TryParse<UserRole>(roleName, out var role))
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Email, user.DisplayName, role);
    }
}
