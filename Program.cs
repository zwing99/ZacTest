using Scalar.AspNetCore; // make sure to import the namespace
using ZacTest.src;
using Microsoft.Extensions.Options;

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
builder.Services.AddSingleton<IOptions<SqlTextOptions>>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var asm = typeof(Program).Assembly;

    var options = new SqlTextOptions(
        Root: "Sql",
        PreferFileSystem: env.IsDevelopment(),
        ResourceAssembly: asm,
        ResourceRootNamespace: $"{asm.GetName().Name}.Sql"
    );

    return Options.Create(options);
});
builder.Services.AddSingleton<ISqlTextProvider>(sp =>
    SqlTextProvider.Create(
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<IOptions<SqlTextOptions>>(),
        sp.GetRequiredService<ILogger<SqlTextProvider>>()
    )
);

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
