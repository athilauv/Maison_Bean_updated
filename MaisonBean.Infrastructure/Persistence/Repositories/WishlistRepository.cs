using MaisonBean.Application.Interfaces;
using MaisonBean.Application.Wishlist.DTOs;
using MaisonBean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaisonBean.Infrastructure.Persistence.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _db;
    public WishlistRepository(AppDbContext db)
    {
        _db = db;
    }

    // GET BY USER ID
    public async Task<IEnumerable<WishlistItem>>
        GetByUserIdAsync(int userId, CancellationToken ct)
    {
        return await _db.WishlistItems
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);
    }

    // GET BY USER + PRODUCT
    public async Task<WishlistItem?>
        GetByUserAndProductAsync(int userId, int productId, CancellationToken ct)
    {
        return await _db.WishlistItems
            .FirstOrDefaultAsync(
                w => w.UserId == userId && w.ProductId == productId, ct);
    }

    // GET BY ID
    public async Task<WishlistItem?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _db.WishlistItems
            .FindAsync(new object[] { id }, ct);
    }

    // ADD
    public async Task AddAsync(WishlistItem item, CancellationToken ct)
    {
        await _db.WishlistItems.AddAsync(item, ct);
    }

    // REMOVE
    public void Remove(WishlistItem item)
    {
        _db.WishlistItems.Remove(item);
    }

    // GET WISHLIST WITH PRODUCTS
    public async Task<List<WishlistItemDto>>
        GetWishlistWithProducts(int userId, CancellationToken ct)
    {
        return await _db.WishlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .Select(w => new WishlistItemDto
            {
                WishlistId = w.Id,
                ProductId = w.ProductId,
                Name = w.Product.Name,
                Price = w.Product.Price,
                Image = w.Product.Image,
                Description = w.Product.Description,
                Category = w.Product.Category,
                HealthBenefits = w.Product.HealthBenefits,
                BaseCalories = w.Product.BaseCalories
            })
            .ToListAsync(ct);
    }
}