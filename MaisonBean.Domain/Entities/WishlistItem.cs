using MaisonBean.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public class WishlistItem
{
    public int Id { get; set; }
    // FOREIGN KEYS
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; }
    // NAVIGATION
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}  