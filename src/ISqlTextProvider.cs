using System;

namespace ZacTest.src;

public interface ISqlTextProvider
{
    string Get(string key); // e.g., "Users/FindById"
}
