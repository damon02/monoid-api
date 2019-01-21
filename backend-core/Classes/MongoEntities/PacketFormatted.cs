/*______________________________*
 *_________© Monoid INC_________*
 *______PacketFormatted.cs______*
 *______________________________*/

using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace backend_core
{
    public class PacketFormatted
    {
        [JsonIgnore]
        public ObjectId Id { get; set; }
        [JsonIgnore]
        public ObjectId UserId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Risk Risk { get; set; }
        public string Reason { get; set; }
        public string SourceIp { get; set; }
        public string DestinationIp { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Protocol Protocol { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MainProtocol MainProtocol { get; set; }
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public string DestinationMacAddress { get; set; }
        public string SourceMacAddress { get; set; }
        public int PacketSize { get; set; }
        public int TimeToLive { get; set; }
        public bool HasSynFlag { get; set; }
        public bool HasAckFlag { get; set; }
        public bool HasRstFlag { get; set; }
        public string DnsRequest { get; set; }
        public bool RuleApplied { get; set; }
        public DateTime IssueDate { get; set; }

        // Only used for attack analysis no need to serialize
        [JsonIgnore]
        public int IcmpType { get; set; }
    }

    public enum MainProtocol
    {
        IP = 0,
        ICMP = 1,
        TCP = 6,
        UDP = 17
    }

    public enum Protocol
    {
        Undefined = 0,
        SSH = 1,
        Telnet = 2,
        Finger = 3,
        TFTP = 4,
        SNMP = 5,
        FTP = 6,
        SMB = 7,
        ARP = 8,
        DNS = 9,
        LLC = 10,
        STP = 11,
        HTTP = 12,
        TCP = 13,
        NBNS = 14,
        LLMNR = 15,
        SSDP = 16,
        ICMP = 17,
        TLSV1 = 18,
        TLSV11 = 19,
        TLSV12 = 20,
        UDP = 21
    }

    public enum Risk
    {
        Information = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
