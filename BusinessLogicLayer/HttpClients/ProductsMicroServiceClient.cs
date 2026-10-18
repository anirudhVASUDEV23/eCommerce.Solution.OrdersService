
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System.Net.Http.Json;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class ProductsMicroServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroServiceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public ProductsMicroServiceClient(HttpClient httpClient,ILogger<UsersMicroServiceClient> logger,IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<ProductDTO?> GetProductByProductID(Guid productID)
    {
        try
        {
            _logger.LogInformation("Calling the Users Microservice");
            string cacheKey = $"product:{productID}";
            string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);
            if (cachedProduct != null)
            {
                ProductDTO? productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);
                return productFromCache;
            }
            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/products/search/product-id/{productID}");
            _logger.LogInformation("Called the Users Microservice");
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode==System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ProductDTO? productFromFallback = await response.Content.ReadFromJsonAsync<ProductDTO>();
                    if (productFromFallback == null) throw new NotImplementedException("Fallback policy was not implemented");
                    return productFromFallback;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad Request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
            }

            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();
            if (product == null) throw new ArgumentException("Invalid ProductID");
            //add to the cache
            string productJson = JsonSerializer.Serialize(product);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                .SetSlidingExpiration(TimeSpan.FromSeconds(10));//if not retrieved in 10 sec it removes from the cache
            string cacheKeyToWrite = $"product:{productID}";
            await _distributedCache.SetStringAsync(cacheKeyToWrite,productJson,options);
            return product;
        }
        catch (BulkheadRejectedException EX)
        {
            _logger.LogError(EX, "Bulkhead isolation blocks the request since the request queue is full");
            return new ProductDTO(
                 ProductID: Guid.Empty,
                ProductName: "Temporarily Unavailable(bulkhead)",
                Category: "Temporarily Unavailable(bulkhead)",
                UnitPrice: 0,
                QuantityInStock: 0);
        }
    }
}
