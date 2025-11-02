using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ZacTest.src;

public sealed record SqlTextOptions(
    string Root = "Sql",
    bool PreferFileSystem = true,
    Assembly? ResourceAssembly = null,
    string? ResourceRootNamespace = null
)
{
    // Parameterless constructor for DI
    public SqlTextOptions()
        : this("Sql", true, null, null) { }
}

public sealed class SqlTextProvider : ISqlTextProvider, IDisposable
{
    // Required init-only properties
    public required IFileProvider FileProvider { get; init; }
    public required Assembly? ResourceAssembly { get; init; }
    public required string? ResourceRootNamespace { get; init; }
    public required ILogger<SqlTextProvider>? Logger { get; init; }

    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly List<IDisposable> _registrations = new();

    /// <summary>
    /// Factory method to create and configure SqlTextProvider from DI.
    /// </summary>
    public static SqlTextProvider Create(
        IHostEnvironment env,
        IOptions<SqlTextOptions> options,
        ILogger<SqlTextProvider> log
    )
    {
        var o = options.Value;
        var asm = o.ResourceAssembly ?? Assembly.GetEntryAssembly();
        var ns = o.ResourceRootNamespace;

        // Candidate roots
        var devPath = Path.Combine(env.ContentRootPath, o.Root ?? "Sql");
        var outPath = Path.Combine(AppContext.BaseDirectory, o.Root ?? "Sql");

        // Only create PhysicalFileProvider for folders that actually exist
        IFileProvider devProvider = Directory.Exists(devPath)
            ? new PhysicalFileProvider(
                devPath,
                ExclusionFilters.Hidden | ExclusionFilters.DotPrefixed
            )
            : new NullFileProvider();

        IFileProvider outProvider = Directory.Exists(outPath)
            ? new PhysicalFileProvider(
                outPath,
                ExclusionFilters.Hidden | ExclusionFilters.DotPrefixed
            )
            : new NullFileProvider();

        // Preferred order: dev first if requested, else output first
        var compositeProvider = o.PreferFileSystem
            ? new CompositeFileProvider(devProvider, outProvider)
            : new CompositeFileProvider(outProvider, devProvider);

        // Friendly diagnostics
        if (devProvider is NullFileProvider && outProvider is NullFileProvider)
        {
            log.LogWarning(
                "No SQL directory found at '{Dev}' or '{Out}'. "
                    + "Either create the folder, copy files to output, or embed resources.",
                devPath,
                outPath
            );
        }

        return new SqlTextProvider
        {
            FileProvider = compositeProvider,
            ResourceAssembly = asm,
            ResourceRootNamespace = ns,
            Logger = log,
        }._Initialize();
    }

    private SqlTextProvider _Initialize()
    {
        // Watch if any real provider exists (NullFileProvider returns a noop token)
        _registrations.Add(
            ChangeToken.OnChange(
                () => FileProvider.Watch("**/*.sql"),
                () =>
                {
                    _cache.Clear();
                    Logger?.LogInformation("SQL text cache invalidated due to file change.");
                }
            )
        );

        return this;
    }

    public string Get(string key)
    {
        var normalized = key.Replace('\\', '/').TrimStart('/');
        return _cache.GetOrAdd(
            normalized,
            k =>
            {
                var file = FileProvider.GetFileInfo($"{k}.sql");
                if (file.Exists && file.PhysicalPath is not null)
                    return File.ReadAllText(file.PhysicalPath);

                // Fallback: embedded resource
                if (ResourceAssembly != null)
                {
                    var resName =
                        ResourceRootNamespace != null
                            ? $"{ResourceRootNamespace}.{k.Replace('/', '.')}.sql"
                            : ResourceAssembly
                                .GetManifestResourceNames()
                                .FirstOrDefault(n =>
                                    n.EndsWith(
                                        $"{k.Replace('/', '.')}.sql",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                );

                    if (resName != null)
                    {
                        using var s =
                            ResourceAssembly.GetManifestResourceStream(resName)
                            ?? throw new FileNotFoundException(
                                $"Embedded SQL not found: {resName}"
                            );
                        using var r = new StreamReader(s);
                        return r.ReadToEnd();
                    }
                }

                throw new FileNotFoundException(
                    $"SQL not found for key '{k}'. Looked for file '{k}.sql' and embedded resource."
                );
            }
        );
    }

    public void Dispose()
    {
        foreach (var d in _registrations)
            d.Dispose();
    }
}
