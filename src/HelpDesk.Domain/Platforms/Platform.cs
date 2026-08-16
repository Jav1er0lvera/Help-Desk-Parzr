using HelpDesk.Domain.Common;

namespace HelpDesk.Domain.Platforms;

public class Platform : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}