using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.Service
{
    public interface IRedisServer
    {
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void InserData(string key, object value);
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetData(string key);
        /// <summary>
        /// 存储数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        public void Add<T>(string key, T value, DateTime expiry);
        /// <summary>
        /// 存储数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="slidingExpiration"></param>
        public void Add<T>(string key, T value, TimeSpan slidingExpiration);
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key);
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key);
        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(string key);
    }
}
