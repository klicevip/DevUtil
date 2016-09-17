using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed.Abstract
{
    public interface ILockItem : IDisposable
    {
        string LockId { get; set; }

        string ResourceId { get; set; }
    }
}
