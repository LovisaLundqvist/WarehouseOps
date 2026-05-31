using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
