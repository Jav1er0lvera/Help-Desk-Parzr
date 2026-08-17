using HelpDesk.Application.Common;
using HelpDesk.Domain.Categories;
using HelpDesk.Domain.Interfaces;
using MediatR;

namespace HelpDesk.Application.Categories;

public record CreateCategoryCommand(string Name, string? Description, Guid? PlatformId) : IRequest<CategoryDto>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _repo;
    public CreateCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.ExistsByNameAsync(name, ct))
            throw new ConflictException("Ya existe una categoría con ese nombre.");

        var category = new Category
        {
            Name = name,
            Description = cmd.Description?.Trim(),
            PlatformId = cmd.PlatformId
        };
        var created = await _repo.CreateAsync(category, ct);
        return created.ToDto();
    }
}
