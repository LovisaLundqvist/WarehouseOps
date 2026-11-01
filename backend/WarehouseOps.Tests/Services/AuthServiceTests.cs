using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidIdentityCredentials_ReturnsJwtResponse()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(90);
        var identity = new Mock<IAuthUserAuthenticator>();
        identity.Setup(service => service.AuthenticateAsync("admin@warehouseops.local", "SecurePassword123!"))
            .ReturnsAsync(new AuthenticatedUser(userId, "admin@warehouseops.local", "Admin User", UserRole.Admin));

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(service => service.GenerateToken(
                userId, "admin@warehouseops.local", "Admin User", UserRole.Admin))
            .Returns(("test.jwt.token", expiresAt));

        var service = new AuthService(identity.Object, jwt.Object);
        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "  ADMIN@WarehouseOps.Local  ",
            Password = "SecurePassword123!"
        });

        response.Token.Should().Be("test.jwt.token");
        response.ExpiresAt.Should().Be(expiresAt);
        response.Email.Should().Be("admin@warehouseops.local");
        response.Role.Should().Be("Admin");
        identity.Verify(auth => auth.AuthenticateAsync("admin@warehouseops.local", "SecurePassword123!"), Times.Once);
    }

    [Fact]
    public async Task WrongCredentials_DoNotIssueJwt()
    {
        var identity = new Mock<IAuthUserAuthenticator>();
        identity.Setup(service => service.AuthenticateAsync("admin@warehouseops.local", "wrong"))
            .ReturnsAsync((AuthenticatedUser?)null);
        var jwt = new Mock<IJwtTokenService>();
        var service = new AuthService(identity.Object, jwt.Object);

        Func<Task> action = () => service.LoginAsync(new LoginRequest
        {
            Email = "admin@warehouseops.local", Password = "wrong"
        });

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
        jwt.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("", "test")]
    [InlineData("admin@warehouseops.local", "")]
    [InlineData("  ", "test")]
    public async Task MissingCredentials_AreRejectedBeforeIdentityLookup(string email, string password)
    {
        var identity = new Mock<IAuthUserAuthenticator>();
        var jwt = new Mock<IJwtTokenService>();
        var service = new AuthService(identity.Object, jwt.Object);

        Func<Task> action = () => service.LoginAsync(new LoginRequest
        {
            Email = email, Password = password
        });

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        identity.VerifyNoOtherCalls();
        jwt.VerifyNoOtherCalls();
    }
}
