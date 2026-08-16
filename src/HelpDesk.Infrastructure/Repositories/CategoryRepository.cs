using HelpDesk.Domain.Categories;
using HelpDesk.Domain.Interfaces;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly HelpDeskDbContext _db;

    public CategoryRepository(HelpDeskDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _db.Categories
            .Where(c => !c.IsDeleted)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _db.Categories.AnyAsync(c => c.Name == name && !c.IsDeleted);
    }
}