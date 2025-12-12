namespace Application.Model;

public class ApplicationOptions
{
    public string TmDbApiKey { get; set; }

    public string KinopoiskApiKey { get; set; }

    public string KinopoiskBaseUrl { get; set; }

    public KafkaBrokerOptions KafkaBroker { get; set; }
}