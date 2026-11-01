using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IAuthUserAuthenticator
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
}
