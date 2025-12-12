using System.Text.Json.Serialization;

namespace Application.DTO;

public class KinopoiskRatingDTO
{
    [JsonPropertyName("kinopoiskId")]
    public int KinopoiskId { get; set; }

    [JsonPropertyName("imdbId")]
    public string ImdbId { get; set; } = string.Empty;
    
    [JsonPropertyName("nameRu")]
    public string NameRu { get; set; } = string.Empty;
    
    [JsonPropertyName("nameEn")]
    public string NameEn { get; set; } = string.Empty;
    
    [JsonPropertyName("nameOriginal")]
    public string NameOriginal { get; set; } = string.Empty;

    [JsonPropertyName("ratingKinopoisk")]
    public double RatingKinopoisk { get; set; }
    
    [JsonPropertyName("ratingImbd")]
    public double RatingImbd { get; set; }
    
    [JsonPropertyName("year")]
    public int Year { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("posterUrl")]
    public string PosterUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("posterUrlPreview")]
    public string PosterUrlPreview { get; set; } = string.Empty;
    
    [JsonPropertyName("userRating")]
    public double UserRating { get; set; }
}


/*
"kinopoiskId": 263531,
"nameRu": "Мстители",
"nameEn": "The Avengers",
"nameOriginal": "The Avengers",
"countries": [
    {
        "country": "США"
    }
],
"genres": [
    {
        "genre": "фантастика"
    }
],
"ratingKinopoisk": 7.9,
"ratingImbd": 7.9,
"year": "2012",
"type": "FILM",
"posterUrl": "http://kinopoiskapiunofficial.tech/images/posters/kp/263531.jpg",
"posterUrlPreview": "https://kinopoiskapiunofficial.tech/images/posters/kp_small/301.jpg",
"userRating": 7
*/