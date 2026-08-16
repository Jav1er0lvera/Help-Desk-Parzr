namespace HelpDesk.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } //Genera automáticamente un identificador único
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow; //Guarda la fecha y hora exacta en que se creó el objeto (hora universal)
        UpdatedAt = DateTime.UtcNow;
    }

}
