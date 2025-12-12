namespace Application.DTO;

/// <summary>
/// Data transfer object for media information from TMDb
/// </summary>
public class MediaDTO
{
    /// <summary>
    /// TMDb ID
    /// </summary>
    public int TmdbId { get; set; }
    
    /// <summary>
    /// IMDB ID
    /// </summary>
    public string? ImdbId { get; set; }
    
    /// <summary>
    /// Kinopoisk ID
    /// </summary>
    public int? KinopoiskId { get; set; }
    
    /// <summary>
    /// Media title (localized)
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Original title
    /// </summary>
    public string OriginalTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Media type (movie or tv)
    /// </summary>
    public string MediaType { get; set; } = string.Empty;
    
    /// <summary>
    /// Release date or first air date
    /// </summary>
    public string? ReleaseDate { get; set; }
    
    /// <summary>
    /// Overview/description
    /// </summary>
    public string? Overview { get; set; }
    
    /// <summary>
    /// Poster path (relative URL)
    /// </summary>
    public string? PosterPath { get; set; }
    
    /// <summary>
    /// Backdrop path (relative URL)
    /// </summary>
    public string? BackdropPath { get; set; }
    
    /// <summary>
    /// Average vote rating
    /// </summary>
    public double VoteAverage { get; set; }
    
    /// <summary>
    /// Number of votes
    /// </summary>
    public int VoteCount { get; set; }
}
