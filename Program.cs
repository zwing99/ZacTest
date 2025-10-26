using Scalar.AspNetCore; // make sure to import the namespace
using ZacTest.src;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IDataSourceFactory, DataSourceFactory>();
builder.Services.AddSingleton(provider =>
{
    var factory = provider.GetRequiredService<IDataSourceFactory>();
    return factory.Create();
});
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure SQL text provider
builder.Services.Configure<SqlTextOptions>(o =>
{
    o.Root = "Sql";
    o.PreferFileSystem = builder.Environment.IsDevelopment(); // hot-reload in dev
    o.ResourceAssembly = typeof(Program).Assembly;
    o.ResourceRootNamespace = typeof(Program).Assembly.GetName().Name + ".Sql"; // if embedded
});
builder.Services.AddSingleton<ISqlTextProvider, SqlTextProvider>();

// Add API Controllers
builder.Services.AddControllers();

// Add Problem Details middleware
builder.Services.AddProblemDetails();

// Use the built-in OpenAPI doc generator
builder.Services.AddOpenApi();

// Add Scalar API Reference middleware
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

// Map (Route) API controllers
app.MapControllers();

app.Run();
