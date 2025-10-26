using System;
using Dapper;
using Npgsql;

namespace ZacTest.src;

public class UserRepository(NpgsqlDataSource dataSource, ISqlTextProvider sqlTextProvider)
    : IUserRepository
{
    private NpgsqlDataSource _dataSource = dataSource;
    private ISqlTextProvider _sqlTextProvider = sqlTextProvider;

    async Task<IEnumerable<User>> IUserRepository.GetAllUsersAsync()
    {
        await using var connection = _dataSource.CreateConnection();
        var sql = _sqlTextProvider.Get("User/GetAllUsers");
        var users = await connection.QueryAsync<User>(sql);

        return users;
    }
}
