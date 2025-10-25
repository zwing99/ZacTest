using Scalar.AspNetCore; // make sure to import the namespace
using ZacTest.src;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDataSourceFactory, DataSourceFactory>();
builder.Services.AddSingleton(provider =>
{
    var factory = provider.GetRequiredService<IDataSourceFactory>();
    return factory.Create();
});
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Use the built-in OpenAPI doc generator
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Map OpenAPI JSON to /spec instead of /openapi/v1.json
    app.MapOpenApi("/spec");

    // Map Scalar UI (points to /spec automatically)
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("ZacTest API")
            .WithTheme(ScalarTheme.Solarized)
            .WithOpenApiRoutePattern("/spec");
    });
}

// **Other middleware**
app.UseExceptionHandler();
app.UseRouting();

// app.UseAuthorization();

app.MapControllers();

app.Run();
