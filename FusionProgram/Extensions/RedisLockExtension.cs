using Redis.Service;
using ServiceStack.Redis;

namespace FusionProgram.Extensions
{
    /// <summary>
    /// redis分布式锁扩展
    /// </summary>
    public class RedisLockExtension
    {
        private readonly IRedisClientsManager _redisManager;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private readonly TimeSpan _lockExpiry;
        private bool _lockAcquired;

        public RedisLockExtension(IRedisServer redisServer, string lockKey, TimeSpan lockExpiry)
        {
            _redisManager = redisServer.GetRedisClientsManager();
            _lockKey = lockKey;
            _lockValue = Guid.NewGuid().ToString();
            _lockExpiry = lockExpiry;
        }

        /// <summary>
        /// 添加锁
        /// </summary>
        /// <returns></returns>
        public bool AcquireLock()
        {
            using (var redis = _redisManager.GetClient())
            {
                _lockAcquired = redis.SetValueIfNotExists(_lockKey, _lockValue);
                if (_lockAcquired)
                {
                    redis.ExpireEntryIn(_lockKey, _lockExpiry);
                }
            }
            return _lockAcquired;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        public void ReleaseLock()
        {
            if (_lockAcquired)
            {
                using (var redis = _redisManager.GetClient())
                {
                    var storedValue = redis.GetValue(_lockKey);
                    if (storedValue == _lockValue)
                    {
                        redis.Remove(_lockKey);
                    }
                }
                _lockAcquired = false;
            }
        }

        public void Dispose()
        {
            ReleaseLock();
        }
    }
}
