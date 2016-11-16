using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Panda.DevUtil.Test
{
    using Distributed;
    using NUnit.Framework;
    [TestFixture]
    public class MongoLockTest : ILockTest
    {
        [SetUp]
        public override void Startup()
        {
            _lock = new MongoLock("mongodb://muser:1qaz2wsx@218.244.136.30:27018/", "devutiltest");
        }
    }
}
