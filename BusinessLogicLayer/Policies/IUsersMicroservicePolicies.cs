using Polly;

namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

public interface IUsersMicroservicePolicies
{
    //IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    //IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
    //IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy();
    IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy();

}
