
using eCommerce.OrdersMicroservice.BusinessLogicLayer;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.DataAccessLayer;
using eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;
using FluentValidation.AspNetCore;
using OrdersMicroService.API.Middleware;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogic(builder.Configuration);

builder.Services.AddControllers();

//Fluent Validations
builder.Services.AddFluentValidationAutoValidation();

//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>();
builder.Services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();
builder.Services.AddTransient<IPollyPolicies, PollyPolicies>();


builder.Services.AddHttpClient<UsersMicroServiceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
})
.AddPolicyHandler(//Using polly for circuit in case of an error
    builder.Services.BuildServiceProvider().GetService<IUsersMicroservicePolicies>().GetCombinedPolicy()
    )

;

builder.Services.AddHttpClient<ProductsMicroServiceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
}).AddPolicyHandler(//Using polly for retrying in case of an error
    builder.Services.BuildServiceProvider().GetService<IProductsMicroservicePolicies>().GetFallbackPolicy())
   .AddPolicyHandler(//Using polly for retrying in case of an error
    builder.Services.BuildServiceProvider().GetService<IProductsMicroservicePolicies>().GetBulkheadIsolationPolicy())
;

var app = builder.Build();

app.UseExceptionHandlingMiddleware();
app.UseRouting();

//Cors
app.UseCors();

//Swagger
app.UseSwagger();
app.UseSwaggerUI();

//Auth
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

//Endpoints
app.MapControllers();

app.Run();
