using HelpDesk.Domain.Categories;
using HelpDesk.Domain.Interfaces;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly HelpDeskDbContext _db;

    public CategoryRepository(HelpDeskDbContext db) => _db = db;

    public async Task<List<Category>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Categories.Where(c => !c.IsDeleted).ToListAsync(ct);

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task<Category> UpdateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await _db.Categories.AnyAsync(c => c.Name == name && !c.IsDeleted, ct);

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
        if (category is null) return false;

        category.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}