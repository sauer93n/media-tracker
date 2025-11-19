using Domain.ValueObject;

namespace Domain.Entity;

public class Animation : Media
{
    public string Studio { get; private set; }
    public IEnumerable<string> Characters { get; private set; }

    public Animation(
        MediaProperties properties,
        string studio,
        IEnumerable<string> characters) : base(properties)
    {
        Studio = studio;
        Characters = characters;
    }
}