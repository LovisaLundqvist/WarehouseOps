using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WarehouseOps.Infrastructure.Identity;

public static class DemoIdentitySeeder
{
    private static readonly DemoAccount[] Accounts =
    [
        new("admin@warehouseops.local", "Admin User", "Admin", "DemoUsers:AdminPassword"),
        new("staff@warehouseops.local", "Warehouse Staff", "WarehouseStaff", "DemoUsers:WarehouseStaffPassword"),
        new("manager@warehouseops.local", "Manager User", "Manager", "DemoUsers:ManagerPassword")
    ];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        // Check secrets before making any changes to the database.
        var accountsWithPasswords = Accounts.Select(account =>
        {
            var password = configuration[account.PasswordKey];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Missing local demo password configuration: {account.PasswordKey}.");
            }

            return (Account: account, Password: password);
        }).ToArray();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Accounts.Select(account => account.Role).Distinct())
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            EnsureSucceeded(result, $"create the {roleName} role");
        }

        foreach (var (account, password) in accountsWithPasswords)
        {
            // Never silently reset credentials or elevate permissions of existing users.
            var existingUser = await userManager.FindByEmailAsync(account.Email);
            if (existingUser is not null)
            {
                if (!await userManager.IsInRoleAsync(existingUser, account.Role))
                {
                    throw new InvalidOperationException(
                        $"Existing demo account {account.Email} does not have its expected role.");
                }

                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = account.Email,
                Email = account.Email,
                DisplayName = account.DisplayName,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"create the {account.Role} demo account");

            var roleResult = await userManager.AddToRoleAsync(user, account.Role);
            EnsureSucceeded(roleResult, $"assign the {account.Role} role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        // Identity error descriptions are safe here; never include passwords.
        var errors = string.Join(", ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {operation}: {errors}");
    }

    private sealed record DemoAccount(
        string Email,
        string DisplayName,
        string Role,
        string PasswordKey);
}
