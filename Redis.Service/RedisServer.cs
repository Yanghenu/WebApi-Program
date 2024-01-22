using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace Redis.Service
{
    public class RedisServer : IRedisServer
    {
        /// <summary>
        /// 本机IP,Redis默认端口是6379
        /// </summary>
        private RedisClient client = null;
        private PooledRedisClientManager pool = null;
        private readonly ILogger<RedisServer> _logger;

        public RedisServer(IConfiguration configuration,ILogger<RedisServer> logger)
        {
            _logger = logger;
            var redisHostStr = configuration["Redis:redis_server"];
            //方式一
            if (!string.IsNullOrEmpty(redisHostStr))
            {
                string[] redisHosts = redisHostStr.Split(',');

                if (redisHosts.Length > 0)
                {
                    pool = new PooledRedisClientManager(redisHosts, redisHosts,
                        new RedisClientManagerConfig()
                        {
                            MaxWritePoolSize = int.Parse(configuration["Redis:redis_max_write_pool"]),
                            MaxReadPoolSize = int.Parse(configuration["Redis:redis_max_read_pool"]),
                            AutoStart = true
                        });
                }
            }
            //方式二
            //if (!string.IsNullOrEmpty(redisHostStr))
            //{
            //    if (redisHostStr.Split(':').Length > 1)
            //    {
            //        string ip = redisHostStr.Split(':')[0];
            //        int port = Convert.ToInt32(redisHostStr.Split(':')[1]);
            //        client = new RedisClient(ip, port);
            //    }
            //}
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void InserData(string key, object value)
        {
            try
            {
                client.Set<object>(key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetData(string key)
        {
            try
            {
                return client.Get<string>(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return ex.ToString();
            }
        }

        /// <summary>
        /// 存储数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        public void Add<T>(string key, T value, DateTime expiry)
        {
            if (value == null)
            {
                return;
            }

            if (expiry <= DateTime.Now)
            {
                Remove(key);

                return;
            }

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Set(key, value, expiry - DateTime.Now);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!KEY = {2}", "cache", "存储", key);
                _logger.LogError(ex.ToString());
            }

        }
        /// <summary>
        /// 存储数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="slidingExpiration"></param>
        public void Add<T>(string key, T value, TimeSpan slidingExpiration)
        {
            if (value == null)
            {
                return;
            }

            if (slidingExpiration.TotalSeconds <= 0)
            {
                Remove(key);

                return;
            }

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Set(key, value, slidingExpiration);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "存储", key);
                _logger.LogError(ex.ToString());
            }

        }


        /// <summary>
        /// 获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }

            T obj = default(T);

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            obj = r.Get<T>(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "获取", key);
                _logger.LogError(ex.ToString());
            }


            return obj;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "删除", key);
                _logger.LogError(ex.ToString());
            }

        }

        public bool Exists(string key)
        {
            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            return r.ContainsKey(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "是否存在", key);
                _logger.LogError(ex.ToString());
            }

            return false;
        }
    }
}
