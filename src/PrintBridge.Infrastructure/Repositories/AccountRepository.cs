using Microsoft.EntityFrameworkCore;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Interfaces;
using PrintBridge.Infrastructure.Data;

namespace PrintBridge.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly PrintBridgeDbContext _db;

    public AccountRepository(PrintBridgeDbContext db) => _db = db;

    public async Task<Account?> GetByIdAsync(int id) =>
        await _db.Set<Account>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Account>> GetAllAsync() =>
        await _db.Set<Account>().AsNoTracking().ToListAsync();

    public async Task<Account> AddAsync(Account entity)
    {
        _db.Set<Account>().Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Account entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _db.Set<Account>().Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Set<Account>().FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            await UpdateAsync(entity);
        }
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _db.Set<Account>().AnyAsync(a => a.Id == id);

    public async Task<Account?> GetByUsernameAsync(string username) =>
        await _db.Set<Account>().FirstOrDefaultAsync(a => a.Username == username);

    public async Task<Account?> GetByEmailAsync(string email) =>
        await _db.Set<Account>().FirstOrDefaultAsync(a => a.Email == email);
}
