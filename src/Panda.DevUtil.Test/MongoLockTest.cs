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
            _lockName = "mongolock,";
            _lock = new MongoLock("mongodb://127.0.0.1/", "devutiltest");
        }
    }
}
