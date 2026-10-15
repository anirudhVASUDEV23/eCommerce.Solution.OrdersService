
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class ProductsMicroServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroServiceClient> _logger;

    public ProductsMicroServiceClient(HttpClient httpClient,ILogger<UsersMicroServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductDTO?> GetProductByProductID(Guid productID)
    {
        _logger.LogInformation("Calling the Users Microservice");
        HttpResponseMessage response= await _httpClient.GetAsync($"/api/products/search/product-id/{productID}");
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

        ProductDTO? product=await response.Content.ReadFromJsonAsync<ProductDTO>();
        if (product == null) throw new ArgumentException("Invalid UserID");
        return product;
    }
}
