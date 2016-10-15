using Panda.DevUtil.Distributed.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed
{
    public class LockItem : ILockItem
    {
        public string LockId
        {
            get; set;
        }

        public string ResourceId
        {
            get; set;
        }

        public ILock Lock
        {
            get; set;
        }

        public void Dispose()
        {
            if (Lock != null)
            {
                if (string.IsNullOrEmpty(ResourceId))
                    return;
                if (string.IsNullOrEmpty(LockId))
                    return;
                Lock.Release(ResourceId, LockId);
            }
        }
    }
}
