using Panda.DevUtil.Idempotent.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Configuration;

namespace Panda.DevUtil.Idempotent
{
    public class RedisIdempotentChecker : IIdempotentChecker
    {
        ConnectionMultiplexer _redis = null;
        int _db = 0;
        public RedisIdempotentChecker() : this(ConfigurationManager.AppSettings["redis"])
        {
        }

        public RedisIdempotentChecker(string connectStr) : this(connectStr, 0)
        {
        }

        public RedisIdempotentChecker(string connectStr, int db)
        {
            _redis = ConnectionMultiplexer.Connect(connectStr);
            _db = db;
        }

        ~RedisIdempotentChecker()
        {
            if (_redis != null)
                _redis.Dispose();
        }

        public bool Check(string bizId, out byte[] data)
        {
            if (string.IsNullOrEmpty(bizId))
            {
                data = null;
                return false;
            }
            var v = GetDB().StringGet(bizId);
            data = (byte[])v;
            return !v.IsNull;
        }

        public async Task<Tuple<bool, byte[]>> CheckAsync(string bizId)
        {
            if (string.IsNullOrEmpty(bizId))
            {
                return new Tuple<bool, byte[]>(false, null);
            }
            var v = await GetDB().StringGetAsync(bizId);
            return new Tuple<bool, byte[]>(!v.IsNull, (byte[])v);
        }


        public bool Set(string bizId, byte[] data, long expire = 0)
        {
            if (string.IsNullOrEmpty(bizId))
                return false;
            if (data == null || data.Length == 0)
            {
                data = new byte[] { 0 };
            }
            TimeSpan? e = null;
            if (expire > 0)
            {
                e = TimeSpan.FromSeconds(expire);
            }
            return GetDB().StringSet(bizId, data, expiry: e);
        }

        public async Task<bool> SetAsync(string bizId, byte[] data, long expire = 0)
        {
            if (string.IsNullOrEmpty(bizId))
                return false;
            if (data == null || data.Length == 0)
            {
                data = new byte[] { 0 };
            }
            TimeSpan? e = null;
            if (expire > 0)
            {
                e = TimeSpan.FromSeconds(expire);
            }
            return await GetDB().StringSetAsync(bizId, data, expiry: e);
        }

        private IDatabase GetDB()
        {
            return _redis.GetDatabase(_db);
        }
    }
}
