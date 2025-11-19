using System.Text.Json.Serialization;

namespace Application.DTO;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReferenceType
{
    Movie,
    TV
}