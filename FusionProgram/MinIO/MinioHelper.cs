using Minio;
using System.Collections.Concurrent;
using static FusionProgram.Extensions.ServiceCollectionExtensions;

namespace FusionProgram.MinIO
{
    public class MinioHelper : IDenpendency
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;
        private MinioClient? _client;
        private readonly ILogger<MinioHelper> _logger;
        private ConcurrentDictionary<string, MinioClient> _clients = null;
        private bool _useproxy = false;

        public MinioHelper(IConfiguration config
            , IHttpClientFactory clientFactory
            , ILogger<MinioHelper> logger)
        {
            _config = config;
            _clientFactory = clientFactory;
            _logger = logger;
            Init();
        }

        private bool Init()
        {
            try
            {
                string minioServer = _config["MicroService:Minio:Url"];
                if (string.IsNullOrEmpty(minioServer))
                    throw new ApplicationException("未配置MinIO配置!");
                string key = _config.GetSection("MicroService:Minio:Key").Value;
                string access = _config.GetSection("MicroService:Minio:Access").Value;
                bool useSsl = false;
                var useSslSection = _config.GetSection("MicroService:Minio:useSsl");
                if (useSslSection.Exists())
                {
                    if (!bool.TryParse(useSslSection.Value, out useSsl))
                    {
                        useSsl = false;
                    }
                }
                var httpclient = _clientFactory.CreateClient("minio");

                _client = new MinioClient()
                    .WithEndpoint(minioServer)
                    .WithCredentials(key, access)
                    .WithHttpClient(httpclient);
                if (useSsl)
                {
                    _client = _client.WithSSL();
                }
                //.WithSSL()
                _client = _client.Build();

                var p = _config.GetSection("MicroService:Minio:UseProxy").Value;

                if (!string.IsNullOrEmpty(p))
                {
                    _useproxy = Boolean.Parse(p);
                    if (_useproxy)
                    {
                        var proxystr = _config["MicroService:Minio:Proxy"];

                        if (!string.IsNullOrEmpty(proxystr))
                        {
                            var proxys = proxystr.Split(",");

                            _clients = new ConcurrentDictionary<string, MinioClient>();

                            for (int i = 0; i < proxys.Length; i++)
                            {
                                try
                                {
                                    var client = new MinioClient()
                        .WithEndpoint(proxys[i])
                        .WithCredentials(key, access)
                        .WithHttpClient(httpclient);
                                    if (useSsl)
                                    {
                                        client = client.WithSSL();
                                    }
                                    //.WithSSL()

                                    client = client.Build();
                                    _clients.AddOrUpdate(proxys[i], client, (k, v) => client);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError($"Minio代理初始化连接失败：{e.Message}");
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Minio初始化连接失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// stream必须在调用方进行释放管理
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task UploadAsync(Stream stream, string bucketName, string filePath, CancellationToken token = default)
        {
            // 判断是否存在，并创建桶名
            await EnsureBucketExistsAsync(bucketName);
            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);
            await _client.PutObjectAsync(args, cancellationToken: token);
        }

        /// <summary>
        /// stream必须在调用方进行释放管理
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="filePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<MemoryStream> DownloadAsync(string bucketName, string filePath, CancellationToken token = default)
        {
            if (null == _client)
                throw new Exception("MinIO客户端未初始化!");

            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath);
            await _client.StatObjectAsync(statArgs, cancellationToken: token);

            MemoryStream memoryStream = new MemoryStream();
            try
            {
                var getArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath)
                .WithCallbackStream((stream) =>
                {
                    stream.CopyTo(memoryStream);
                });
                await _client.GetObjectAsync(getArgs);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception)
            {
                try { memoryStream.Dispose(); } catch { }
                throw;
            }
        }

        /// <summary>
        /// 根据文件名获取预览路径
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bucketName"></param>
        /// <param name="expireSeconds"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<string> PreviewAsync(string file, string bucketName, int expireSeconds, string host = "", CancellationToken token = default, DateTime? requestDate = null)
        {
            var args = new PresignedGetObjectArgs()
                          .WithBucket(bucketName)
                          .WithObject(file)
                          .WithExpiry(expireSeconds)
                          .WithRequestDate(requestDate)
                          ;
            if (!string.IsNullOrEmpty(host))
            {
                if (_clients != null)
                {
                    string minioServer = _config["MicroService:Minio:Url"];

                    var key = host + ":" + minioServer.Split(":")[1];
                    if (_clients.TryGetValue(key, out MinioClient client))
                    {
                        return client.PresignedGetObjectAsync(args);
                    }
                }
            }

            if (null == _client)
                return Task.FromException<string>(new IOException("MinIO客户端未初始化!"));
            return _client.PresignedGetObjectAsync(args);
        }


        /// <summary>
        /// 使用代理根据文件名获取预览路径
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="fileName"></param>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> GetFileUrlWithProxyAsync(string bucketName, string fileName, HttpRequest request, CancellationToken token = default, DateTime? requestDate = null)
        {

            return await PreviewAsync(fileName, bucketName, 60 * 60 * 24 * 7, request.Host.Host, token, requestDate);
        }

        /// <summary>
        /// 获取文件路径
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> GetFileUrlAsync(string bucketName, string fileName, CancellationToken token = default, DateTime? requestDate = null)
        {
            return await PreviewAsync(fileName, bucketName, 60 * 60 * 24 * 7, "", token, requestDate);
        }

        /// <summary>
        /// 确保桶存在，若不存在则创建桶
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken token = default)
        {
            if (null == _client) throw new Exception("MinIO客户端未初始化!");
            var beArgs = new BucketExistsArgs().WithBucket(bucketName);
            if (!await _client.BucketExistsAsync(beArgs, token))
            {
                var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _client.MakeBucketAsync(mbArgs, token);
            }
        }

        /// <summary>
        /// 根据桶名和文件名删除文件
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="filePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task RemoveFileAsync(string bucketName, string filePath, CancellationToken token = default)
        {
            if (null == _client) throw new Exception("MinIO客户端未初始化!");
            if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            RemoveObjectArgs rmArgs = new RemoveObjectArgs()
                                        .WithBucket(bucketName)
                                        .WithObject(filePath);
            await _client.RemoveObjectAsync(rmArgs);
        }
    }
}
