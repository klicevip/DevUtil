using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed.Abstract
{
    /// <summary>
    /// 锁
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="lockId"></param>
        /// <param name="expire"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        bool Get(string resourceId, int expire, out string lockId, GetLockOption option = null);
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockId"></param>
        void Release(string resourceId, string lockId);
        /// <summary>
        /// using操作锁
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="expire"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        ILockItem Using(string resourceId, int expire, GetLockOption option = null);
        /// <summary>
        /// 异步获取锁
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="expire"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null);
        /// <summary>
        /// 异步释放锁
        /// </summary>
        /// <param name="lockId"></param>
        /// <returns></returns>
        Task ReleaseAsync(string resourceId, string lockId);
        /// <summary>
        /// 异步using锁
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="expire"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null);
    }
}
