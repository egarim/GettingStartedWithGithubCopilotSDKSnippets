using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class EmployeeTerritory
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid TerritoryId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(TerritoryId))]
    public Territory Territory { get; set; } = null!;
}
