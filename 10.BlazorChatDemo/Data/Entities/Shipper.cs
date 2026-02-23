using System.ComponentModel.DataAnnotations;

namespace BlazorChatDemo.Data.Entities;

public class Shipper
{
    public Guid Id { get; set; }

    [StringLength(128)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Phone { get; set; }

    public IList<Order> Orders { get; set; } = new List<Order>();
}
