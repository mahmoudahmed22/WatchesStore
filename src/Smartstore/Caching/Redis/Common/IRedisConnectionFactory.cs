using Smartstore.Redis.Caching;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public interface IRedisConnectionFactory
    {
        ConnectionMultiplexer GetConnection(string connectionString);
        RedisMessageBus GetMessageBus(string connectionString);
    }
}