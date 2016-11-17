using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Panda.DevUtil
{
    public static class CommonUtil
    {
#if dotnetcore
        //todo use time zone
        static DateTime _startTime = new DateTime(1970, 1, 1, 8, 0, 0, DateTimeKind.Local);
#else
        static DateTime _startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
#endif
        public static long GetTimeStamp(DateTime time)
        {
            if (time == DateTime.MinValue)
                return 0;
            return (long)(time - _startTime).Seconds;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <param name="time"></param>
        /// <example>
        /// 1479287206543
        /// </example>
        /// <returns></returns>
        public static long GetTimeStampMillisecond(DateTime time)
        {
            if (time == DateTime.MinValue)
                return 0;
            return (long)(time - _startTime).TotalMilliseconds;
        }
        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        public static DateTime FromTimeStampMillisecond(long stamp)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(stamp);
            return _startTime.Add(span);
        }
    }
}
