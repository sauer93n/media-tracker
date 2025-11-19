namespace Domain.ValueObject;

public record struct MediaProperties
{
    public double Rating { get; init; }
    public int ReleaseYear { get; init; }
    public string Title { get; init; }

    public MediaProperties(double rating, int releaseYear, string title)
    {
        Rating = rating;
        ReleaseYear = releaseYear;
        Title = title;
    }
}