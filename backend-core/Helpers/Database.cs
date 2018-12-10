/*______________________________*
 *_________© Monoid INC_________*
 *__________Database.cs_________*
 *______________________________*/

using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public sealed class Database
    {
        private static Database instance = null;
        private static readonly object padlock = new object();

        public static Database Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Database();
                    }
                    return instance;
                }
            }
        }
        
        MongoClient mClient;
        IMongoDatabase mDatabase;
        bool mOnline = false;
        const string DB_ERROR = "Unable to establish database connection";

        public Database(MongoClient mExternalClient = null)
        {
            mClient = mExternalClient == null ? new MongoClient("mongodb://localhost:27017") : mExternalClient;
            mDatabase = mClient.GetDatabase("monoid");

            mOnline = mDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
        }

        #region Users

        /// <summary> Get a user </summary>
        public DataResult<User> GetUser(User user)
        {
            DataResult<User> result = new DataResult<User>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }
            List<User> users = new List<User>();

            if(!string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                users = GetUserCollection().Find(x => x.EmailAddress == user.EmailAddress).ToList();
            }
            else if(!string.IsNullOrWhiteSpace(user.UserName) && !string.IsNullOrWhiteSpace(user.Password))
            {
                users = GetUserCollection().Find(x => x.UserName == user.UserName
                                                        && x.Password == user.Password).ToList();
            }
            else if(user.Id != null)
            {
                users = GetUserCollection().Find(x => x.Id == user.Id).ToList();
            }

            if(users.Count > 0)
            {
                result.Data = users;
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "The combination of credentials is not found";
            }

            return result;
        }

        /// <summary> Creates a new user </summary>
        public DataResult<User> CreateUser(User user)
        {
            DataResult<User> result = new DataResult<User>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            GetUserCollection().InsertOne(user);

            User createdUser = GetUserCollection().Find(x => x.UserName == user.UserName).FirstOrDefault();
            if(createdUser != null)
            {
                result.Data = new List<User>() { createdUser };
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create the user";
            }

            return result;
        }

        /// <summary> Updates a user </summary>
        public DataResult<User> UpdateUser(User user)
        {
            DataResult<User> result = new DataResult<User>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {   
                GetUserCollection().UpdateOne(Builders<User>.Filter.Eq(x => x.Id, user.Id),
                                              Builders<User>.Update.Set(x => x.Password, user.Password)
                                                                   .Set(x => x.Token, user.Token));
                result.Success = true;
            }
            catch
            {
                result.Success = false;
                result.ErrorMessage = "Unable to update user";
            }

            return result;
        }

        #endregion

        #region Packets

        /// <summary>
        /// Save all packets from storage
        /// </summary>
        /// <param name="packets"></param>
        /// <returns></returns>
        public DataResult<Packet> StorePackets(List<Packet> packets)
        {
            DataResult<Packet> result = new DataResult<Packet>();

            if(!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetPacketCollection().InsertMany(packets);
                result.Success = true;
            }
            catch(Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }

        #endregion

        #region EndPointLog & ErrorLog
        // Errorlog store
        public DataResult<ErrorLog> StoreErrorLog(ErrorLog log)
        {
            DataResult<ErrorLog> result = new DataResult<ErrorLog>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetErrorLogCollection().InsertOne(log);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // Endpointlog store
        public DataResult<EndPointLog> StoreEndPointLog(EndPointLog log)
        {
            DataResult<EndPointLog> result = new DataResult<EndPointLog>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetEndPointLogCollection().InsertOne(log);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
        #endregion

        #region RecoveryRequest
        // RecoveryRequest store
        public DataResult<RecoveryRequest> StoreRecoveryRequest(RecoveryRequest rr)
        {
            DataResult<RecoveryRequest> result = new DataResult<RecoveryRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetRecoveryRequestCollection().InsertOne(rr);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public DataResult<RecoveryRequest> DeleteRecoveryRequest(RecoveryRequest rr)
        {
            DataResult<RecoveryRequest> result = new DataResult<RecoveryRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetRecoveryRequestCollection().DeleteOne(Builders<RecoveryRequest>.Filter.Eq(x => x.Token, rr.Token));
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // RecoveryRequest get
        public DataResult<RecoveryRequest> GetRecoveryRequest(RecoveryRequest rr)
        {
            DataResult<RecoveryRequest> result = new DataResult<RecoveryRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<RecoveryRequest> recoveryRequests = new List<RecoveryRequest>();

            if(!string.IsNullOrWhiteSpace(rr.Token))
            {
                recoveryRequests = GetRecoveryRequestCollection().Find(x => x.Token == rr.Token).ToList();
            }

            if(recoveryRequests.Count > 0)
            {
                result.Success = true;
                result.Data = recoveryRequests;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to find the request";
            }

            return result;
        }
        #endregion

        #region Helpers - Collections
        private IMongoCollection<RecoveryRequest> GetRecoveryRequestCollection()
        {
            return mDatabase.GetCollection<RecoveryRequest>("recoveryrequest");
        }

        private IMongoCollection<ErrorLog> GetErrorLogCollection()
        {
            return mDatabase.GetCollection<ErrorLog>("errorlog");
        }

        private IMongoCollection<EndPointLog> GetEndPointLogCollection()
        {
            return mDatabase.GetCollection<EndPointLog>("endpointlog");
        }

        private IMongoCollection<User> GetUserCollection()
        {
            return mDatabase.GetCollection<User>("user");
        }

        private IMongoCollection<Packet> GetPacketCollection()
        {
            return mDatabase.GetCollection<Packet>("packet");
        }
        #endregion
    }
}
