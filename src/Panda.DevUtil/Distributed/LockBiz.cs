using Panda.DevUtil.Distributed.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panda.DevUtil.Distributed
{
    internal class LockBiz : ILock
    {
        internal static int _initTime = 0;
        internal static int _processId = 0;
        internal static int _serverIP = 0;
        internal static int _count = 0;
        internal static string _lockIdPerfix = null;
        static LockBiz()
        {
            InitEnviroment();
        }

        internal Func<bool, string, string, int, bool> _insertFunc;
        internal Func<bool, string, string, int, Task<bool>> _insertAsyncFunc;
        internal Action<string, string> _deleteAction;
        internal Func<string, string, Task> _deleteAsyncAction;
        internal Func<string, int, string> _generateLockIdFunc;

        public bool Get(string resourceId, int expire, out string lockId, GetLockOption option = null)
        {
            lockId = null;
            bool got = false;
            bool firstGet = true;
            bool retry = false;
            int retryInterval = 0;
            int retryTimeout = 0;
            CheckGetArgument(resourceId, expire, option, out retry, out retryInterval, out retryTimeout);
            TimeSpan? e = TimeSpan.FromMilliseconds(expire);
            string randomLockId = null;
            if (_generateLockIdFunc == null)
                randomLockId = GetRandomLockId(resourceId, expire);
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                if (!firstGet)
                    Thread.Sleep(retryInterval);
                if (_generateLockIdFunc != null)
                    randomLockId = GetRandomLockId(resourceId, expire);
                got = _insertFunc(firstGet, resourceId, randomLockId, expire);
                firstGet = false;
            } while (!got && retry && watch.ElapsedMilliseconds < retryTimeout);
            if (got)
                lockId = randomLockId;
            return got;
        }

        public async Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null)
        {
            string lockId = null;
            bool got = false;
            bool firstGet = true;
            bool retry = false;
            int retryInterval = 0;
            int retryTimeout = 0;
            CheckGetArgument(resourceId, expire, option, out retry, out retryInterval, out retryTimeout);
            TimeSpan? e = TimeSpan.FromMilliseconds(expire);
            string randomLockId = null;
            if (_generateLockIdFunc == null)
                randomLockId = GetRandomLockId(resourceId, expire);
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                if (!firstGet)
                    Thread.Sleep(retryInterval);
                if (_generateLockIdFunc != null)
                    randomLockId = GetRandomLockId(resourceId, expire);
                got = await _insertAsyncFunc(firstGet, resourceId, randomLockId, expire);
                firstGet = false;
            } while (!got && retry && watch.ElapsedMilliseconds < retryTimeout);
            if (got)
                lockId = randomLockId;
            return new Tuple<bool, string>(got, lockId);
        }

        public void Release(string resourceId, string lockId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return;
            if (string.IsNullOrEmpty(lockId))
                return;
            _deleteAction(resourceId, lockId);
        }

        public async Task ReleaseAsync(string resourceId, string lockId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return;
            if (string.IsNullOrEmpty(lockId))
                return;
            await _deleteAsyncAction(resourceId, lockId);
        }

        public ILockItem Using(string resourceId, int expire, GetLockOption option = null)
        {
            string lockId = null;
            Get(resourceId, expire, out lockId, option);
            return new LockItem { ResourceId = resourceId, LockId = lockId, Lock = this };
        }

        public async Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null)
        {
            Tuple<bool, string> gotAndLockId = await GetAsync(resourceId, expire, option);
            return new LockItem { ResourceId = resourceId, LockId = gotAndLockId.Item2, Lock = this };
        }

        void CheckGetArgument(string resourceId, int expire, GetLockOption option, out bool retry, out int retryInterval, out int timeout)
        {
            retry = false;
            retryInterval = 0;
            timeout = 0;
            if (string.IsNullOrEmpty(resourceId))
                throw new ArgumentNullException("resourceId");
            if (expire <= 0)
                throw new ArgumentNullException("expire");
            retry = option != null && option.Retry;
            if (retry)
            {
                retryInterval = option.GetCorrectRetryInterval();
                timeout = option.GetCorrectTimeout();
            }
        }

        /// <summary>
        /// 生成随机锁Id
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 标识资源的持有者，防止资源锁被非持有者错误释放
        /// {服务器IP}{进程Id}{初始化时间戳}{全局计数器}
        /// </remarks>
        string GetRandomLockId(string resourceId, int expire)
        {
            if (_generateLockIdFunc != null)
                return _generateLockIdFunc(resourceId, expire);
            int c = Interlocked.Increment(ref _count);
            return string.Format("{0}{1}", _lockIdPerfix, c);
        }

        static void InitEnviroment()
        {
            DateTime now = DateTime.Now;
            _initTime = now.Hour * 60 * 60 + now.Minute * 60 + now.Second;
            _processId = Process.GetCurrentProcess().Id;

            string hostname = Dns.GetHostName();//得到本机名   
#if dotnetcore
            IPHostEntry localhost = Dns.GetHostEntryAsync(hostname).Result;
#else
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
#endif
            IPAddress localaddr = localhost.AddressList[0];
            _serverIP = localaddr.GetHashCode();
            _lockIdPerfix = string.Format("{0}{1}{2}", _serverIP, _processId, _initTime);
        }
    }
}
