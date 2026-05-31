using System.Security.Cryptography;
using System.Text;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

public class PasswordHashingService : IPasswordHashingService
{
    public bool VerifyPassword(string password, string storedPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedPasswordHash))
        {
            return false;
        }

        var parts = storedPasswordHash.Split('$');

        if (parts.Length != 4 || parts[0] != "PBKDF2")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
