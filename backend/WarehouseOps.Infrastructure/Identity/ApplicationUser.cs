using Microsoft.AspNetCore.Identity;

namespace WarehouseOps.Infrastructure.Identity;

// Authentication and user persistence belong to Infrastructure.
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
