using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Test
{
    using Distributed;
    using NUnit.Framework;
    [TestFixture]
    public class RedisLockTest : ILockTest
    {
        [SetUp]
        public override void Startup()
        {
            _lockName = "redislock,";
#if dotnetcore
            _lock = new RedisLock("127.0.0.1");
#else
            _lock = new RedisLock();
#endif
        }
    }

    [TestFixture]
    public class UseScriptRedisLockTest : ILockTest
    {
        [SetUp]
        public override void Startup()
        {
            _lockName = "usescriptredislock,";
#if dotnetcore
            _lock = new RedisLock("127.0.0.1");
#else
            _lock = new RedisLock("127.0.0.1", 0, true);
#endif
        }
    }
}
