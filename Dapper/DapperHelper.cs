using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DapperSQL
{
    public class DapperHelper
    {
        private static string ConnectionString;
        private static readonly object LockObject = new object();
        private static bool isInitialized = false;
        // 初始化连接字符串
        public static void Initialize(IConfiguration configuration)
        {
            lock (LockObject)
            {
                if (!isInitialized)
                {
                    ConnectionString = configuration.GetSection("Dapper_ConnectionString").Value;
                    isInitialized = true;
                }
            }
        }
        // 查询单个实体
        public static T QueryFirstOrDefault<T>(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new SqlConnection(ConnectionString))
            {
                dbConnection.Open();
                
                return dbConnection.QueryFirstOrDefault<T>(sql, parameters);
            }
        }

        // 查询多个实体
        public static IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new SqlConnection(ConnectionString))
            {
                dbConnection.Open();
                return dbConnection.Query<T>(sql, parameters);
            }
        }

        // 查询返回datatable
        public static DataTable QueryToDataTable(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new SqlConnection(ConnectionString))
            {
                dbConnection.Open();
                var table = new DataTable();
                var reader = dbConnection.ExecuteReader(sql);
                table.Load(reader);
                return table;
            }
        }

        // 执行插入、更新、删除操作
        public static int Execute(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new SqlConnection(ConnectionString))
            {
                dbConnection.Open();
                return dbConnection.Execute(sql, parameters);
            }
        }

        // 插入并返回自增ID
        public static int InsertWithIdentity(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new SqlConnection(ConnectionString))
            {
                dbConnection.Open();
                return dbConnection.ExecuteScalar<int>(sql, parameters);
            }
        }
    }
}
