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

        public static long GetTimeStampMillisecond(DateTime time)
        {
            if (time == DateTime.MinValue)
                return 0;
            return (long)(time - _startTime).TotalMilliseconds;
        }
    }
}
