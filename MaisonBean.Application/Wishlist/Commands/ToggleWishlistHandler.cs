using MaisonBean.Application.Interfaces;
using MaisonBean.Domain.Entities;
using MediatR;

namespace MaisonBean.Application.Wishlist.Commands;

public class ToggleWishlistHandler : IRequestHandler<ToggleWishlistCommand, WishlistResult>
{
    private readonly IWishlistRepository _wishlist;
    private readonly IUnitOfWork _uow;

    public ToggleWishlistHandler(IWishlistRepository wishlist, IUnitOfWork uow)
    {
        _wishlist = wishlist;
        _uow = uow;
    }

    public async Task<WishlistResult> Handle(ToggleWishlistCommand request, CancellationToken ct)
    {
        var existing = await _wishlist
            .GetByUserAndProductAsync(request.UserId, request.ProductId, ct);

        // REMOVE IF EXISTS
        if (existing != null)
        {
            _wishlist.Remove(existing);
            await _uow.SaveChangesAsync(ct);

            return new WishlistResult
            {
                IsAdded = false,
                Message = "Removed from wishlist"
            };
        }

        // CREATE WISHLIST ITEM
        var item = new WishlistItem
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            AddedAt = DateTime.UtcNow
        };

        await _wishlist.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);

        return new WishlistResult
        {
            IsAdded = true,
            Message = "Successfully added to wishlist"
        };
    }
}