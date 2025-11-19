namespace Domain.Entity;

public abstract class BaseEntity
{
    public Guid Id { get; init; }

    public BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}