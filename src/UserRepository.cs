using System;
using Dapper;
using Npgsql;

namespace ZacTest.src;

public class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    private NpgsqlDataSource _dataSource = dataSource;

    async Task<IEnumerable<User>> IUserRepository.GetAllUsersAsync()
    {
        await using var connection = _dataSource.CreateConnection();
        var users = await connection.QueryAsync<User>("SELECT username, email FROM users");

        return users;
    }
}
