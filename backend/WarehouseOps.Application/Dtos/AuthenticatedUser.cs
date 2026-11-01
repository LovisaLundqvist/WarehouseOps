using WarehouseOps.Domain;

namespace WarehouseOps.Application.Dtos;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role);
