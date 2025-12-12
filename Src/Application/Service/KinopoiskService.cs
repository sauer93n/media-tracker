using System.Text.Json;
using Application.DTO;
using Application.Interface;
using Application.Model;
using Domain.Entity;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.Find;

namespace Application.Service;

public class KinopoiskService(
    IHttpClientFactory httpClientFactory, 
    IReviewService reviewService,
    IOptions<ApplicationOptions> applicationOptions,
    ILogger<IKinopoiskService> logger,
    TMDbClient tmdbClient) : IKinopoiskService
{
    public async Task<Result<IEnumerable<KinopoiskRatingDTO>>> ImportUserRatings(string userId)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", applicationOptions.Value.KinopoiskApiKey);

        var ratings = new List<KinopoiskRatingDTO>();
        try
        {
            var page = 1;
            var response = await client.GetAsync($"{applicationOptions.Value.KinopoiskBaseUrl}/api/v1/kp_users/{userId}/votes?page={page}");
            var content = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(content).RootElement;
            root.TryGetProperty("totalPages", out var totalPagesElement);
            var totalPages = totalPagesElement.GetInt32();
            var items = root.GetProperty("items");
            var deserializedRatings = JsonSerializer.Deserialize<List<KinopoiskRatingDTO>>(items.GetRawText()); // возвращает пустые сущности внутри, нужно исправить!!!

            if (deserializedRatings != null && deserializedRatings.Any())
                ratings.AddRange(deserializedRatings);

            page++;

            while (page <= totalPages)
            {
                response = await client.GetAsync($"{applicationOptions.Value.KinopoiskBaseUrl}/api/v1/kp_users/{userId}/votes?page={page}");
                content = await response.Content.ReadAsStringAsync();
                root = JsonDocument.Parse(content).RootElement;
                items = root.GetProperty("items");

                deserializedRatings = JsonSerializer.Deserialize<List<KinopoiskRatingDTO>>(items.GetRawText());

                if (deserializedRatings == null || !deserializedRatings.Any())
                    break;

                ratings.AddRange(deserializedRatings);
                page++;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error exporting Kinopoisk ratings for user {userId}");
        }

        return Result.Ok(ratings.AsEnumerable());
    }

    /// <summary>
    /// Finds media (movie or TV show) by Kinopoisk ID using two-step lookup via IMDB ID
    /// Falls back to title-based search if IMDB ID is not available
    /// </summary>
    /// <param name="kinopoiskId">The Kinopoisk ID of the media</param>
    /// <returns>Result containing MediaDTO with TMDb and IMDB information, or error if not found</returns>
    public async Task<Result<MediaDTO>> FindMediaByKinopoiskId(int kinopoiskId)
    {
        try
        {
            // Step 1: Get IMDB ID from Kinopoisk
            logger.LogInformation("Fetching IMDB ID for Kinopoisk ID: {KinopoiskId}", kinopoiskId);
            var imdbIdResult = await GetImdbIdFromKinopoisk(kinopoiskId);
            
            if (imdbIdResult.IsFailed)
            {
                logger.LogWarning("Failed to get data from Kinopoisk for ID {KinopoiskId}: {Errors}", 
                    kinopoiskId, string.Join(", ", imdbIdResult.Errors));
                return Result.Fail(imdbIdResult.Errors);
            }
            
            var imdbId = imdbIdResult.Value;
            
            // Step 2: If IMDB ID exists, search TMDb using it
            if (!string.IsNullOrEmpty(imdbId))
            {
                logger.LogInformation("Using IMDB ID: {ImdbId} for Kinopoisk ID: {KinopoiskId}", imdbId, kinopoiskId);
                var tmdbResult = await FindMediaByImdbId(imdbId, kinopoiskId);
                
                if (tmdbResult.IsSuccess)
                {
                    return tmdbResult;
                }
                
                logger.LogWarning("Failed to find media by IMDB ID {ImdbId}, trying fallback search", imdbId);
            }
            
            // Step 3: Fallback - search by title and year from Kinopoisk data
            logger.LogInformation("Attempting fallback search by title for Kinopoisk ID: {KinopoiskId}", kinopoiskId);
            var fallbackResult = await FindMediaByKinopoiskData(kinopoiskId);
            
            return fallbackResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding media by Kinopoisk ID: {KinopoiskId}", kinopoiskId);
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts Kinopoisk ratings to reviews and optionally saves them to the database
    /// </summary>
    /// <param name="ratings">Collection of Kinopoisk ratings to convert</param>
    /// <param name="user">The user who owns these ratings</param>
    /// <param name="saveToDatabase">Whether to save the reviews to database (default: true)</param>
    /// <returns>Result containing collection of created ReviewDTOs with enriched TMDb data</returns>
    public async Task<Result<IEnumerable<ReviewDTO>>> ConvertRatingsToReviews(
        IEnumerable<KinopoiskRatingDTO> ratings, 
        User user)
    {
        var reviews = new List<ReviewDTO>();
        var errors = new List<string>();

        logger.LogInformation("Converting {Count} Kinopoisk ratings to reviews for user {UserId}", 
            ratings.Count(), user.Id);

        foreach (var rating in ratings)
        {
            try
            {
                // Step 1: Find media in TMDb using Kinopoisk ID
                logger.LogInformation("Looking up TMDb data for Kinopoisk ID: {KinopoiskId}", rating.KinopoiskId);
                var mediaResult = await FindMediaByKinopoiskId(rating.KinopoiskId);

                if (mediaResult.IsFailed)
                {
                    logger.LogWarning("Failed to find TMDb data for Kinopoisk ID {KinopoiskId}: {Errors}", 
                        rating.KinopoiskId, string.Join(", ", mediaResult.Errors));
                    errors.Add($"Kinopoisk ID {rating.KinopoiskId} ({rating.NameOriginal ?? rating.NameRu}): {string.Join(", ", mediaResult.Errors)}");
                    continue;
                }

                var media = mediaResult.Value;

                // Step 2: Determine reference type
                var referenceType = media.MediaType.ToLowerInvariant() == "movie" 
                    ? Domain.ValueObject.ReferenceType.Movie 
                    : Domain.ValueObject.ReferenceType.TV;

                // Step 3: Create review content from Kinopoisk data
                var content = GenerateReviewContent(rating, media);

                // Step 5: Create review request
                var createReviewRequest = new CreateReviewRequest
                {
                    AuthorId = user.Id,
                    AuthorName = user.Name,
                    Content = content,
                    Rating = rating.UserRating,
                    ReferenceId = media.TmdbId.ToString(),
                    ReferenceType = referenceType == Domain.ValueObject.ReferenceType.Movie 
                        ? ReferenceType.Movie 
                        : ReferenceType.TV
                };

                var reviewResult = await reviewService.CreateReviewAsync(createReviewRequest);
                
                if (reviewResult.IsSuccess)
                {
                    reviews.Add(reviewResult.Value);
                    logger.LogInformation("Successfully created review for {Title} (TMDb: {TmdbId}, Kinopoisk: {KinopoiskId})", 
                        media.Title, media.TmdbId, rating.KinopoiskId);
                }
                else
                {
                    logger.LogWarning("Failed to create review for {Title}: {Errors}", 
                        media.Title, string.Join(", ", reviewResult.Errors));
                    errors.Add($"{media.Title}: {string.Join(", ", reviewResult.Errors)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting rating for Kinopoisk ID {KinopoiskId}", rating.KinopoiskId);
                errors.Add($"Kinopoisk ID {rating.KinopoiskId}: {ex.Message}");
            }
        }

        logger.LogInformation("Successfully converted {SuccessCount}/{TotalCount} ratings to reviews. {ErrorCount} errors.", 
            reviews.Count, ratings.Count(), errors.Count);

        if (errors.Any())
        {
            logger.LogWarning("Conversion errors: {Errors}", string.Join("; ", errors));
        }

        return reviews.Any() 
            ? Result.Ok(reviews.AsEnumerable()) 
            : Result.Fail<IEnumerable<ReviewDTO>>($"Failed to convert any ratings. Errors: {string.Join("; ", errors)}");
    }

    private string GenerateReviewContent(KinopoiskRatingDTO rating, MediaDTO media)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Imported from Kinopoisk");
        sb.AppendLine();
        sb.Append("**").Append(media.Title).Append("** (").Append(media.ReleaseDate?.Split('-')[0]).AppendLine(")");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(media.Overview))
        {
            sb.AppendLine(media.Overview);
            sb.AppendLine();
        }
        
        sb.Append("My Rating: ").Append(rating.UserRating).AppendLine("/10");
        sb.Append("Kinopoisk Rating: ").Append(rating.RatingKinopoisk).AppendLine("/10");
        
        if (!string.IsNullOrEmpty(rating.ImdbId) && rating.RatingImbd > 0)
        {
            sb.Append("IMDb Rating: ").Append(rating.RatingImbd).AppendLine("/10");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.Append("*Originally rated on Kinopoisk (ID: ").Append(rating.KinopoiskId).Append(")*");

        return sb.ToString();
    }

    private async Task<Result<string?>> GetImdbIdFromKinopoisk(int kinopoiskId)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", applicationOptions.Value.KinopoiskApiKey);
            
            var response = await client.GetAsync(
                $"{applicationOptions.Value.KinopoiskBaseUrl}/api/v2.2/films/{kinopoiskId}"
            );
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Failed to fetch Kinopoisk movie {KinopoiskId}: {StatusCode}, {Content}", 
                    kinopoiskId, response.StatusCode, errorContent);
                return Result.Fail($"Failed to fetch Kinopoisk movie: {response.StatusCode}");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("imdbId", out var imdbIdElement))
            {
                var imdbId = imdbIdElement.GetString();
                if (!string.IsNullOrEmpty(imdbId))
                {
                    logger.LogInformation("Found IMDB ID: {ImdbId} for Kinopoisk ID: {KinopoiskId}", imdbId, kinopoiskId);
                    return Result.Ok<string?>(imdbId);
                }
            }
            
            // IMDB ID is missing, return success with null to trigger fallback search
            logger.LogWarning("IMDB ID not found for Kinopoisk ID: {KinopoiskId}, will try fallback search", kinopoiskId);
            return Result.Ok<string?>(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting IMDB ID from Kinopoisk: {KinopoiskId}", kinopoiskId);
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    private async Task<Result<MediaDTO>> FindMediaByImdbId(string imdbId, int kinopoiskId)
    {
        try
        {
            logger.LogInformation("Searching TMDb for IMDB ID: {ImdbId}", imdbId);
            
            // Use TMDbClient Find API to search by IMDB ID
            var findResult = await tmdbClient.FindAsync(FindExternalSource.Imdb, imdbId);
            
            if (findResult == null)
            {
                logger.LogWarning("No results found in TMDb for IMDB ID: {ImdbId}", imdbId);
                return Result.Fail($"No media found with IMDB ID: {imdbId}");
            }
            
            // Try movie_results first
            if (findResult.MovieResults != null && findResult.MovieResults.Count > 0)
            {
                var movie = findResult.MovieResults[0];
                logger.LogInformation("Found movie in TMDb: {Title} (ID: {TmdbId})", movie.Title, movie.Id);
                
                return Result.Ok(new MediaDTO
                {
                    TmdbId = movie.Id,
                    ImdbId = imdbId,
                    KinopoiskId = kinopoiskId,
                    Title = movie.Title ?? "",
                    OriginalTitle = movie.OriginalTitle ?? "",
                    MediaType = "movie",
                    ReleaseDate = movie.ReleaseDate?.ToString("yyyy-MM-dd"),
                    Overview = movie.Overview,
                    PosterPath = movie.PosterPath,
                    BackdropPath = movie.BackdropPath,
                    VoteAverage = movie.VoteAverage,
                    VoteCount = movie.VoteCount
                });
            }
            
            // Try tv_results
            if (findResult.TvResults != null && findResult.TvResults.Count > 0)
            {
                var tv = findResult.TvResults[0];
                logger.LogInformation("Found TV show in TMDb: {Name} (ID: {TmdbId})", tv.Name, tv.Id);
                
                return Result.Ok(new MediaDTO
                {
                    TmdbId = tv.Id,
                    ImdbId = imdbId,
                    KinopoiskId = kinopoiskId,
                    Title = tv.Name ?? "",
                    OriginalTitle = tv.OriginalName ?? "",
                    MediaType = "tv",
                    ReleaseDate = tv.FirstAirDate?.ToString("yyyy-MM-dd"),
                    Overview = tv.Overview,
                    PosterPath = tv.PosterPath,
                    BackdropPath = tv.BackdropPath,
                    VoteAverage = tv.VoteAverage,
                    VoteCount = tv.VoteCount
                });
            }
            
            logger.LogWarning("No media found in TMDb for IMDB ID: {ImdbId}", imdbId);
            return Result.Fail($"No media found with IMDB ID: {imdbId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding media by IMDB ID: {ImdbId}", imdbId);
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    private async Task<Result<MediaDTO>> FindMediaByKinopoiskData(int kinopoiskId)
    {
        try
        {
            // Fetch full data from Kinopoisk API
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", applicationOptions.Value.KinopoiskApiKey);
            
            var response = await client.GetAsync(
                $"{applicationOptions.Value.KinopoiskBaseUrl}/api/v2.2/films/{kinopoiskId}"
            );
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch Kinopoisk data for fallback search: {KinopoiskId}", kinopoiskId);
                return Result.Fail($"Could not fetch data from Kinopoisk for ID: {kinopoiskId}");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            // Extract title and year
            var nameRu = root.TryGetProperty("nameRu", out var nameRuElement) ? nameRuElement.GetString() : null;
            var nameOriginal = root.TryGetProperty("nameOriginal", out var nameOrigElement) ? nameOrigElement.GetString() : null;
            var year = root.TryGetProperty("year", out var yearElement) ? yearElement.GetInt32() : (int?)null;
            var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            
            var searchTitle = nameOriginal ?? nameRu;
            if (string.IsNullOrEmpty(searchTitle))
            {
                logger.LogWarning("No title found in Kinopoisk data for ID: {KinopoiskId}", kinopoiskId);
                return Result.Fail($"No title found for Kinopoisk ID: {kinopoiskId}");
            }
            
            logger.LogInformation("Searching TMDb for title: {Title}, year: {Year}, type: {Type}", searchTitle, year, type);
            
            // Determine if it's a movie or TV show based on type
            var isMovie = type?.ToLowerInvariant() == "film" || type?.ToLowerInvariant() == "movie";
            
            if (isMovie)
            {
                // Search movies
                var movieSearch = await tmdbClient.SearchMovieAsync(searchTitle, year: year ?? 0);
                
                if (movieSearch?.Results != null && movieSearch.Results.Count > 0)
                {
                    var movie = movieSearch.Results[0];
                    logger.LogInformation("Found movie via fallback search: {Title} (ID: {TmdbId})", movie.Title, movie.Id);
                    
                    return Result.Ok(new MediaDTO
                    {
                        TmdbId = movie.Id,
                        ImdbId = null, // Not available from this search
                        KinopoiskId = kinopoiskId,
                        Title = movie.Title ?? "",
                        OriginalTitle = movie.OriginalTitle ?? "",
                        MediaType = "movie",
                        ReleaseDate = movie.ReleaseDate?.ToString("yyyy-MM-dd"),
                        Overview = movie.Overview,
                        PosterPath = movie.PosterPath,
                        BackdropPath = movie.BackdropPath,
                        VoteAverage = movie.VoteAverage,
                        VoteCount = movie.VoteCount
                    });
                }
            }
            else
            {
                // Search TV shows
                var tvSearch = await tmdbClient.SearchTvShowAsync(searchTitle, firstAirDateYear: year ?? 0);
                
                if (tvSearch?.Results != null && tvSearch.Results.Count > 0)
                {
                    var tv = tvSearch.Results[0];
                    logger.LogInformation("Found TV show via fallback search: {Name} (ID: {TmdbId})", tv.Name, tv.Id);
                    
                    return Result.Ok(new MediaDTO
                    {
                        TmdbId = tv.Id,
                        ImdbId = null, // Not available from this search
                        KinopoiskId = kinopoiskId,
                        Title = tv.Name ?? "",
                        OriginalTitle = tv.OriginalName ?? "",
                        MediaType = "tv",
                        ReleaseDate = tv.FirstAirDate?.ToString("yyyy-MM-dd"),
                        Overview = tv.Overview,
                        PosterPath = tv.PosterPath,
                        BackdropPath = tv.BackdropPath,
                        VoteAverage = tv.VoteAverage,
                        VoteCount = tv.VoteCount
                    });
                }
            }
            
            logger.LogWarning("No match found in TMDb for Kinopoisk title: {Title}", searchTitle);
            return Result.Fail($"No TMDb match found for: {searchTitle}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in fallback search for Kinopoisk ID: {KinopoiskId}", kinopoiskId);
            return Result.Fail($"Fallback search error: {ex.Message}");
        }
    }
}