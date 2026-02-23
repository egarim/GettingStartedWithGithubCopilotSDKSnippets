using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class Territory
{
    public Guid Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public Guid? RegionId { get; set; }

    [ForeignKey(nameof(RegionId))]
    public Region? Region { get; set; }

    public IList<EmployeeTerritory> EmployeeTerritories { get; set; } = new List<EmployeeTerritory>();
}
