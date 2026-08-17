using HelpDesk.Domain.Interfaces;
using MediatR;

namespace HelpDesk.Application.Categories;

public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto?>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly ICategoryRepository _repo;
    public GetCategoryByIdHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery query, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(query.Id, ct);
        return category?.ToDto();     // null → el controlador responde 404
    }
}
