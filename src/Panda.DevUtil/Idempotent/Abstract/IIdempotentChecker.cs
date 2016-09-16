using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.DevUtil.Idempotent.Abstract
{
    /// <summary>
    /// 业务幂等性校验
    /// </summary>
    interface IIdempotentChecker
    {
        /// <summary>
        /// 校验业务是否已经执行，并且返回数据
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool Check(string bizId, out byte[] data);
        /// <summary>
        /// 设置业务已经执行成功，并添加返回数据，设置幂等性过期时间
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="data"></param>
        /// <param name="expire">过期时间，单位秒</param>
        /// <returns></returns>
        bool Set(string bizId, byte[] data, long expire = 0);
        /// <summary>
        /// 异步检查幂等性
        /// </summary>
        /// <param name="bizId"></param>
        /// <returns></returns>
        Task<Tuple<bool, byte[]>> CheckAsync(string bizId);
        /// <summary>
        /// 异步设置业务执行成功
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="data"></param>
        /// <param name="expire">过期时间，单位秒</param>
        /// <returns></returns>
        Task<bool> SetAsync(string bizId, byte[] data, long expire = 0);
    }
}
