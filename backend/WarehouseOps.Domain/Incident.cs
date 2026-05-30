namespace WarehouseOps.Domain;

public class Incident : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

    public IncidentRelatedEntityType RelatedEntityType { get; set; } = IncidentRelatedEntityType.General;

    public string RelatedEntityId { get; set; } = string.Empty;

    public IncidentStatus Status { get; set; }

    public string ResolutionNotes { get; set; } = string.Empty;

    public DateTime? ClosedAt { get; set; }
}
