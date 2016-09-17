using Panda.DevUtil.Distributed.Abstract;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed
{
    public class RedisLock : ILock
    {

        [ThreadStaticAttribute]
        static Dictionary<string, string> threadLockDict = new Dictionary<string, string>();

        ConnectionMultiplexer _redis = null;

        public RedisLock() : this(ConfigurationManager.AppSettings["redis"]) { }
        public RedisLock(string connectStr)
        {
            _redis = ConnectionMultiplexer.Connect(connectStr);
        }
        public bool Get(string resourceId, long expire, out string lockId, GetLockOption option = null)
        {
            lockId = null;
            if (string.IsNullOrEmpty(resourceId))
                return false;
            if (expire < 0)
                return false;
            if (threadLockDict.TryGetValue(resourceId, out lockId))
            {
                return true;
            }

            Stopwatch watch = Stopwatch.StartNew();
            var db = _redis.GetDatabase();
            string rad = "rad";
            bool got = db.StringSet(resourceId, rad, null, when:When.NotExists);
            if (got)
            {
                lockId = rad;
                threadLockDict.Add(resourceId, lockId);
                return true;
            }
            if (option == null || !option.Retry)
                return false;
            int interval = 100;
            if (option.RetryInterval > 0)
            {
                interval = option.RetryInterval;
            }
            int timeout = 1000;
            if (option.Timeout > 0)
            {
                timeout = option.Timeout;
            }
            while (!got)
            {
                if (watch.ElapsedMilliseconds >= timeout)
                    break;
                Thread.Sleep(interval);
                got = db.StringSet(resourceId, rad, null, when:When.NotExists);
            }
            if (got)
            {
                lockId = rad;
                threadLockDict.Add(resourceId, lockId);
            }
            return got;
        }

        public Task<Tuple<bool, string>> GetAsync(string resourceId, long expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }

        public void Release(string lockId)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseAsync(string lockId)
        {
            throw new NotImplementedException();
        }

        public ILockItem Using(string resourceId, long expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }

        public Task<ILockItem> UsingAsync(string resourceId, long expire, GetLockOption option = null)
        {
            throw new NotImplementedException();
        }
    }
}
