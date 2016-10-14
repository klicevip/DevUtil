using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Test
{
    using Distributed;
    using NUnit.Framework;
    using System.Threading;
    [TestFixture]
    public class RedisLockTest
    {
        RedisLock _lock = null;
        [SetUp]
        public void Startup()
        {
            _lock = new RedisLock();
        }
        [Test]
        public void GetTest()
        {
            string resourceId = DateTime.Now.ToString();
            string lockId = null;
            int expire = 100;
            bool locked = _lock.Get(resourceId, expire, out lockId);
            Assert.IsTrue(!string.IsNullOrEmpty(lockId), "lockId empty");
            Assert.IsTrue(locked, "locked failed");

            string lockId1 = null;
            bool locked1 = _lock.Get(resourceId, expire, out lockId1);
            Assert.IsFalse(locked1, "locked1 failed");
            Assert.IsTrue(string.IsNullOrEmpty(lockId1), "lockId1 not empty");

            Thread.Sleep(100);
            string lockId2 = null;
            bool locked2 = _lock.Get(resourceId, expire, out lockId2);
            Assert.IsTrue(!string.IsNullOrEmpty(lockId2), "lockId2 empty");
            Assert.IsTrue(locked2, "locked2 failed");
        }

        [Test]
        public void RetryGetTest()
        {
            string resourceId = DateTime.Now.ToString();
            string lockId = null;
            int expire = 100;
            bool locked = _lock.Get(resourceId, expire, out lockId);
            Assert.IsTrue(!string.IsNullOrEmpty(lockId), "lockId empty");
            Assert.IsTrue(locked, "locked failed");

            string lockId1 = null;
            bool locked1 = _lock.Get(resourceId, expire, out lockId1, new Distributed.Abstract.GetLockOption { Retry = true, Timeout = 200 });
            Assert.IsTrue(locked1, "locked1 failed");
            Assert.IsTrue(!string.IsNullOrEmpty(lockId1), "lockId1 empty");
        }

        [Test]
        public void ReleaseTest()
        {
            string resourceId = DateTime.Now.ToString();
            string lockId = "lockId";
            int expire = 100;

            _lock.Release(resourceId, lockId);

            string lockId1 = null;
            bool locked = _lock.Get(resourceId, expire, out lockId1);
            Assert.IsTrue(locked, "locked failed");

            string lockId2 = null;
            _lock.Release(resourceId, lockId);
            bool locked2 = _lock.Get(resourceId, expire, out lockId2);
            Assert.IsFalse(locked2, "locked2 failed");

            _lock.Release(resourceId, lockId1);

            string lockId3 = null;
            bool locked3 = _lock.Get(resourceId, expire, out lockId3);
            Assert.IsTrue(locked3, "locked3 failed");
        }
    }
}
