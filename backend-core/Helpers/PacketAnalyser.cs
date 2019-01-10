/*______________________________*
 *_________© Monoid INC_________*
 *______PacketAnalyser.cs_______*
 *______________________________*/

using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace backend_core
{
    public class PacketAnalyser
    {
        private List<Rule> Rules { get; set; }
        private Logger Logger = new Logger();
        private Mailer Mailer = new Mailer();
        private Settings Settings { get; set; }

        public PacketAnalyser(List<Rule> _rules, Settings _settings)
        {
            this.Rules = _rules;
            this.Settings = _settings;
        }

        public List<PacketFormatted> Analyse(List<Packet> packets)
        {
            if (packets == null || packets.Count < 1) return null;

            List<PacketFormatted> pFormatList = new List<PacketFormatted>();

            foreach(Packet packet in packets)
            {
                try
                {
                    BsonDocument cPacket = packet.PacketData;

                    string packetIndex = cPacket["_index"].AsString;
                    string packetType = cPacket["_type"].AsString;

                    MainProtocol mainProtocol = MainProtocol.Undefined;
                    Protocol protocol = Protocol.Undefined;
                    string sourceIp = null;
                    string destIp = null;
                    string dnsRequest = null;
                    string sourceMac = null;
                    string destMac = null;
                    int sourcePort = 0;
                    int destPort = 0;
                    int packetSize = 0;
                    bool hasSynFlag = false;
                    bool hasAckFlag = false;
                    bool hasRstFlag = false;

                    BsonDocument packetSource = cPacket["_source"].AsBsonDocument;
                    BsonDocument packetLayers = packetSource["layers"].AsBsonDocument;

                    // https://www.wireshark.org/docs/dfref/f/frame.html
                    if (packetLayers.Contains("frame"))
                    {
                        BsonDocument packetFrame = packetLayers["frame"].AsBsonDocument;
                        packetSize = Convert.ToInt32(packetFrame["frame_len"].AsString);
                    }

                    // https://www.wireshark.org/docs/dfref/i/ip.html
                    if (packetLayers.Contains("ip"))
                    {
                        BsonDocument packetIp = packetLayers["ip"].AsBsonDocument;
                        mainProtocol = (MainProtocol)Convert.ToInt32(packetIp["ip_proto"].AsString);
                        sourceIp = packetIp["ip_src"].AsString;
                        if (packetIp.Contains("ip_dst"))
                        {
                            destIp = packetIp["ip_dst"].AsString;
                        }
                    }

                    if (packetLayers.Contains("eth"))
                    {
                        BsonDocument packetEth = packetLayers["eth"].AsBsonDocument;
                        BsonDocument ethSource = packetEth["eth_src_tree"].AsBsonDocument;
                        sourceMac = ethSource["eth_addr"].AsString;
                        BsonDocument ethDest = packetEth["eth_dst_tree"].AsBsonDocument;
                        destMac = ethDest["eth_addr"].AsString;
                    }

                    // https://www.wireshark.org/docs/dfref/t/tcp.html
                    if (packetLayers.Contains("tcp"))
                    {
                        BsonDocument packetTcp = packetLayers["tcp"].AsBsonDocument;
                        sourcePort = Convert.ToInt32(packetTcp["tcp_srcport"].AsString);
                        destPort = Convert.ToInt32(packetTcp["tcp_dstport"].AsString);

                        BsonDocument flagTree = packetTcp["tcp_flags_tree"].AsBsonDocument;
                        hasSynFlag = flagTree["tcp_flags_syn"].AsString == "1" ? true : false;
                        hasAckFlag = flagTree["tcp_flags_ack"].AsString == "1" ? true : false;
                        hasRstFlag = flagTree["tcp_flags_reset"].AsString == "1" ? true : false;
                    }
                    // https://www.wireshark.org/docs/dfref/u/udp.html
                    else if (packetLayers.Contains("udp"))
                    {
                        BsonDocument packetUdp = packetLayers["udp"].AsBsonDocument;
                        sourcePort = Convert.ToInt32(packetUdp["udp_srcport"].AsString);
                        destPort = Convert.ToInt32(packetUdp["udp_dstport"].AsString);
                    }

                    #region Protocols

                    // https://www.wireshark.org/docs/dfref/s/ssh.html
                    if (packetLayers.Contains("ssh"))
                    {
                        // SSH packet data is encrypted and therefore not accessible
                        protocol = Protocol.SSH;
                    }

                    // https://www.wireshark.org/docs/dfref/d/dns.html
                    else if (packetLayers.Contains("dns"))
                    {
                        BsonDocument packetDns = packetLayers["dns"].AsBsonDocument;
                        if (packetDns.Contains("Queries"))
                        {
                            string json = packetDns["Queries"].AsBsonDocument.ToString();
                            JToken queries = JToken.Parse(json);
                            if (queries is JObject)
                            {
                                JObject dnsQueries = queries as JObject;
                                foreach (JProperty prop in dnsQueries.Properties())
                                {
                                    JObject dnsRecord = prop.Value is JObject ? (JObject)prop.Value : null;

                                    if (dnsRecord == null) continue;

                                    if (dnsRecord.ContainsKey("dns_qry_name"))
                                    {
                                        dnsRequest = Convert.ToString(dnsRecord["dns_qry_name"]);
                                    }
                                }
                            }
                        }

                        protocol = Protocol.DNS;
                    }

                    // https://www.wireshark.org/docs/dfref/t/telnet.html
                    else if (packetLayers.Contains("telnet"))
                    {
                        protocol = Protocol.Telnet;
                    }

                    // https://www.wireshark.org/docs/dfref/f/finger.html
                    else if (packetLayers.Contains("finger"))
                    {
                        protocol = Protocol.Finger;
                    }

                    // https://www.wireshark.org/docs/dfref/t/tftp.html
                    else if (packetLayers.Contains("tftp"))
                    {
                        protocol = Protocol.TFTP;
                    }

                    // https://www.wireshark.org/docs/dfref/s/snmp.html
                    else if (packetLayers.Contains("snmp"))
                    {
                        protocol = Protocol.SNMP;
                    }

                    // https://www.wireshark.org/docs/dfref/f/ftp.html
                    else if (packetLayers.Contains("ftp"))
                    {
                        protocol = Protocol.FTP;
                    }

                    // https://www.wireshark.org/docs/dfref/s/smb.html
                    else if (packetLayers.Contains("smb"))
                    {
                        protocol = Protocol.SMB;
                    }

                    // https://www.wireshark.org/docs/dfref/a/arp.html
                    else if (packetLayers.Contains("arp"))
                    {
                        protocol = Protocol.ARP;
                    }

                    // https://www.wireshark.org/docs/dfref/l/llc.html
                    else if (packetLayers.Contains("llc"))
                    {
                        protocol = Protocol.LLC;
                    }

                    else if (packetLayers.Contains("stp"))
                    {
                        protocol = Protocol.STP;
                    }


                    // 

                    #endregion

                    /*
                    BsonDocument packetEth = packetLayers["eth"].AsBsonDocument;
                    BsonDocument packetIcmp = packetLayers["icmp"].AsBsonDocument;
                    BsonDocument packetLlc = packetLayers["llc"].AsBsonDocument;
                    BsonDocument packetStp = packetLayers["stp"].AsBsonDocument;
                    BsonDocument packetArp = packetLayers["arp"].AsBsonDocument;
                    BsonDocument packetSsl = packetLayers["ssl"].AsBsonDocument;
                    BsonDocument packetTcpSegments = packetLayers["tcp_segments"].AsBsonDocument;
                    */

                    // Apply rules
                    Risk risk = Risk.Information;
                    Rule appliedRule = null;

                    // Determine which rule is most suitable for current packet
                    if(Rules != null && Rules.Count > 0)
                    {
                        foreach (Rule rule in Rules)
                        {
                            if (rule.Protocol == mainProtocol)
                            {
                                if (rule.DestIp.Contains(destIp) && rule.SourceIp.Contains(sourceIp))
                                {
                                    if (rule.DestPort.Contains(destPort) && rule.SourcePort.Contains(sourcePort))
                                    {
                                        appliedRule = rule;
                                    }
                                }
                            }
                        }
                    }

                    // Execute most suitable rule
                    if (appliedRule != null)
                    {
                        string message = null;
                        if (appliedRule.Message.Contains("*|") && appliedRule.Message.Contains("|*"))
                        {
                            // Apply string formats
                            message = appliedRule.Message.Replace("*|DEST_IP|*", destIp)
                                .Replace("*|SOURCE_IP|*", sourceIp)
                                .Replace("*|DEST_PORT|*", Convert.ToString(destPort))
                                .Replace("*|SOURCE_PORT|*", Convert.ToString(sourcePort));
                        }
                        else
                        {
                            message = appliedRule.Message;
                        }

                        if (appliedRule.Notify)
                        {
                            // Send email
                            Mailer.SendSystemNotification(Settings, message, appliedRule.Risk);

                            if (appliedRule.Log)
                            {
                                // also write log

                            }
                        }
                        else if (appliedRule.Log)
                        {
                            // Write log

                        }

                        risk = appliedRule.Risk;
                    }

                    PacketFormatted pFormatted = new PacketFormatted
                    {
                        DestinationIp = destIp,
                        DestinationPort = destPort,
                        DestinationMacAddress = destMac,
                        PacketSize = packetSize,
                        Protocol = protocol,
                        MainProtocol = mainProtocol,
                        SourceIp = sourceIp,
                        SourcePort = sourcePort,
                        SourceMacAddress = sourceMac,
                        HasAckFlag = hasAckFlag,
                        HasSynFlag = hasSynFlag,
                        HasRstFlag = hasRstFlag,
                        DnsRequest = dnsRequest,
                        Risk = risk,
                        RuleApplied = appliedRule != null
                    };

                    pFormatList.Add(pFormatted);
                }
                catch(Exception ex)
                {
                    Logger.CreateErrorLog(ex);
                }
            }

            return pFormatList;
        }
    }

    public enum MainProtocol
    {
        Undefined = 0,
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
        STP = 11
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
