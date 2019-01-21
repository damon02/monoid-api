using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class TrafficCountPerIp
    {
        public TrafficCountPerIp(string u, int c)
        {
            this.UniqueIp = u;
            this.Count = c;
        }

        public string UniqueIp { get; set; }
        public int Count { get; set; }
    }

    public class TrafficSizePerIp
    {
        public TrafficSizePerIp(string u, long s)
        {
            this.UniqueIp = u;
            this.Size = s;
        }

        public string UniqueIp { get; set; }
        public long Size { get; set; }
    }

    public class TrafficByProtocol
    {
        public TrafficByProtocol(string p, int c)
        {
            this.Protocol = p;
            this.Count = c;
        }

        public string Protocol { get; set; }
        public int Count { get; set; }
    }

    public class TrafficByTlsVersion
    {
        public TrafficByTlsVersion(string t, int c)
        {
            this.TlsVersion = t;
            this.Count = c;
        }

        public string TlsVersion { get; set; }
        public int Count { get; set; }
    }

    public class LineGraphData
    {
        public LineGraphData(DateTime t, int c)
        {
            this.DateTime = t;
            this.Count = c;
        }

        public DateTime DateTime { get; set; }
        public int Count { get; set; }
    }

    public class Counters
    {
        public int LowRisks { get; set; }
        public int MediumRisks { get; set; }
        public int HighRisks { get; set; }
        public int CriticalRisks { get; set; }
        public int Rules { get; set; }
        public int UniqueProtocols { get; set; }
        public int Packets { get; set; }
    }
}
