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
        public List<PacketFormatted> GetPackets(ObjectId uId, int seconds)
        {
            DataResult<Packet> dr = database.GetPackets(uId, seconds);

            if (!dr.Success) return null;

            PacketAnalyser pa = new PacketAnalyser(null);
            List<PacketFormatted> packetsAnalysed = pa.Analyse(dr.Data);

            return packetsAnalysed;
        }
    }
}
