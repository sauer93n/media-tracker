namespace Application.Model;

public class ApplicationOptions
{
    public string TmDbApiKey { get; set; }

    public KafkaBrokerOptions KafkaBroker { get; set; }
}