using System;
using Npgsql;

namespace ZacTest.src;

public interface IDataSourceFactory
{
    NpgsqlDataSource Create();
}
