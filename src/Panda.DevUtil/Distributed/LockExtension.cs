using Panda.DevUtil.Distributed.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed
{
    public static class LockExtension
    {
        public static ILockItem UsingIgnoreException(this ILock locker, string resourceId, int expire, GetLockOption option = null)
        {
            ILockItem lockItem = null;
            try
            {
                lockItem = locker.Using(resourceId, expire, option);
            }
            catch
            {
                lockItem = new LockItem() { ResourceId = resourceId, LockId = "exception" };
            }
            return lockItem;
        }
    }
}
