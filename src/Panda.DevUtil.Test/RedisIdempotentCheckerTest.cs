using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Test
{
    using Idempotent;
    using NUnit.Framework;
    using System.Threading;
    [TestFixture]
    public class RedisIdempotentCheckerTest
    {
        RedisIdempotentChecker _checker = null;
        [SetUp]
        public void Startup()
        {
            _checker = new RedisIdempotentChecker();
        }
        [Test]
        public void CheckTest()
        {
            string bizId = DateTime.Now.ToString();
            byte[] data = new byte[] { 1, 2, 3, 4 };
            byte[] outdata = null;
            bool exist = _checker.Check(bizId, out outdata);
            Assert.IsFalse(exist);

            bool success = _checker.Set(bizId, data, 1);
            Assert.IsTrue(success);

            
            exist = _checker.Check(bizId, out outdata);
            Assert.IsTrue(exist);
            Assert.IsTrue(outdata.Length == data.Length);
            for (int i = 0; i < outdata.Length; i++)
            {
                Assert.AreEqual(data[i], outdata[i]);
            }

            Thread.Sleep(1000);
            exist = _checker.Check(bizId, out outdata);
            Assert.IsFalse(exist);
            Assert.IsNull(outdata);
        }
    }
}
