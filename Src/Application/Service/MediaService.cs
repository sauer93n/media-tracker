using Application.DTO;
using Application.Interface;
using Application.Model;
using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Options;
using TMDbLib.Client;

namespace Application.Service;

public class MediaService: IMediaService
{
    private readonly ApplicationOptions applicationOptions;
    private readonly TMDbClient tMDbClient;
    private readonly IMapper mapper;

    public MediaService(IOptions<ApplicationOptions> applicationOptions, IMapper mapper)
    {
        this.applicationOptions = applicationOptions.Value;
        tMDbClient = new TMDbClient(this.applicationOptions.TmDbApiKey);
        this.mapper = mapper;
    }

    public async Task<Result<MediaDetailsDTO>> GetMediaDetailsAsync(string referenceId, ReferenceType referenceType, CancellationToken cancellationToken = default)
    {
        MediaDetailsDTO? media = referenceType switch
        {
            ReferenceType.Movie => (await GetMovieDetailsAsync(referenceId, cancellationToken)).Value,
            ReferenceType.TV => (await GetTvShowDetailsAsync(referenceId, cancellationToken)).Value,
            _ => null
        };

        if (media == null) return Result.Fail<MediaDetailsDTO>("Invalid reference type");

        return Result.Ok(media);
    }

    public async Task<Result<MediaDetailsDTO>> GetMovieDetailsAsync(string referenceId, CancellationToken cancellationToken)
    {
        var result = await tMDbClient.GetMovieAsync(referenceId, cancellationToken: cancellationToken);
        if (result == null)
            return Result.Fail<MediaDetailsDTO>("Movie not found");

        return Result.Ok(mapper.Map<MediaDetailsDTO>(result));
    }

    public async Task<Result<byte[]>> GetMoviePosterImageAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var movie = await tMDbClient.GetMovieAsync(referenceId, cancellationToken: cancellationToken);
        if (movie == null)
            return Result.Fail<byte[]>("Movie not found");
        await tMDbClient.GetConfigAsync();
        var poster = await tMDbClient.GetImageBytesAsync("w500", movie.PosterPath, true, cancellationToken);
        if (poster == null)
            return Result.Fail<byte[]>("Movie poster not found");

        return Result.Ok(poster);
    }

    public async Task<Result<byte[]>> GetTvShowPosterImageAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var tvShow = await tMDbClient.GetTvShowAsync(int.Parse(referenceId), cancellationToken: cancellationToken);
        if (tvShow == null)
            return Result.Fail<byte[]>("TV Show not found");

        var poster = await tMDbClient.GetImageBytesAsync("w500", tvShow.PosterPath, true, cancellationToken);
        if (poster == null)
            return Result.Fail<byte[]>("TV Show poster not found");

        return Result.Ok(poster);
    }

    public async Task<Result<byte[]>> GetPosterImageAsync(string referenceId, ReferenceType referenceType, CancellationToken cancellationToken = default)
    {
        return referenceType switch
        {
            ReferenceType.Movie => await GetMoviePosterImageAsync(referenceId, cancellationToken),
            ReferenceType.TV => await GetTvShowPosterImageAsync(referenceId, cancellationToken),
            _ => Result.Fail("Invalid reference type")
        };
    }

    public async Task<Result<MediaDetailsDTO>> GetTvShowDetailsAsync(string referenceId, CancellationToken cancellationToken)
    {
        var result = await tMDbClient.GetTvShowAsync(int.Parse(referenceId), cancellationToken: cancellationToken);
        if (result == null)
            return Result.Fail<MediaDetailsDTO>("TV Show not found");

        return Result.Ok(mapper.Map<MediaDetailsDTO>(result));
    }


    public async Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMediaAsync(string query, ReferenceType referenceType, CancellationToken cancellationToken = default)
    {
        return referenceType switch
        {
            ReferenceType.Movie => await SearchMoviesAsync(query, cancellationToken),
            ReferenceType.TV => await SearchTvShowsAsync(query, cancellationToken),
            _ => Result.Fail<IEnumerable<MediaDetailsDTO>>("Invalid reference type")
        };
    }

    public async Task<Result<IEnumerable<MediaDetailsDTO>>> SearchMoviesAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchResults = await tMDbClient.SearchMovieAsync(query, cancellationToken: cancellationToken);
        
        if (searchResults == null || searchResults.TotalResults == 0) 
            return Result.Fail<IEnumerable<MediaDetailsDTO>>("No movies found");
        
        var allResults = new List<MediaDetailsDTO>();
        allResults.AddRange(searchResults.Results.Select(media => mapper.Map<MediaDetailsDTO>(media)));
        
        while (searchResults != null && searchResults.TotalResults != 0 && searchResults.Page < searchResults.TotalPages)
        {
            searchResults = await tMDbClient.SearchMovieAsync(query, searchResults.Page + 1, cancellationToken: cancellationToken);
            allResults.AddRange(searchResults.Results.Select(media => mapper.Map<MediaDetailsDTO>(media)));
        }

        return Result.Ok(allResults.AsEnumerable());
    }

    public async Task<Result<IEnumerable<MediaDetailsDTO>>> SearchTvShowsAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchResults = await tMDbClient.SearchTvShowAsync(query, cancellationToken: cancellationToken);
        
        if (searchResults == null || searchResults.TotalResults == 0) 
            return Result.Fail<IEnumerable<MediaDetailsDTO>>("No TV shows found");
        
        var allResults = new List<MediaDetailsDTO>();
        allResults.AddRange(searchResults.Results.Select(media => mapper.Map<MediaDetailsDTO>(media)));
        
        while (searchResults != null && searchResults.TotalResults != 0 && searchResults.Page < searchResults.TotalPages)
        {
            searchResults = await tMDbClient.SearchTvShowAsync(query, searchResults.Page + 1, cancellationToken: cancellationToken);
            allResults.AddRange(searchResults.Results.Select(media => mapper.Map<MediaDetailsDTO>(media)));
        }

        return Result.Ok(allResults.AsEnumerable());
    }
}