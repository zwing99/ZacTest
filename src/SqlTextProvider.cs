using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ZacTest.src;

public sealed class SqlTextOptions
{
    public string Root = "Sql"; // folder under content root or output dir
    public bool PreferFileSystem = true; // dev: true, prod: false if you embed
    public Assembly? ResourceAssembly { get; set; } // for embedded mode
    public string? ResourceRootNamespace { get; set; } // e.g., "MyApp.Sql"
}

public sealed class SqlTextProvider : ISqlTextProvider, IDisposable
{
    private readonly IFileProvider _files; // composite
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly List<IDisposable> _registrations = new();
    private readonly Assembly? _asm;
    private readonly string? _ns;

    public SqlTextProvider(
        IHostEnvironment env,
        IOptions<SqlTextOptions> options,
        ILogger<SqlTextProvider> log
    )
    {
        var o = options.Value;
        _asm = o.ResourceAssembly ?? Assembly.GetEntryAssembly();
        _ns = o.ResourceRootNamespace;

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
        _files = o.PreferFileSystem
            ? new CompositeFileProvider(devProvider, outProvider)
            : new CompositeFileProvider(outProvider, devProvider);

        // Watch if any real provider exists (NullFileProvider returns a noop token)
        _registrations.Add(
            ChangeToken.OnChange(
                () => _files.Watch("**/*.sql"),
                () =>
                {
                    _cache.Clear();
                    log.LogInformation("SQL text cache invalidated due to file change.");
                }
            )
        );

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
    }

    public string Get(string key)
    {
        var normalized = key.Replace('\\', '/').TrimStart('/');
        return _cache.GetOrAdd(
            normalized,
            k =>
            {
                var file = _files.GetFileInfo($"{k}.sql");
                if (file.Exists && file.PhysicalPath is not null)
                    return File.ReadAllText(file.PhysicalPath);

                // Fallback: embedded resource
                if (_asm != null)
                {
                    var resName =
                        _ns != null
                            ? $"{_ns}.{k.Replace('/', '.')}.sql"
                            : _asm.GetManifestResourceNames()
                                .FirstOrDefault(n =>
                                    n.EndsWith(
                                        $"{k.Replace('/', '.')}.sql",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                );

                    if (resName != null)
                    {
                        using var s =
                            _asm.GetManifestResourceStream(resName)
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
