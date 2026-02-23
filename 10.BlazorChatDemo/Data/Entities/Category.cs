using System.ComponentModel.DataAnnotations;

namespace BlazorChatDemo.Data.Entities;

public class Category
{
    public Guid Id { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Description { get; set; }

    public IList<Product> Products { get; set; } = new List<Product>();
}
