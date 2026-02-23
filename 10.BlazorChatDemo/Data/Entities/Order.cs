using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class Order
{
    public Guid Id { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public DateTime? ShippedDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Freight { get; set; }

    [StringLength(256)]
    public string? ShipAddress { get; set; }

    [StringLength(64)]
    public string? ShipCity { get; set; }

    [StringLength(64)]
    public string? ShipCountry { get; set; }

    public OrderStatus Status { get; set; }

    public Guid? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    public Guid? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    public Guid? ShipperId { get; set; }

    [ForeignKey(nameof(ShipperId))]
    public Shipper? Shipper { get; set; }

    public Guid? InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice? Invoice { get; set; }

    public IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
