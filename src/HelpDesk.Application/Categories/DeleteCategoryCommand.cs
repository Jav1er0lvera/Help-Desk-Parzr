using HelpDesk.Domain.Interfaces;
using MediatR;

namespace HelpDesk.Application.Categories;

public record DeleteCategoryCommand(Guid Id) : IRequest<bool>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryRepository _repo;
    public DeleteCategoryHandler(ICategoryRepository repo) => _repo = repo;

    public Task<bool> Handle(DeleteCategoryCommand cmd, CancellationToken ct) => _repo.SoftDeleteAsync(cmd.Id, ct);
}
