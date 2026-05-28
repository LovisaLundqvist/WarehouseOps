namespace WarehouseOps.Domain;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public string Changes { get; set; } = string.Empty;
}