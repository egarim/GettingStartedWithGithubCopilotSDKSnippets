using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class Employee
{
    public Guid Id { get; set; }

    [StringLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(64)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? Title { get; set; }

    public DateTime? HireDate { get; set; }

    [StringLength(128)]
    public string? Email { get; set; }

    [StringLength(32)]
    public string? Phone { get; set; }

    public Guid? ReportsToId { get; set; }

    [ForeignKey(nameof(ReportsToId))]
    public Employee? ReportsTo { get; set; }

    public IList<Employee> DirectReports { get; set; } = new List<Employee>();

    public IList<EmployeeTerritory> Territories { get; set; } = new List<EmployeeTerritory>();

    public IList<Order> Orders { get; set; } = new List<Order>();

    [NotMapped]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
