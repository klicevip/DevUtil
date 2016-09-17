using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed.Abstract
{
    /// <summary>
    /// 获取锁操作选项
    /// </summary>
    public class GetLockOption
    {
        /// <summary>
        /// 是否失败重试
        /// </summary>
        public bool Retry { get; set; }
        /// <summary>
        /// 重试间隔，单位毫秒
        /// </summary>
        public int RetryInterval { get; set; }
        /// <summary>
        /// 获取锁超时时间，单位毫秒
        /// </summary>
        public int Timeout { get; set; }

    }
}
