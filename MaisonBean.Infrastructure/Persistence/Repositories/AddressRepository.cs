using MaisonBean.Application.Interfaces;
using MaisonBean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaisonBean.Infrastructure.Persistence.Repositories;
public class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _context;
    public AddressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Address>>
GetByUserIdAsync(int userId, CancellationToken ct)
    {
        return await _context.Addresses
            .Where(a =>
                a.UserId == userId &&
                !a.IsDeleted
            ).ToListAsync(ct);
    }

    public async Task<Address?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Addresses.FindAsync(new object[] { id }, ct);
    }

    //add addtess
    public async Task AddAsync(Address address, CancellationToken ct)
    {
        await _context.Addresses.AddAsync(address, ct);
    }

    //update address
    public void Update(Address address)
    {
        _context.Addresses.Update(address);
    }

    //delete address
    public void Delete(Address address)
    {
        address.SoftDelete();
        _context.Addresses.Update(address);
    }
}