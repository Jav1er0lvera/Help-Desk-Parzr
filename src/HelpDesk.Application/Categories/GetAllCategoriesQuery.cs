using HelpDesk.Domain.Interfaces;
using MediatR;

namespace HelpDesk.Application.Categories;

public record GetAllCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _repo;
    public GetAllCategoriesHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery query, CancellationToken ct)
    {
        var categories = await _repo.GetAllAsync(ct);     // el repo ya filtra !IsDeleted
        return categories.Select(c => c.ToDto()).ToList();
    }
}
