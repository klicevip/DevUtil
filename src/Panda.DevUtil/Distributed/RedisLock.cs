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
            bool got = false;
            bool firstGet = true;
            bool retry = false;
            int retryInterval = 0;
            int retryTimeout = 0;
            CheckGetArgument(resourceId, expire, option, out retry, out retryInterval, out retryTimeout);
            TimeSpan? e = TimeSpan.FromMilliseconds(expire);
            string randomLockId = GetRandomLockId();
            Stopwatch watch = Stopwatch.StartNew();
            var db = GetDB();
            do
            {
                if (!firstGet)
                    Thread.Sleep(retryInterval);
                got = db.StringSet(GetRedisLockKey(resourceId), randomLockId, e, when: When.NotExists);
                firstGet = false;
            } while (!got && retry && watch.ElapsedMilliseconds < retryTimeout);
            if (got)
                lockId = randomLockId;
            return got;
        }

        public async Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null)
        {
            string lockId = null;
            bool got = false;
            bool firstGet = true;
            bool retry = false;
            int retryInterval = 0;
            int retryTimeout = 0;
            CheckGetArgument(resourceId, expire, option, out retry, out retryInterval, out retryTimeout);
            TimeSpan? e = TimeSpan.FromMilliseconds(expire);
            string randomLockId = GetRandomLockId();
            Stopwatch watch = Stopwatch.StartNew();
            var db = GetDB();
            do
            {
                if (!firstGet)
                    Thread.Sleep(retryInterval);
                got = await db.StringSetAsync(GetRedisLockKey(resourceId), randomLockId, e, when: When.NotExists);
                firstGet = false;
            } while (!got && retry && watch.ElapsedMilliseconds < retryTimeout);
            if (got)
                lockId = randomLockId;
            return new Tuple<bool, string>(got, lockId);
        }

        public void Release(string resourceId, string lockId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return;
            if (string.IsNullOrEmpty(lockId))
                return;
            var db = GetDB();
            db.ScriptEvaluate(_releaseLockScript, new { rid = GetRedisLockKey(resourceId), lid = lockId });
        }

        public async Task ReleaseAsync(string resourceId, string lockId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return;
            if (string.IsNullOrEmpty(lockId))
                return;
            var db = GetDB();
            await db.ScriptEvaluateAsync(_releaseLockScript, new { rid = GetRedisLockKey(resourceId), lid = lockId });
        }

        public ILockItem Using(string resourceId, int expire, GetLockOption option = null)
        {
            string lockId = null;
            Get(resourceId, expire, out lockId, option);
            return new LockItem { ResourceId = resourceId, LockId = lockId, Lock = this };
        }

        public async Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null)
        {
            Tuple<bool, string> gotAndLockId = await GetAsync(resourceId, expire, option);
            return new LockItem { ResourceId = resourceId, LockId = gotAndLockId.Item2, Lock = this };
        }

        void CheckGetArgument(string resourceId, int expire, GetLockOption option, out bool retry, out int retryInterval, out int timeout)
        {
            retry = false;
            retryInterval = 0;
            timeout = 0;
            if (string.IsNullOrEmpty(resourceId))
                throw new ArgumentNullException("resourceId");
            if (expire <= 0)
                throw new ArgumentNullException("expire");
            retry = option != null && option.Retry;
            if (retry)
            {
                retryInterval = option.GetCorrectRetryInterval();
                timeout = option.GetCorrectTimeout();
            }
        }

        static void InitEnviroment()
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

        IDatabase GetDB()
        {
            InitRedis();
            if (_redis == null)
                throw new Exception("RedisLock connect redis failed");
            return _redis.GetDatabase(_db);
        }

        void InitRedis()
        {
            if (_redis == null && _lastConnectExceptionTime.AddMinutes(1) < DateTime.Now)
            {
                lock (this)
                {
                    try
                    {
                        _redis = ConnectionMultiplexer.Connect(_connectStr);
                        string script = "if redis.call(\"GET\",@rid) == @lid then redis.call(\"DEL\",@rid) end";
                        LuaScript luaScript = LuaScript.Prepare(script);
                        ConfigurationOptions options = ConfigurationOptions.Parse(_connectStr);
                        options.SetDefaultPorts();
                        EndPoint point = options.EndPoints[0];
                        var server = _redis.GetServer(options.EndPoints[0]);
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
        /// <returns></returns>
        /// <remarks>
        /// 标识资源的持有者，防止资源锁被非持有者错误释放
        /// {服务器IP}{进程Id}{初始化时间戳}{全局计数器}
        /// </remarks>
        string GetRandomLockId()
        {
            int c = Interlocked.Increment(ref _count);
            return string.Format("{0}{1}", _lockIdPerfix, c);
        }

        string GetRedisLockKey(string resourceId)
        {
            return string.Format("l_{0}", resourceId);
        }
    }
}
