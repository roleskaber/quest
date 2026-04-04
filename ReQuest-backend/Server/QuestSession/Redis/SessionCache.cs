using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using ReQuest_backend.Server.Database.User;

namespace ReQuest_backend.Server.QuestSession.Redis;

using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

public class SessionCache
{
    private readonly ConnectionMultiplexer _muxer;
    private readonly IDatabase _db;

    public SessionCache()
    {
        _muxer = ConnectionMultiplexer.Connect("localhost:6379");
        _db = _muxer.GetDatabase();
    }

    public async Task<string> CreateSessionToken(UserEntity user)
    {
        var token = Guid.NewGuid().ToString("N");

        await _db.StringSetAsync(
            $"session:{token}",
            user.Id,
            TimeSpan.FromMinutes(10)
        );

        return token;
    }
}