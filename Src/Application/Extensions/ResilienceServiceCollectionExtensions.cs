using Application.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

/// <summary>
/// Extension methods for adding resilience policies to services
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Adds Polly resilience policies to the HttpClient
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder.AddPolicyHandler(ResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy())
            .AddPolicyHandler(ResiliencePolicies.GetTimeoutPolicy());
    }
}

