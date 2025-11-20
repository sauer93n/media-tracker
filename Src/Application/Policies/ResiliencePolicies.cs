using Polly;

namespace Application.Policies;

/// <summary>
/// Polly resilience policies for HTTP requests
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy with exponential backoff
    /// Retries up to 3 times with increasing delays
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult<HttpResponseMessage>(r => 
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout || // 408
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests || // 429
                r.StatusCode == System.Net.HttpStatusCode.InternalServerError || // 500
                r.StatusCode == System.Net.HttpStatusCode.BadGateway || // 502
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || // 503
                r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout) // 504
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s. Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });
    }

    /// <summary>
    /// Circuit breaker policy
    /// Opens after 5 failures in 30 seconds
    /// Stays open for 10 seconds before attempting recovery
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(10),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }

    /// <summary>
    /// Timeout policy
    /// Aborts requests after 10 seconds
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Combined policy: Retry -> Circuit Breaker -> Timeout
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetRetryPolicy(),
            GetCircuitBreakerPolicy(),
            GetTimeoutPolicy());
    }
}
