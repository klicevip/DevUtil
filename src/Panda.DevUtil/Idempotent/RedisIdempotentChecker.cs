﻿using Panda.DevUtil.Idempotent.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
#if !dotnetcore
using System.Configuration;
#endif

namespace Panda.DevUtil.Idempotent
{
    public class RedisIdempotentChecker : IIdempotentChecker
    {
        ConnectionMultiplexer _redis = null;
        int _db = 0;
#if !dotnetcore
        public RedisIdempotentChecker() : this(ConfigurationManager.AppSettings["redis"])
        {
        }
#endif
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
            var v = GetDB().StringGet(GetRedisKey(bizId));
            data = (byte[])v;
            return !v.IsNull;
        }

        public async Task<Tuple<bool, byte[]>> CheckAsync(string bizId)
        {
            if (string.IsNullOrEmpty(bizId))
            {
                return new Tuple<bool, byte[]>(false, null);
            }
            var v = await GetDB().StringGetAsync(GetRedisKey(bizId));
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
            return GetDB().StringSet(GetRedisKey(bizId), data, expiry: e);
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
            return await GetDB().StringSetAsync(GetRedisKey(bizId), data, expiry: e);
        }

        IDatabase GetDB()
        {
            return _redis.GetDatabase(_db);
        }

        string GetRedisKey(string bizId)
        {
            return string.Format("ic_{0}", bizId);
        }
    }
}
