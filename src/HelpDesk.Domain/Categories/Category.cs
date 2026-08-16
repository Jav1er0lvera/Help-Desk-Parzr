using HelpDesk.Domain.Common;

namespace HelpDesk.Domain.Categories;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PlatformId { get; set; }
    public bool IsDeleted { get; set; } = false;
}