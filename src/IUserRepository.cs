using System;

namespace ZacTest.src;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync();
}
