using Panda.DevUtil.Distributed.Abstract;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed
{
    public class RedisLock : ILock
    {

        //[ThreadStaticAttribute]
        //static Dictionary<string, string> threadLockDict = new Dictionary<string, string>();
        static int _initTime = 0;
        static int _processId = 0;
        static int _serverIP = 0;
        static int _count = 0;
        static string _lockIdPerfix = null;

        static RedisLock()
        {
            InitEnviroment();
        }

        ConnectionMultiplexer _redis = null;
        int _db = 0;
        string _connectStr = null;
        DateTime _lastConnectExceptionTime = DateTime.MinValue;
        LoadedLuaScript _releaseLockScript = null;

        public RedisLock() : this(ConfigurationManager.AppSettings["redis"]) { }
        public RedisLock(string connectStr) : this(connectStr, 0) { }
        public RedisLock(string connectStr, int db)
        {
            _connectStr = connectStr;
            _db = db;
        }

        public bool Get(string resourceId, int expire, out string lockId, GetLockOption option = null)
        {
            lockId = null;
            if (string.IsNullOrEmpty(resourceId))
                throw new ArgumentNullException("resourceId");
            if (expire <= 0)
                throw new ArgumentNullException("expire");
            TimeSpan? e = TimeSpan.FromMilliseconds(expire);
            string randomLockId = GetRandomLockId(resourceId);
            bool got = false;
            bool firstGet = true;
            bool retry = option != null && option.Retry;
            int retryInterval = 0;
            int retryTimeout = 0;
            if (retry)
            {
                retryInterval = option.RetryInterval > 0 ? option.RetryInterval : 100;
                retryTimeout = option.Timeout > 0 ? option.Timeout : 300;
            }
            Stopwatch watch = Stopwatch.StartNew();
            var db = GetDB();
            do
            {
                if (!firstGet)
                    Thread.Sleep(retryInterval);
                got = db.StringSet(resourceId, randomLockId, e, when: When.NotExists);
                firstGet = false;
            } while (!got && retry && watch.ElapsedMilliseconds < retryTimeout);
            if (got)
                lockId = randomLockId;
            return got;
        }

        public Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }

        public void Release(string resourceId, string lockId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return;
            if (string.IsNullOrEmpty(lockId))
                return;
            var db = GetDB();
            db.ScriptEvaluate(_releaseLockScript, new { rid = resourceId, lid = lockId });
        }

        public Task ReleaseAsync(string resourceId, string lockId)
        {
            throw new NotImplementedException();
        }

        public ILockItem Using(string resourceId, int expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }

        public Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }

        private static void InitEnviroment()
        {
            DateTime now = DateTime.Now;
            _initTime = now.Hour * 60 * 60 + now.Minute * 60 + now.Second;
            _processId = Process.GetCurrentProcess().Id;

            string hostname = Dns.GetHostName();//得到本机名   
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            IPAddress localaddr = localhost.AddressList[0];
            _serverIP = localaddr.GetHashCode();
            _lockIdPerfix = string.Format("{0}{1}{2}", _serverIP, _processId, _initTime);
        }

        private IDatabase GetDB()
        {
            InitRedis();
            if (_redis == null)
                throw new Exception("RedisLock connect redis failed");
            return _redis.GetDatabase(_db);
        }

        private void InitRedis()
        {
            if (_redis == null && _lastConnectExceptionTime.AddMinutes(1) < DateTime.Now)
            {
                lock (this)
                {
                    try
                    {
                        _redis = ConnectionMultiplexer.Connect(_connectStr);
                        string script = "if redis.call(\"GET\",@rid) == @lid then redis.call(\"DEL\",@rid) end";
                        ConfigurationOptions options = ConfigurationOptions.Parse(_connectStr);
                        options.SetDefaultPorts();
                        EndPoint point = options.EndPoints[0];
                        var server = _redis.GetServer(options.EndPoints[0]);
                        if (server.ScriptExists(script))
                            return;
                        LuaScript luaScript = LuaScript.Prepare(script);
                        _releaseLockScript = luaScript.Load(server);
                    }
                    catch
                    {
                        _lastConnectExceptionTime = DateTime.Now;
                        _redis = null;
                    }
                }
            }
        }

        /// <summary>
        /// 生成随机锁Id
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        /// <remarks>
        /// 标识资源的持有者，防止资源锁被非持有者错误释放
        /// {服务器IP}{进程Id}{初始化时间戳}{全局计数器}
        /// </remarks>
        private string GetRandomLockId(string resourceId)
        {
            int c = Interlocked.Increment(ref _count);
            return string.Format("{0}{1}", _lockIdPerfix, c);
        }
    }
}
