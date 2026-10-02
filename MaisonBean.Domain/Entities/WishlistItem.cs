using System.ComponentModel.DataAnnotations.Schema;

namespace MaisonBean.Domain.Entities;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; }
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}