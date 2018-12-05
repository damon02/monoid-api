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

        /// <summary>
        /// Get user for authorization purposes
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public DataResult<User> GetUser(User user)
        {
            DataResult<User> result = new DataResult<User>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<User> users = GetUserCollection().Find(x => x.UserName == user.UserName
                                                        && x.Password == user.Password).ToList();

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

        /// <summary>
        /// Creates a user during the registration process
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
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

        // Helper to easily obtain the error log collection
        private IMongoCollection<ErrorLog> GetErrorLogCollection()
        {
            return mDatabase.GetCollection<ErrorLog>("errorlog");
        }

        // Helper to easily obtain the endpointlog collection
        private IMongoCollection<EndPointLog> GetEndPointLogCollection()
        {
            return mDatabase.GetCollection<EndPointLog>("endpointlog");
        }

        // Helper to easily obtain the usercollection
        private IMongoCollection<User> GetUserCollection()
        {
            return mDatabase.GetCollection<User>("user");
        }

        // Helper to easily obtain the packetcollection
        private IMongoCollection<Packet> GetPacketCollection()
        {
            return mDatabase.GetCollection<Packet>("packet");
        }
    }
}
