namespace WarehouseOps.Application.Dtos;

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = "Medium";

    public string RelatedEntityType { get; set; } = "General";

    public string RelatedEntityId { get; set; } = string.Empty;
}
