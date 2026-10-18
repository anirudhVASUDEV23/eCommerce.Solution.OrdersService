
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http.Json;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class UsersMicroServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroServiceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public UsersMicroServiceClient(HttpClient httpClient,ILogger<UsersMicroServiceClient> logger,IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userID)
    {
        try
        {
            _logger.LogInformation("Calling the Users Microservice");
            string cacheKeyToRead = $"user:{userID}";
            string? cachedUser = await _distributedCache.GetStringAsync(cacheKeyToRead);
            if(cachedUser!=null)
            {
                UserDTO? userFromCache = JsonSerializer.Deserialize<UserDTO>(cachedUser);
                return userFromCache;
            }
            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/Users/{userID}");
            _logger.LogInformation("Called the Users Microservice");
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    UserDTO? fallbackUser = await response.Content.ReadFromJsonAsync<UserDTO>();
                    if(fallbackUser==null)
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad Request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    //throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                    //Fault tolerance=>Fault data
                    return new UserDTO(PersonName: "Temporarily Unavailable", Email: "Temporarily Unavailable", Gender: "Temporarily Unavailable(circuit breaker)"
                        , UserID: Guid.Empty
                        );
                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();
            if (user == null) throw new ArgumentException("Invalid UserID");

            //add to the cache
            var cacheKey = $"user:{userID}";
            string userJson = JsonSerializer.Serialize(user);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions().
                SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(3));
            await _distributedCache.SetStringAsync(cacheKey, userJson, options);
            return user;
        }
        catch(BrokenCircuitException ex)
        {
            _logger.LogError(ex,"Request failed because the circuit breaker is in open state.Returning dummy data");
            //throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
            //Fault tolerance=>Fault data
            return new UserDTO(PersonName: "Temporarily Unavailable(circuit breaker)", Email: "Temporarily Unavailable(circuit breaker)", Gender: "Temporarily Unavailable"
                , UserID: Guid.Empty
                );
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Timeout occured while fetching user data.Returning dummy data");
            //throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
            //Fault tolerance=>Fault data
            return new UserDTO(PersonName: "Temporarily Unavailable(timeout)", Email: "Temporarily Unavailable(timeout)", Gender: "Temporarily Unavailable(timeout)"
                , UserID: Guid.Empty
                );
        }
    }
}
