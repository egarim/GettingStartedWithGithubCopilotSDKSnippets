using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Discount { get; set; }

    public Guid? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    public Guid? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
