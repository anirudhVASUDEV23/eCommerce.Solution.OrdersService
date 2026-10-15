
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class UsersMicroServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroServiceClient> _logger;

    public UsersMicroServiceClient(HttpClient httpClient,ILogger<UsersMicroServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userID)
    {
        _logger.LogInformation("Calling the Users Microservice");
        HttpResponseMessage response= await _httpClient.GetAsync($"/api/Users/{userID}");
        _logger.LogInformation("Called the Users Microservice");
        if (!response.IsSuccessStatusCode)
        {
            if(response.StatusCode==System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException("Bad Request", null, System.Net.HttpStatusCode.BadRequest);
            }
            else throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
        }

        UserDTO? user=await response.Content.ReadFromJsonAsync<UserDTO>();
        if (user == null) throw new ArgumentException("Invalid UserID");
        return user;
    }
}
