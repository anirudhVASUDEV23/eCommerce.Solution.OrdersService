
using eCommerce.OrdersMicroservice.BusinessLogicLayer;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.DataAccessLayer;
using FluentValidation.AspNetCore;
using OrdersMicroService.API.Middleware;

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


builder.Services.AddHttpClient<UsersMicroServiceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
});

builder.Services.AddHttpClient<ProductsMicroServiceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
});

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
