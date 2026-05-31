namespace WarehouseOps.Application.Dtos;

public class AuditLogDto
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; }

    public string Changes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
