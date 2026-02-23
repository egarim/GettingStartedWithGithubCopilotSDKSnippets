using System.ComponentModel.DataAnnotations;

namespace BlazorChatDemo.Data.Entities;

public class Customer
{
    public Guid Id { get; set; }

    [StringLength(128)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? ContactName { get; set; }

    [StringLength(32)]
    public string? Phone { get; set; }

    [StringLength(128)]
    public string? Email { get; set; }

    [StringLength(256)]
    public string? Address { get; set; }

    [StringLength(64)]
    public string? City { get; set; }

    [StringLength(64)]
    public string? Country { get; set; }

    public IList<Order> Orders { get; set; } = new List<Order>();
}
