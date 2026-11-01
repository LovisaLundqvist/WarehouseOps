using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Tests.Integration;

public class JwtValidationHttpTests
{
    private const string ValidSecretKey = "WarehouseOpsIntegrationTestSecretKey_2026_LocalOnly";
    private const string OtherSecretKey = "OtherIntegrationTestSecretKey_2026_LocalOnly123";
    private const string ValidIssuer = "WarehouseOps.Api";
    private const string ValidAudience = "WarehouseOps.Client";

    [Theory]
    [InlineData("Untrusted.Issuer", ValidAudience, false)]
    [InlineData(ValidIssuer, "Untrusted.Client", false)]
    [InlineData(ValidIssuer, ValidAudience, true)]
    public async Task InvalidIssuerAudienceOrSigningKey_Returns401(
        string issuer,
        string audience,
        bool useOtherKey)
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        SetBearerToken(client, CreateJwt(
            issuer: issuer,
            audience: audience,
            signingKey: useOtherKey ? OtherSecretKey : ValidSecretKey));

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        VerifyProductsWereNotRead(factory);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        var now = DateTime.UtcNow;
        SetBearerToken(client, CreateJwt(
            notBefore: now.AddMinutes(-20),
            expires: now.AddMinutes(-10)));

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        VerifyProductsWereNotRead(factory);
    }

    [Fact]
    public async Task TokenNotYetValid_Returns401()
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        var now = DateTime.UtcNow;
        SetBearerToken(client, CreateJwt(
            notBefore: now.AddMinutes(10),
            expires: now.AddMinutes(20)));

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        VerifyProductsWereNotRead(factory);
    }

    [Fact]
    public async Task UnsignedToken_Returns401()
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        SetBearerToken(client, CreateJwt(signToken: false));

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        VerifyProductsWereNotRead(factory);
    }

    [Fact]
    public async Task ValidTokenWithoutRole_Returns403()
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        SetBearerToken(client, CreateJwt(includeRole: false));

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        VerifyProductsWereNotRead(factory);
    }

    [Fact]
    public async Task ValidSignedAdminToken_AllowsRead()
    {
        using var factory = new JwtSecurityTestFactory();
        using var client = CreateClient(factory);
        SetBearerToken(client, CreateJwt());

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.ProductServiceMock.Verify(
            service => service.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);
    }

    private static string CreateJwt(
        string issuer = ValidIssuer,
        string audience = ValidAudience,
        string signingKey = ValidSecretKey,
        DateTime? notBefore = null,
        DateTime? expires = null,
        bool includeRole = true,
        bool signToken = true)
    {
        var now = DateTime.UtcNow;
        var claims = includeRole
            ? new[] { new Claim(ClaimTypes.Role, "Admin") }
            : Array.Empty<Claim>();

        var credentials = signToken
            ? new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256)
            : null;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore ?? now.AddMinutes(-1),
            expires: expires ?? now.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpClient CreateClient(JwtSecurityTestFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static void SetBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static void VerifyProductsWereNotRead(JwtSecurityTestFactory factory)
    {
        factory.ProductServiceMock.Verify(
            service => service.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    private sealed class JwtSecurityTestFactory : WebApplicationFactory<Program>
    {
        private static readonly Dictionary<string, string?> TestSettings = new()
        {
            ["Jwt:SecretKey"] = ValidSecretKey,
            ["Jwt:Issuer"] = ValidIssuer,
            ["Jwt:Audience"] = ValidAudience,
            ["Database:AutoMigrate"] = "false",
            ["Database:SeedDemoData"] = "false"
        };

        public Mock<IProductService> ProductServiceMock { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration =>
                configuration.AddInMemoryCollection(TestSettings));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(TestSettings));

            builder.ConfigureTestServices(services =>
            {
                ProductServiceMock.Setup(service => service.GetAllAsync(
                        It.IsAny<string?>(), It.IsAny<string?>()))
                    .ReturnsAsync(new List<ProductDto>());

                services.RemoveAll<IProductService>();
                services.AddSingleton(ProductServiceMock.Object);
            });
        }
    }
}
