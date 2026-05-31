namespace WarehouseOps.Api.Security;

public static class RoleNames
{
    public const string Admin = "Admin";

    public const string WarehouseStaff = "WarehouseStaff";

    public const string Manager = "Manager";

    public const string AllRoles = Admin + "," + WarehouseStaff + "," + Manager;

    public const string AdminOrWarehouseStaff = Admin + "," + WarehouseStaff;

    public const string AdminOrManager = Admin + "," + Manager;
}
