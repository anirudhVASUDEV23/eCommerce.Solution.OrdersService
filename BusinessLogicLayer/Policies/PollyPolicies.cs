
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

public class PollyPolicies : IPollyPolicies
{
    private readonly ILogger<UsersMicroservicePolicies> _logger;

    public PollyPolicies(ILogger<UsersMicroservicePolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking,TimeSpan durationOfBreak)
    {
        IAsyncPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,//after three continuos failed requests the circuit breaker gets opened 
            durationOfBreak: durationOfBreak,//after how much time the circuit state moves from open state to half open
            onBreak: (outcome,timespan) =>//when moving from closed to open state
            {
                _logger.LogInformation($"Circuit breaker opened for {timespan.TotalMinutes} minutes due to consecutive 3 failures.The subsequent requests will be blocked");
            },
            onReset: () =>//when moving from half open to closed state
            {
                _logger.LogInformation($"Circuit breaker closed.The subsequent requests will be allowed");
            }
            );
        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        AsyncRetryPolicy<HttpResponseMessage>policy= Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(
          retryCount: retryCount,
          sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt))
        , onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            //TO DO:add logs
            _logger.LogInformation($"Retry {retryAttempt} after {timespan.TotalSeconds} seconds");
        }
        );
        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(timeout);
        return policy;
    }

   
}
