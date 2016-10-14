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
        /// <summary>
        /// 锁共享
        /// </summary>
        public LockShare Share
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    /// <summary>
    /// 锁共享
    /// </summary>
    public enum LockShare
    {
        /// <summary>
        /// 不共享
        /// </summary>
        None = 0,
        /// <summary>
        /// 线程共享
        /// </summary>
        Thread = 1,
        /// <summary>
        /// 进程共享
        /// </summary>
        Process = 2,
    }
}
