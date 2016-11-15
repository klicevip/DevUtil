using Panda.DevUtil.Distributed.Abstract;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if !dotnetcore
using System.Configuration;
#endif
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
        ConnectionMultiplexer _redis = null;
        int _db = 0;
        string _connectStr = null;
        DateTime _lastConnectExceptionTime = DateTime.MinValue;
        LoadedLuaScript _releaseLockScript = null;
        LockBiz _biz = null;

#if !dotnetcore
        public RedisLock() : this(ConfigurationManager.AppSettings["redis"]) { }
#endif
        public RedisLock(string connectStr) : this(connectStr, 0) { }
        public RedisLock(string connectStr, int db)
        {
            _connectStr = connectStr;
            _db = db;
            _biz = new LockBiz();
            _biz._insertFunc = Insert;
            _biz._insertAsyncFunc = InsertAsync;
            _biz._deleteAction = Delete;
            _biz._deleteAsyncAction = DeleteAsync;
        }

        public bool Get(string resourceId, int expire, out string lockId, GetLockOption option = null)
        {
            return _biz.Get(resourceId, expire, out lockId, option);
        }

        public async Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null)
        {
            return await _biz.GetAsync(resourceId, expire, option);
        }

        public void Release(string resourceId, string lockId)
        {
            _biz.Release(resourceId, lockId);
        }

        public async Task ReleaseAsync(string resourceId, string lockId)
        {
            await _biz.ReleaseAsync(resourceId, lockId);
        }

        public ILockItem Using(string resourceId, int expire, GetLockOption option = null)
        {
            return _biz.Using(resourceId, expire, option);
        }

        public async Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null)
        {
            return await _biz.UsingAsync(resourceId, expire, option);
        }

        bool Insert(bool firstTry, string resourceId, string randomLockId, int expire)
        {
            var db = GetDB();
            return db.StringSet(GetRedisLockKey(resourceId), randomLockId, TimeSpan.FromMilliseconds(expire), when: When.NotExists);
        }

        async Task<bool> InsertAsync(bool firstTry, string resourceId, string randomLockId, int expire)
        {
            var db = GetDB();
            return await db.StringSetAsync(GetRedisLockKey(resourceId), randomLockId, TimeSpan.FromMilliseconds(expire), when: When.NotExists);
        }

        void Delete(string resourceId, string lockId)
        {
            var db = GetDB();
            db.ScriptEvaluate(_releaseLockScript, new { rid = GetRedisLockKey(resourceId), lid = lockId });
        }

        async Task DeleteAsync(string resourceId, string lockId)
        {
            var db = GetDB();
            await db.ScriptEvaluateAsync(_releaseLockScript, new { rid = GetRedisLockKey(resourceId), lid = lockId });
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

        string GetRedisLockKey(string resourceId)
        {
            return string.Format("l_{0}", resourceId);
        }
    }
}
