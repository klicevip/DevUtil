using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Panda.DevUtil.Test
{
    using Distributed;
    using Distributed.Abstract;
    using NUnit.Framework;
    using System.Threading;

    public abstract class ILockTest
    {
        protected ILock _lock = null;

        public abstract void Startup();

        [Test]
        public void GetTest()
        {
            string resourceId = GetResourceId();
            int expire = 100;
            Task<Tuple<bool, string>>[] ts = new Task<Tuple<bool, string>>[3];
            var parResult = Parallel.For(0, 3, i => ts[i] = _lock.GetAsync(resourceId, expire));
            while (!parResult.IsCompleted)
            {
                Thread.Sleep(expire);
            }
            Task.WaitAll(ts);

            int getCount = ts.Count(t => t.Result.Item1);
            Assert.AreEqual(1, getCount, "get count failed");

            Thread.Sleep(expire);
            string lockId2 = null;
            bool locked2 = _lock.Get(resourceId, expire, out lockId2);
            Assert.IsTrue(!string.IsNullOrEmpty(lockId2), "lockId2 empty");
            Assert.IsTrue(locked2, "locked2 failed");
        }

        [Test]
        public void RetryGetTest()
        {
            string resourceId = GetResourceId();
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
            string resourceId = GetResourceId();
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

        [Test]
        public void UsingTest()
        {
            string resourceId = GetResourceId();
            int expire = 100;
            using (var lockItem = _lock.Using(resourceId, expire))
            {
                Assert.IsFalse(string.IsNullOrEmpty(lockItem.LockId), "lockId empty");
            }
        }

        [Test]
        public async Task RetryGetAsyncTest()
        {
            string resourceId = GetResourceId();
            int expire = 50;
            var gotAndLockItem = await _lock.GetAsync(resourceId, expire);
            Assert.IsTrue(!string.IsNullOrEmpty(gotAndLockItem.Item2), "lockId empty");
            Assert.IsTrue(gotAndLockItem.Item1, "locked failed");

            var locked1 = await _lock.GetAsync(resourceId, expire, new Distributed.Abstract.GetLockOption { Retry = true, Timeout = 100 });
            Assert.IsTrue(locked1.Item1, "locked1 failed");
            Assert.IsTrue(!string.IsNullOrEmpty(locked1.Item2), "lockId1 empty");
        }

        static int _count;
        private string GetResourceId()
        {
            int count = Interlocked.Increment(ref _count);
            return string.Format("{0}{1}", DateTime.Now, count);
        }
    }
}
