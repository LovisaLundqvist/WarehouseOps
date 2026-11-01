using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Integration;

public class SecurityHttpTests
{
    [Fact]
    public async Task AnonymousProductRequest_Returns401()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManagerCannotCreateProduct_Returns403()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Manager);

        using var response = await client.PostAsJsonAsync("/api/Products", new
        {
            Name = "Test product",
            Sku = "SECURITY-TEST-001",
            Category = "Test",
            Description = "Security integration test",
            Price = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.ProductServiceMock.Verify(
            service => service.CreateAsync(It.IsAny<CreateProductRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task WarehouseStaffCannotReadAuditLogs_Returns403()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.WarehouseStaff);

        using var response = await client.GetAsync("/api/AuditLogs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.AuditLogServiceMock.Verify(
            service => service.GetAllAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ManagerCanReadProducts_Returns200()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Manager);

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminCanReadAuditLogs_Returns200()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Admin);

        using var response = await client.GetAsync("/api/AuditLogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvalidLogin_Returns401()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);

        using var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            Email = "admin@warehouseops.local",
            Password = "not-the-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidDemoLogin_Returns200AndToken()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);

        using var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            Email = "admin@warehouseops.local",
            Password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        login.Should().NotBeNull();
        login!.Role.Should().Be("Admin");
        login.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SixthLoginAttemptFromSameClient_Returns429()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        var request = new
        {
            Email = "admin@warehouseops.local",
            Password = "wrong-password"
        };

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var attemptResponse = await client.PostAsJsonAsync("/api/Auth/login", request);
            attemptResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Too many login attempts");
    }

    // These endpoints must not disclose data without authentication.
    [Theory]
    [InlineData("/api/Customers")]
    [InlineData("/api/Inventory")]
    [InlineData("/api/Orders")]
    [InlineData("/api/Shipments")]
    [InlineData("/api/Incidents")]
    [InlineData("/api/Dashboard/summary")]
    [InlineData("/api/AuditLogs")]
    public async Task AnonymousRequestsToOtherProtectedEndpoints_Return401(string path)
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Manager is read-only for operational records, even with a valid JWT.
    [Theory]
    [InlineData("POST", "/api/Products")]
    [InlineData("PUT", "/api/Products/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/Products/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/Customers")]
    [InlineData("POST", "/api/Inventory")]
    [InlineData("POST", "/api/Orders")]
    [InlineData("PUT", "/api/Orders/11111111-1111-1111-1111-111111111111/status")]
    [InlineData("POST", "/api/Shipments")]
    [InlineData("PUT", "/api/Shipments/11111111-1111-1111-1111-111111111111/status")]
    [InlineData("POST", "/api/Incidents")]
    [InlineData("PUT", "/api/Incidents/11111111-1111-1111-1111-111111111111/resolve")]
    public async Task ManagerCannotModifyOperationalData_Return403(string method, string path)
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Manager);

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            // The authorization middleware must reject the request before model binding.
            request.Content = JsonContent.Create(new { Value = "security-test" });
        }

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // WarehouseStaff is authorized to work with inventory, not the product catalog.
    [Theory]
    [InlineData("POST", "/api/Products")]
    [InlineData("PUT", "/api/Products/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/Products/11111111-1111-1111-1111-111111111111")]
    public async Task WarehouseStaffCannotModifyProducts_Return403(string method, string path)
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.WarehouseStaff);

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { Value = "security-test" });
        }

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WarehouseStaffCannotReadAuditLogById_Returns403()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.WarehouseStaff);

        using var response = await client.GetAsync(
            $"/api/AuditLogs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.AuditLogServiceMock.Verify(
            service => service.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ManagerCanReadAuditLogs_Returns200()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Manager);

        using var response = await client.GetAsync("/api/AuditLogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.AuditLogServiceMock.Verify(
            service => service.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task InvalidBearerToken_Returns401()
    {
        using var factory = new SecurityTestFactory();
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-valid-jwt");

        using var response = await client.GetAsync("/api/Products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.ProductServiceMock.Verify(
            service => service.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task AdminCanCreateProduct_Returns201AndCallsServiceOnce()
    {
        using var factory = new SecurityTestFactory();
        var productId = Guid.NewGuid();
        factory.ProductServiceMock
            .Setup(service => service.CreateAsync(It.IsAny<CreateProductRequest>()))
            .ReturnsAsync(new ProductDto
            {
                Id = productId,
                Name = "Test product",
                Sku = "SECURITY-TEST-001",
                Category = "Test",
                Price = 10m,
                CreatedAt = DateTime.UtcNow
            });

        using var client = CreateClient(factory);
        SetRoleToken(client, factory, UserRole.Admin);

        using var response = await client.PostAsJsonAsync("/api/Products", new
        {
            Name = "Test product",
            Sku = "SECURITY-TEST-001",
            Category = "Test",
            Description = "Authorization integration test",
            Price = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        created.Should().NotBeNull();
        created!.Id.Should().Be(productId);
        factory.ProductServiceMock.Verify(
            service => service.CreateAsync(It.IsAny<CreateProductRequest>()),
            Times.Once);
    }

    private static HttpClient CreateClient(SecurityTestFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static void SetRoleToken(
        HttpClient client,
        SecurityTestFactory factory,
        UserRole role)
    {
        var tokenService = factory.Services.GetRequiredService<IJwtTokenService>();
        var token = tokenService.GenerateToken(
            Guid.NewGuid(),
            "security-test@warehouseops.local",
            "Security Test",
            role).Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed class SecurityTestFactory : WebApplicationFactory<Program>
    {
        private static readonly Dictionary<string, string?> TestSettings = new()
        {
            ["Jwt:SecretKey"] = "WarehouseOpsIntegrationTestSecretKey_2026_LocalOnly",
            ["Jwt:Issuer"] = "WarehouseOps.Api",
            ["Jwt:Audience"] = "WarehouseOps.Client",
            ["Database:AutoMigrate"] = "false",
            ["Database:SeedDemoData"] = "false"
        };

        public Mock<IProductService> ProductServiceMock { get; } = new();
        public Mock<IAuditLogService> AuditLogServiceMock { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program.cs reads Jwt:SecretKey before WebApplicationBuilder.Build().
            // Host configuration is available at that early startup stage.
            builder.ConfigureHostConfiguration(configuration =>
                configuration.AddInMemoryCollection(TestSettings));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Preserve the same settings for services resolved after startup.
            // No migrations, demo seeding or external SQL Server are needed.
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(TestSettings));

            builder.ConfigureTestServices(services =>
            {
                ProductServiceMock.Setup(service => service.GetAllAsync(
                        It.IsAny<string?>(), It.IsAny<string?>()))
                    .ReturnsAsync(new List<ProductDto>());

                services.RemoveAll<IProductService>();
                services.AddSingleton(ProductServiceMock.Object);

                AuditLogServiceMock.Setup(service => service.GetAllAsync())
                    .ReturnsAsync(new List<AuditLogDto>());

                services.RemoveAll<IAuditLogService>();
                services.AddSingleton(AuditLogServiceMock.Object);
            });
        }
    }
}
