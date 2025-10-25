using System;
using Npgsql;

namespace ZacTest.src;

public class DataSourceFactory : IDataSourceFactory, IDisposable
{
    private readonly string _connectionString;
    private NpgsqlDataSource? _dataSource;

    public DataSourceFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found."
            );
    }

    public NpgsqlDataSource Create()
    {
        if (_dataSource == null)
        {
            var builder = new NpgsqlDataSourceBuilder(_connectionString);
            // Optionally configure your builder: pooling, logging, etc.
            _dataSource = builder.Build();
        }

        return _dataSource;
    }

    public void Dispose()
    {
        _dataSource?.Dispose();
    }
}
