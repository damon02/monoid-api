/*______________________________*
 *_________© Monoid INC_________*
 *______PacketFormatted.cs______*
 *______________________________*/

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class PacketFormatted
    {
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
        public bool HasSynFlag { get; set; }
        public bool HasAckFlag { get; set; }
        public bool HasRstFlag { get; set; }
        public string DnsRequest { get; set; }
        public bool RuleApplied { get; set; }
    }
}
