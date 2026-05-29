namespace WarehouseOps.Application.Dtos;

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
