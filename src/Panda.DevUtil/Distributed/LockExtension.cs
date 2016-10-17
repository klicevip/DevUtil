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
                lockItem = new IgnoreExceptionLockItem(locker.Using(resourceId, expire, option));
            }
            catch
            {
                lockItem = new LockItem() { ResourceId = resourceId, LockId = "exception" };
            }
            return lockItem;
        }

        public class IgnoreExceptionLockItem : ILockItem
        {
            internal IgnoreExceptionLockItem(ILockItem item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");
                Item = item;
            }

            internal ILockItem Item { get; set; }

            public string LockId
            {
                get
                {
                    return Item.LockId;
                }

                set
                {
                    Item.LockId = value;
                }
            }

            public string ResourceId
            {
                get
                {
                    return Item.ResourceId;
                }

                set
                {
                    Item.ResourceId = value;
                }
            }

            public void Dispose()
            {
                try
                {
                    Item.Dispose();
                }
                catch { }
            }
        }
    }
}
