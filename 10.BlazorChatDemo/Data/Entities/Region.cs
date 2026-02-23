using System.ComponentModel.DataAnnotations;

namespace BlazorChatDemo.Data.Entities;

public class Region
{
    public Guid Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public IList<Territory> Territories { get; set; } = new List<Territory>();
}
