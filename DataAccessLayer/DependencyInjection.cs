using eCommerce.OrdersMicroService.DataAccessLayer.Repositories;
using eCommerce.OrdersMicroService.DataAccessLayer.RepositoryContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.DataAccessLayer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your data access layer services here
            // Example: services.AddScoped<IYourRepository, YourRepository>();
            string connectionStringTemplate= configuration.GetConnectionString("MongoDB")!;
            string connectionString = connectionStringTemplate.Replace("$MONGO_HOST", Environment.GetEnvironmentVariable("MONGODB_HOST"))
                .Replace("$MONGO_PORT", Environment.GetEnvironmentVariable("MONGODB_PORT"));
            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddScoped<IMongoDatabase>(provider =>
            {
                IMongoClient client= provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(Environment.GetEnvironmentVariable("MONGODB_DATABASE"));

            });
            services.AddScoped<IOrdersRepository, OrdersRepository>();
            return services;
        }
    }
}
