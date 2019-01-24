/*______________________________*
 *_________© Monoid INC_________*
 *__________DataCore.cs_________*
 *______________________________*/

using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary> Store formatted packets </summary>
        public DataResult<PacketFormatted> StorePacketsFormatted(List<PacketFormatted> packets)
        {
            DataResult<PacketFormatted> dr = database.StorePacketsFormatted(packets);

            return dr;
        }

        /// <summary> Get raw packets from database and format them to display risks </summary>
        public List<Packet> GetPackets(ObjectId uId, int seconds)
        {
            DataResult<Packet> dr = database.GetPackets(uId, seconds);

            if (!dr.Success) return null;

            return dr.Data;
        }

        /// <summary> Get formatted packets </summary>
        public List<PacketFormatted> GetPacketsFormatted(ObjectId uId, DateTime startTime, DateTime endTime, int packetLimit = 2000)
        {
            DataResult<PacketFormatted> dr = database.GetFormattedPackets(uId, startTime, endTime, packetLimit);

            if (!dr.Success) return null;

            return dr.Data;
        }

        public List<DataLog> GetDataLogs(ObjectId uId)
        {
            DataResult<DataLog> dr = database.GetDataLogs(uId, ignoreVisibility: true);

            if (!dr.Success) return null;

            return dr.Data;
        }

        #region Graph data

        public List<LineGraphData> GetLineGraphData(ObjectId uId, DateTime startTime, DateTime endTime)
        {
            List<LineGraphData> response = new List<LineGraphData>();

            List<PacketFormatted> packets = GetPacketsFormatted(uId, startTime, endTime, int.MaxValue);

            TimeSpan interval = new TimeSpan(0, 10, 0);

            if (packets == null || packets.Count < 1) return response;

            response = (from p in packets
                               group p by p.IssueDate.Ticks / interval.Ticks
                               into g
                               select new LineGraphData(new DateTime(g.Key * interval.Ticks), g.Count())).ToList();

            return response;
        }

        public List<TrafficCountPerIp> GetTrafficCountIp(ObjectId uId, DateTime startTime, DateTime endTime)
        {
            List<TrafficCountPerIp> response = new List<TrafficCountPerIp>();

            List<PacketFormatted> packets = GetPacketsFormatted(uId, startTime, endTime, int.MaxValue);

            if (packets == null || packets.Count < 1) return response;

            List<string> destIps = packets.Select(x => x.DestinationIp).Distinct().ToList();

            List<string> sourceIps = packets.Select(x => x.SourceIp).Distinct().ToList();

            List<string> combinedIps = destIps.Union(sourceIps).ToList();

            foreach(string uniqueIp in combinedIps)
            {
                int count = packets.Count(x => x.SourceIp == uniqueIp);
                count = count + packets.Count(x => x.DestinationIp == uniqueIp);

                response.Add(new TrafficCountPerIp(uniqueIp, count));
            }

            return response;
        }

        public List<TrafficSizePerIp> GetTrafficSizeIp(ObjectId uId, DateTime startTime, DateTime endTime)
        {
            List<TrafficSizePerIp> response = new List<TrafficSizePerIp>();

            List<PacketFormatted> packets = GetPacketsFormatted(uId, startTime, endTime, int.MaxValue);

            if (packets == null || packets.Count < 1) return response;

            List<string> destIps = packets.Select(x => x.DestinationIp).Distinct().ToList();

            List<string> sourceIps = packets.Select(x => x.SourceIp).Distinct().ToList();

            List<string> combinedIps = destIps.Union(sourceIps).ToList();

            foreach (string uniqueIp in combinedIps)
            {
                long size = packets.Where(x => x.DestinationIp == uniqueIp).Sum(x => x.PacketSize);
                size = size + packets.Where(x => x.SourceIp == uniqueIp).Sum(x => x.PacketSize);

                response.Add(new TrafficSizePerIp(uniqueIp, size));
            }

            return response;
        }

        public List<TrafficByProtocol> GetTrafficByProtocol(ObjectId uId, DateTime startTime, DateTime endTime)
        {
            List<TrafficByProtocol> response = new List<TrafficByProtocol>();

            List<PacketFormatted> packets = GetPacketsFormatted(uId, startTime, endTime, int.MaxValue);

            if (packets == null || packets.Count < 1) return response;

            List<Protocol> protocols = packets.Select(x => x.Protocol).Distinct().ToList();

            foreach (Protocol protocol in protocols)
            {
                int count = packets.Where(x => x.Protocol == protocol).Count();

                response.Add(new TrafficByProtocol(protocol.ToString(), count));
            }

            return response;
        }

        public List<TrafficByTlsVersion> GetTrafficByTlsVersion(ObjectId uId, DateTime startTime, DateTime endTime)
        {
            List<TrafficByTlsVersion> response = new List<TrafficByTlsVersion>();

            List<PacketFormatted> packets = GetPacketsFormatted(uId, startTime, endTime, int.MaxValue);

            if (packets == null || packets.Count < 1) return null;

            List<Protocol> protocols = packets.Select(x => x.Protocol).Where(x => x == Protocol.TLSV1 || x == Protocol.TLSV11 || x == Protocol.TLSV12).Distinct().ToList();

            foreach (Protocol protocol in protocols)
            {
                int count = packets.Where(x => x.Protocol == protocol).Count();

                response.Add(new TrafficByTlsVersion(protocol.ToString(), count));
            }

            return response;
        }

        /// <summary> Get total packet count </summary>
        public Counters GetAllCounters(ObjectId uId)
        {
            List<PacketFormatted> packets = GetPacketsFormatted(uId, DateTime.Now.AddYears(-100), DateTime.Now, int.MaxValue);

            Counters counters = new Counters();

            if (packets == null || packets.Count < 1) return counters;

            // Risk from rules
            counters.LowRisks = packets.Where(x => x.Risk == Risk.Low).Count();
            counters.MediumRisks = packets.Where(x => x.Risk == Risk.Medium).Count();
            counters.HighRisks = packets.Where(x => x.Risk == Risk.High).Count();
            counters.CriticalRisks = packets.Where(x => x.Risk == Risk.Critical).Count();

            // Alternative risk

            DataResult<DataLog> dr = database.GetDataLogs(uId, ignoreVisibility: true);
            if(dr.Success)
            {
                List<DataLog> dataLogList = dr.Data;
                counters.LowRisks += dataLogList.Where(x => x.Risk == Risk.Low).Count();
                counters.MediumRisks += dataLogList.Where(x => x.Risk == Risk.Medium).Count();
                counters.HighRisks += dataLogList.Where(x => x.Risk == Risk.High).Count();
                counters.CriticalRisks += dataLogList.Where(x => x.Risk == Risk.Critical).Count();
            }

            counters.UniqueProtocols = packets.Select(x => x.Protocol).Distinct().Count();

            List<Rule> rules = GetRules(uId);

            counters.Rules = rules != null ? rules.Count : 0;

            counters.Packets = packets.Count;

            return counters;
        }

        #endregion

        /// <summary> Delete a given rule </summary>
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
