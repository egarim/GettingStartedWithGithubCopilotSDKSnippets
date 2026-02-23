using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorChatDemo.Data.Entities;

public class Invoice
{
    public Guid Id { get; set; }

    [StringLength(32)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public IList<Order> Orders { get; set; } = new List<Order>();

    [NotMapped]
    public decimal TotalAmount
    {
        get
        {
            decimal total = 0m;
            foreach (var order in Orders)
            {
                if (order?.OrderItems == null) continue;
                foreach (var item in order.OrderItems)
                {
                    var line = item.UnitPrice * item.Quantity;
                    var discountFactor = 1m - (item.Discount / 100m);
                    total += line * discountFactor;
                }
            }
            return total;
        }
    }
}
