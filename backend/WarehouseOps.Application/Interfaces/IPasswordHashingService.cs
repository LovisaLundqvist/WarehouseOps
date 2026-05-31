namespace WarehouseOps.Application.Interfaces;

public interface IPasswordHashingService
{
    bool VerifyPassword(string password, string storedPasswordHash);
}
