using MaisonBean.Application.Interfaces;
using MaisonBean.Application.Wishlist.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

[ApiController]
[Authorize(Roles = "CUSTOMER")]
[EnableRateLimiting("wishlist")]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _wishlist;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public WishlistController(IWishlistRepository wishlist, IUnitOfWork uow, IMediator mediator)
    {
        _wishlist = wishlist;
        _uow = uow;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("id"); 
        if (!int.TryParse(userIdClaim, out var userId)) 
        {
            return Unauthorized(); 
        }
        var items = await _wishlist.GetWishlistWithProducts(userId, ct);
        return Ok(items);
    }

    // Toggle
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(
        [FromBody] ToggleWishlistCommand cmd, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(userIdClaim, out var userId)) 
        {
            return Unauthorized(); 
        }
        cmd.UserId = userId;
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }


    [HttpDelete("remove/{id}")]
    public async Task<IActionResult> Remove(int id, CancellationToken ct )
    {
        var userIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var item = await _wishlist.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound();

        if (item.UserId != userId)
            return Forbid();

        _wishlist.Remove(item);
        await _uow.SaveChangesAsync(ct);
        return Ok(new
        {
            message = "Removed"
        });
    }


    [HttpDelete("clear")]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("id");
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(); 
        }
        var items = await _wishlist.GetByUserIdAsync(userId, ct);
        foreach (var item in items)
        {
            _wishlist.Remove(item);
        }

        await _uow.SaveChangesAsync(ct);
        return Ok(new
        {
            message = "Wishlist cleared"
        });
    }
}
