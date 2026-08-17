using HelpDesk.Domain.Interfaces;
using MediatR;

namespace HelpDesk.Application.Categories;

public record UpdateCategoryCommand(Guid Id, string Name, string? Description, Guid? PlatformId) : IRequest<CategoryDto?>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto?>
{
    private readonly ICategoryRepository _repo;
    public UpdateCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto?> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(cmd.Id, ct);
        if (category is null) return null;     // no existe (o está borrada) → 404

        category.Name = cmd.Name.Trim();
        category.Description = cmd.Description?.Trim();
        category.PlatformId = cmd.PlatformId;
        var updated = await _repo.UpdateAsync(category, ct);
        return updated.ToDto();
    }
}
