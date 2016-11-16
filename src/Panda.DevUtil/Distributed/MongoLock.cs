using Panda.DevUtil.Distributed.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

namespace Panda.DevUtil.Distributed
{
    public class MongoLock : ILock
    {

        LockBiz _lockBiz = null;
        string _connectStr = null;
        string _database = null;
        string _collection = null;
        DateTime _lastConnectExceptionTime = DateTime.MinValue;
        FilterDefinitionBuilder<MongoLockDocument> _filterBuilder = new FilterDefinitionBuilder<MongoLockDocument>();
        UpdateDefinitionBuilder<MongoLockDocument> _updateBuilder = new UpdateDefinitionBuilder<MongoLockDocument>();
        FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument> _updateOption = new FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>();
        
        IMongoCollection<MongoLockDocument> _col = null;
        public MongoLock(string mongoConnectString, string database) : this(mongoConnectString, database, "_mongolock")
        { }

        public MongoLock(string mongoConnectString, string database, string collection)
        {
            _connectStr = mongoConnectString;
            _database = database;
            _collection = collection;

            _updateOption.IsUpsert = true;

            _lockBiz = new LockBiz();
            _lockBiz._insertFunc = Insert;
            _lockBiz._insertAsyncFunc = InsertAsync;
            _lockBiz._deleteAction = Delete;
            _lockBiz._deleteAsyncAction = DeleteAsync;
        }

        public bool Get(string resourceId, int expire, out string lockId, GetLockOption option = null)
        {
            return _lockBiz.Get(resourceId, expire, out lockId, option);
        }

        public async Task<Tuple<bool, string>> GetAsync(string resourceId, int expire, GetLockOption option = null)
        {
            return await _lockBiz.GetAsync(resourceId, expire, option);
        }

        public void Release(string resourceId, string lockId)
        {
            _lockBiz.Release(resourceId, lockId);
        }

        public async Task ReleaseAsync(string resourceId, string lockId)
        {
            await _lockBiz.ReleaseAsync(resourceId, lockId);
        }

        public ILockItem Using(string resourceId, int expire, GetLockOption option = null)
        {
            return _lockBiz.Using(resourceId, expire, option);
        }

        public async Task<ILockItem> UsingAsync(string resourceId, int expire, GetLockOption option = null)
        {
            return await _lockBiz.UsingAsync(resourceId, expire, option);
        }

        bool Insert(bool firstTry, string resourceId, string randomLockId, int expire)
        {
            var col = GetCollection();
            long now = CommonUtil.GetTimeStampMillisecond(DateTime.Now);
            var filter = _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"Id", resourceId);
            filter = _filterBuilder.And(filter, _filterBuilder.Lte((FieldDefinition<MongoLockDocument, long>)"Exp", now));
            var update = _updateBuilder.Set((FieldDefinition<MongoLockDocument, long>)"Exp", now + expire).Set((FieldDefinition<MongoLockDocument, string>)"LockId", randomLockId);
            try
            {
                MongoLockDocument result = col.FindOneAndUpdate(filter, update, _updateOption);
                return true;
            }
            catch (MongoDuplicateKeyException)
            {
                return false;
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError == null || ex.WriteError.Code != 11000)//非重复键错误
                    throw;
                return false;
            }
            catch (MongoCommandException cmdex)
            {
                if (cmdex == null || cmdex.Code != 11000)//非重复键错误
                    throw;
                return false;
            }
        }

        async Task<bool> InsertAsync(bool firstTry, string resourceId, string randomLockId, int expire)
        {
            var col = GetCollection();
            long now = CommonUtil.GetTimeStampMillisecond(DateTime.Now);
            var filter = _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"Id", resourceId);
            filter = _filterBuilder.And(filter, _filterBuilder.Lte((FieldDefinition<MongoLockDocument, long>)"Exp", now));
            var update = _updateBuilder.Set((FieldDefinition<MongoLockDocument, long>)"Exp", now + expire).Set((FieldDefinition<MongoLockDocument, string>)"LockId", randomLockId);
            try
            {
                MongoLockDocument result = await col.FindOneAndUpdateAsync(filter, update, _updateOption);
                return true;
            }
            catch (MongoDuplicateKeyException)
            {
                return false;
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError == null || ex.WriteError.Code != 11000)//非重复键错误
                    throw;
                return false;
            }
            catch (MongoCommandException cmdex)
            {
                if (cmdex == null || cmdex.Code != 11000)//非重复键错误
                    throw;
                return false;
            }
        }

        void Delete(string resourceId, string lockId)
        {
            var col = GetCollection();
            var filter = _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"Id", resourceId);
            filter = _filterBuilder.And(filter, _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"LockId", lockId));
            col.DeleteOne(filter);
        }

        async Task DeleteAsync(string resourceId, string lockId)
        {
            var col = GetCollection();
            var filter = _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"Id", resourceId);
            filter = _filterBuilder.And(filter, _filterBuilder.Eq((FieldDefinition<MongoLockDocument, string>)"LockId", lockId));
            await col.DeleteOneAsync(filter);
        }

        IMongoCollection<MongoLockDocument> GetCollection()
        {
            InitMongo();
            if (_col == null)
                throw new Exception(string.Format("MongoLock connect mongodb failed,{0}, {1}, {2}", _connectStr, _database, _collection));
            return _col;
        }

        void InitMongo()
        {
            if (_col == null && _lastConnectExceptionTime.AddMinutes(1) < DateTime.Now)
            {
                lock (this)
                {
                    if (_col == null && _lastConnectExceptionTime.AddMinutes(1) < DateTime.Now)
                    {
                        try
                        {
                            MongoClient client = new MongoClient(_connectStr);
                            var db = client.GetDatabase(_database);
                            _col = db.GetCollection<MongoLockDocument>(_collection);
                        }
                        catch
                        {
                            _lastConnectExceptionTime = DateTime.Now;
                            _col = null;
                        }
                    }
                }
            }
        }

        class MongoLockDocument
        {
            /// <summary>
            /// 资源id
            /// </summary>
            [BsonId]
            public string Id { get; set; }
            /// <summary>
            /// 过期时间戳
            /// </summary>
            public long Exp { get; set; }
            /// <summary>
            /// 锁id
            /// </summary>
            public string LockId { get; set; }
        }
    }
}
