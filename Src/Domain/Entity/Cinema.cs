using Domain.ValueObject;

namespace Domain.Entity;

public class Cinema : Media
{
    public Person Director { get; private set; }
    public IEnumerable<Person> Cast { get; private set; }

    public Cinema(
        MediaProperties mediaProperties,
        Person director,
        IEnumerable<Person> cast
    ) : base(mediaProperties)
    {
        Director = director;
        Cast = cast;
    }
}