/*______________________________*
 *_________© Monoid INC_________*
 *__________DataCore.cs_________*
 *______________________________*/

using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class DataCore : BaseCore
    {
        private static DataCore instance = null;
        private static readonly object padlock = new object();

        public static DataCore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DataCore();
                    }
                    return instance;
                }
            }
        }

        /// <summary> Store packets into mongodb as bsondocument </summary>
        public DataResult<Packet> StorePackets(JToken json, ObjectId uId)
        {
            List<Packet> packets = json.ConvertToPackets(uId);

            DataResult<Packet> dr = database.StorePackets(packets);

            return dr;
        }

        /// <summary> Get packets from database and format them to display risks </summary>
        public List<Packet> GetPackets(ObjectId uId, int seconds)
        {
            DataResult<Packet> dr = database.GetPackets(uId, seconds);

            if (!dr.Success) return null;

            return dr.Data;
        }

        public bool DeleteRule(ObjectId ruleId, ObjectId uId)
        {
            if (ruleId == null || uId == null) return false;

            DataResult<Rule> dr = database.DeleteRule(ruleId, uId);

            return dr.Success;
        }

        /// <summary> Store rules </summary>
        public bool StoreRule(Rule rule, ObjectId uId, string ruleId)
        {
            if (rule == null) return false;

            DataResult<Rule> dr = null;

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                dr = database.StoreRule(rule);
            }
            else
            {
                dr = database.UpdateRule(rule, ObjectId.Parse(ruleId));     
            }

            return dr.Success;
        }

        /// <summary> Get rules </summary>
        public List<Rule> GetRules(ObjectId uId)
        {
            if (uId == null) return null;

            DataResult<Rule> dr = database.GetRules(uId);

            if (!dr.Success) return null;

            return dr.Data;
        }
    }
}
