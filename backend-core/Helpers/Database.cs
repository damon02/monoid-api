/*______________________________*
 *_________© Monoid INC_________*
 *__________Database.cs_________*
 *______________________________*/

using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

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
        
        private MongoClient mClient;
        private IMongoDatabase mDatabase;
        bool mOnline = false;
        const string DB_ERROR = "Unable to establish database connection";
        
        public Database(MongoClient mExternalClient = null)
        {
            mClient = mExternalClient == null ? new MongoClient("mongodb://localhost:27017") : mExternalClient;
            mDatabase = mClient.GetDatabase("monoid");

            mOnline = mDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
        }

        #region Users & Settings

        /// <summary> Check if username exists </summary>
        public DataResult<User> CheckUsername(string username)
        {
            DataResult<User> result = new DataResult<User>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<User> users = GetUserCollection().Find(x => x.UserName == username).ToList();

            if (users.Count > 0)
            {
                result.ErrorMessage = "This username already exists";
                result.Success = true;
            }
            else
            {
                result.Success = false;
            }

            return result;
        }

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
            else if(!string.IsNullOrWhiteSpace(user.Token))
            {
                users = GetUserCollection().Find(x => x.Token == user.Token).ToList();
            }
            else if (user.Id != null)
            {
                users = GetUserCollection().Find(x => x.Id == user.Id).ToList();
            }

            if (users.Count > 0)
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
                                                                   .Set(x => x.Token, user.Token)
                                                                   .Set(x => x.Activated, user.Activated));
                result.Success = true;
            }
            catch
            {
                result.Success = false;
                result.ErrorMessage = "Unable to update user";
            }

            return result;
        }

        /// <summary> Get settings from current user </summary>
        public DataResult<Settings> GetSettings(ObjectId uId)
        {
            DataResult<Settings> result = new DataResult<Settings>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<Settings> settings = GetSettingsCollection().Find(x => x.UserId == uId).ToList();

            if(settings.Count > 0)
            {
                result.Success = true;
                result.Data = settings;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to find settings";
            }

            return result;
        }

        /// <summary> Get settings from current user </summary>
        public DataResult<Settings> StoreSettings(Settings settings)
        {
            DataResult<Settings> result = new DataResult<Settings>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            // Delete old settings
            GetSettingsCollection().DeleteOne(Builders<Settings>.Filter.Eq(x => x.UserId, settings.UserId));

            try
            {
                GetSettingsCollection().InsertOne(settings);
                result.Success = true;
            }
            catch
            {
                result.Success = false;
                result.ErrorMessage = "Unable to store settings";
            }

            return result;
        }

        #endregion

        #region Packets

        /// <summary> Store packets into database </summary>
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
                result.Data = packets;
                result.Success = true;
            }
            catch(Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                new Logger().CreateErrorLog(ex);
            }
            
            return result;
        }

        /// <summary> Get packets based on time parameters </summary>
        public DataResult<Packet> GetPackets(ObjectId uId, int packetLimit = 2000)
        {
            DataResult<Packet> result = new DataResult<Packet>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<Packet> foundPackets = GetPacketCollection().Find(x => x.UserId == uId)
                .SortByDescending(x => x.CreationDate)
                .Limit(packetLimit).ToList();
            
            if (foundPackets.Count > 0)
            {
                result.Success = true;
                result.Data = foundPackets;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to retrieve packets";
            }

            return result;
        }

        /// <summary> Store formatted packets into database </summary>
        public DataResult<PacketFormatted> StorePacketsFormatted(List<PacketFormatted> packets)
        {
            DataResult<PacketFormatted> result = new DataResult<PacketFormatted>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetPacketFormattedCollection().InsertMany(packets);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                new Logger().CreateErrorLog(ex);
            }

            return result;
        }

        /// <summary> Get formatted packets </summary>
        public DataResult<PacketFormatted> GetFormattedPackets(ObjectId uId, DateTime startTime, DateTime endTime, int packetLimit)
        {
            DataResult<PacketFormatted> result = new DataResult<PacketFormatted>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<PacketFormatted> foundPackets = GetPacketFormattedCollection().Find(x => x.UserId == uId && x.IssueDate < endTime && x.IssueDate > startTime)
                .SortByDescending(x => x.IssueDate)
                .Limit(packetLimit).ToList();

            if (foundPackets.Count > 0)
            {
                result.Success = true;
                result.Data = foundPackets;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to retrieve packets";
            }

            return result;
        }

        /// <summary> Get total packet count </summary>
        public DataResult<long> GetPacketCount(ObjectId uId)
        {
            DataResult<long> result = new DataResult<long>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            long packetCount = GetPacketFormattedCollection().CountDocuments(Builders<PacketFormatted>.Filter.Eq(x => x.UserId, uId));

            if (packetCount > 0)
            {
                result.Success = true;
                result.Data = new List<long> { packetCount };
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to retrieve packet count";
            }

            return result;
        }

        #endregion

        #region EndPointLog & ErrorLog & DataLog
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

        // Datalog store
        public DataResult<DataLog> StoreDataLog(DataLog log)
        {
            DataResult<DataLog> result = new DataResult<DataLog>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetDataLogCollection().InsertOne(log);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public DataResult<DataLog> GetDataLogs(ObjectId uId, bool visible = true, bool ignoreVisibility = false)
        {
            DataResult<DataLog> result = new DataResult<DataLog>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }
            List<DataLog> dataLogs = new List<DataLog>();

            if (ignoreVisibility)
            {
                dataLogs = GetDataLogCollection().Find(x => x.UserId == uId).ToList();
            }
            else
            {
                dataLogs = GetDataLogCollection().Find(x => x.Visible == visible && x.UserId == uId).ToList();
            }
            

            if (dataLogs.Count > 0)
            {
                result.Success = true;
                result.Data = dataLogs;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to find any datalogs";
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

        #region ActivationRequest
        // Store activationrequest
        public DataResult<ActivationRequest> StoreActivationRequest(ActivationRequest ar)
        {
            DataResult<ActivationRequest> result = new DataResult<ActivationRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetActivationRequestCollection().InsertOne(ar);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // Delete activationrequest
        public DataResult<ActivationRequest> DeleteActivationRequest(ActivationRequest ar)
        {
            DataResult<ActivationRequest> result = new DataResult<ActivationRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetActivationRequestCollection().DeleteOne(Builders<ActivationRequest>.Filter.Eq(x => x.Token, ar.Token));
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
        public DataResult<ActivationRequest> GetActivationRequest(string token)
        {
            DataResult<ActivationRequest> result = new DataResult<ActivationRequest>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<ActivationRequest> activationRequests = GetActivationRequestCollection().Find(x => x.Token == token).ToList();

            if (activationRequests.Count > 0)
            {
                result.Success = true;
                result.Data = activationRequests;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to find the activation request";
            }

            return result;
        }
        #endregion

        #region Rules
        // Store rules
        public DataResult<Rule> StoreRule(Rule rule)
        {
            DataResult<Rule> result = new DataResult<Rule>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetRuleCollection().InsertOne(rule);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // Update rule
        public DataResult<Rule> UpdateRule(Rule rule, ObjectId ruleId)
        {
            DataResult<Rule> result = new DataResult<Rule>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            try
            {
                GetRuleCollection().UpdateOne(Builders<Rule>.Filter.Eq(x => x.Id, ruleId),
                    Builders<Rule>.Update.Set("DestIp", rule.DestIp)
                                         .Set("DestPort", rule.DestPort)
                                         .Set("SourceIp", rule.SourceIp)
                                         .Set("SourcePort", rule.SourcePort)
                                         .Set("Log", rule.Log)
                                         .Set("Notify", rule.Notify)
                                         .Set("Protocol", rule.Protocol)
                                         .Set("Risk", rule.Risk));
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // Get rules
        public DataResult<Rule> GetRules(ObjectId userId)
        {
            DataResult<Rule> result = new DataResult<Rule>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }

            List<Rule> rules = new List<Rule>();

            rules = GetRuleCollection().Find(x => x.UserId == userId).ToList();

            if (rules.Count > 0)
            {
                result.Success = true;
                result.Data = rules;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Unable to find the requested rules";
            }

            return result;
        }

        // Delete rules
        public DataResult<Rule> DeleteRule(ObjectId ruleId, ObjectId userId)
        {
            DataResult<Rule> result = new DataResult<Rule>();

            if (!mOnline)
            {
                result.Success = false;
                result.ErrorMessage = DB_ERROR;
                return result;
            }
            
            try
            {
                GetRuleCollection().DeleteOne(Builders<Rule>.Filter.Where(x => x.UserId == userId && x.Id == ruleId));
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

        #region Helpers - Collections
        private IMongoCollection<Rule> GetRuleCollection()
        {
            return mDatabase.GetCollection<Rule>("rule");
        }

        private IMongoCollection<DataLog> GetDataLogCollection()
        {
            return mDatabase.GetCollection<DataLog>("datalog");
        }

        private IMongoCollection<ActivationRequest> GetActivationRequestCollection()
        {
            return mDatabase.GetCollection<ActivationRequest>("activationrequest");
        }

        private IMongoCollection<Settings> GetSettingsCollection()
        {
            return mDatabase.GetCollection<Settings>("settings");
        }

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

        private IMongoCollection<PacketFormatted> GetPacketFormattedCollection()
        {
            return mDatabase.GetCollection<PacketFormatted>("packetformatted");
        }
        #endregion
    }
}
