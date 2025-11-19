using Domain.ValueObject;

namespace Domain.Entity;

public abstract class Media : BaseEntity
{
    public MediaProperties Properties { get; private set; }

    public Media(
        MediaProperties mediaProperties) : base()
    {
        Properties = mediaProperties;
    }
}