/*______________________________*
 *_________© Monoid INC_________*
 *______PacketAnalyser.cs_______*
 *______________________________*/

using backend_core;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackendApi
{
    public class PacketAnalyser
    {
        private List<Rule> Rules { get; set; }
        private Logger Logger = new Logger();
        private Mailer Mailer = new Mailer();
        private Settings Settings { get; set; }
        private MemoryCache Cache { get; set; }
        private const int GLOBAL_RULE_COOLDOWN = 3600;

        public PacketAnalyser(List<Rule> _rules, Settings _settings, MemoryCache _cache)
        {
            this.Rules = _rules;
            this.Settings = _settings;
            this.Cache = _cache;
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

                    MainProtocol mainProtocol = MainProtocol.IP;
                    Protocol protocol = Protocol.Undefined;
                    string sourceIp = null;
                    string destIp = null;
                    string dnsRequest = null;
                    string sourceMac = null;
                    string destMac = null;
                    int sourcePort = 0;
                    int destPort = 0;
                    int packetSize = 0;
                    int timeToLive = 0;
                    bool hasSynFlag = false;
                    bool hasAckFlag = false;
                    bool hasRstFlag = false;
                    int icmpType = 0;
                    DateTime issueDate = DateTime.Now;

                    BsonDocument packetSource = cPacket["_source"].AsBsonDocument;
                    BsonDocument packetLayers = packetSource["layers"].AsBsonDocument;

                    // https://www.wireshark.org/docs/dfref/f/frame.html
                    if (packetLayers.Contains("frame"))
                    {
                        BsonDocument packetFrame = packetLayers["frame"].AsBsonDocument;
                        packetSize = Convert.ToInt32(packetFrame["frame_len"].AsString);
                        try
                        {
                            issueDate = Convert.ToDateTime(packetFrame["frame_time"].AsString);
                        }
                        catch { }
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

                        if(packetIp.Contains("ip_ttl"))
                        {
                            timeToLive = Convert.ToInt32(packetIp["ip_ttl"].AsString);
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
                    else if (packetLayers.Contains("icmp"))
                    {
                        BsonDocument packetIcmp = packetLayers["icmp"].AsBsonDocument;
                        icmpType = Convert.ToInt32(packetIcmp["icmp_type"].AsString);
                    }

                    if (packetLayers.Contains("http"))
                    {
                        BsonDocument packetHttp = packetLayers["http"].AsBsonDocument;
                        protocol = Protocol.HTTP;
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

                    // https://www.wireshark.org/docs/dfref/s/stp.html
                    else if (packetLayers.Contains("stp"))
                    {
                        protocol = Protocol.STP;
                    } 

                    // https://www.wireshark.org/docs/dfref/n/nbns.html
                    else if (packetLayers.Contains("nbns"))
                    {
                        protocol = Protocol.NBNS;
                    }

                    // No Wireshark documentation available
                    else if (packetLayers.Contains("llmnr"))
                    {
                        protocol = Protocol.LLMNR;
                    }

                    // No Wireshark documentation available
                    else if (packetLayers.Contains("ssdp"))
                    {
                        protocol = Protocol.SSDP;
                    }

                    else if(packetLayers.Contains("ssl"))
                    {
                        BsonDocument packetSsl = packetLayers["ssl"].AsBsonDocument;
                        BsonDocument packetSslRecord = packetSsl["ssl_record"].AsBsonDocument;
                        string rawversion = packetSslRecord["ssl_record_version"].AsString;

                        // Apparently this is how to determine TLS version (wireshark)
                        if (rawversion.Last() == '3')
                        {
                            protocol = Protocol.TLSV12;
                        }
                        else if (rawversion.Last() == '2')
                        {
                            protocol = Protocol.TLSV11;
                        }
                        else if (rawversion.Last() == '1')
                        {
                            protocol = Protocol.TLSV1;
                        }
                    }

                    if(mainProtocol == MainProtocol.TCP && protocol == Protocol.Undefined)
                    {
                        if(packetLayers.ElementCount == 4 
                            || (packetLayers.ElementCount == 5 && packetLayers.Contains("transum"))
                            || (packetLayers.ElementCount == 5 && packetLayers.Contains("data")))
                        {
                            protocol = Protocol.TCP;
                        }
                    }
                    else if(mainProtocol == MainProtocol.ICMP && protocol == Protocol.Undefined)
                    {
                        if(packetLayers.ElementCount == 4)
                        {
                            protocol = Protocol.ICMP;
                        }
                    }
                    else if(mainProtocol == MainProtocol.UDP && protocol == Protocol.Undefined)
                    {
                        if(packetLayers.ElementCount == 4
                            || (packetLayers.ElementCount == 5 && packetLayers.Contains("db-lsp-disc")))
                        {
                            protocol = Protocol.UDP;
                        }
                    }

                    // 

                    #endregion

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
                    string message = null;
                    if (appliedRule != null)
                    {
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

                        risk = appliedRule.Risk;

                        // Rule has been applied -> register entry in Cache so the notifications are not being spammed
                        string ruleKey = Settings.UserId.ToString() + "-" + appliedRule.Id + "-applied";
                        bool applyRule = true;
                        if(Cache.TryGetValue(ruleKey, out bool applied))
                        {
                            applyRule = false;  
                        }

                        if(applyRule)
                        {
                            if (appliedRule.Notify)
                            {
                                string title = "Rule condition met";
                                // Send email
                                Mailer.SendSystemNotification(Settings, message, risk, title);

                                if (appliedRule.Log)
                                {
                                    // Store a log entry
                                    Logger.CreateDataLog(Settings.UserId, message, risk);
                                }
                            }
                            else if (appliedRule.Log)
                            {
                                // Store a log entry
                                Logger.CreateDataLog(Settings.UserId, message, risk);
                            }

                            if(!appliedRule.Log)
                            {
                                Logger.CreateDataLog(Settings.UserId, message, risk, visible:false);
                            }

                            // Register cache entry
                            MemoryCacheEntryOptions ruleCacheOptions = new MemoryCacheEntryOptions()
                                .SetAbsoluteExpiration(TimeSpan.FromSeconds(GLOBAL_RULE_COOLDOWN));

                            Cache.Set(ruleKey, true, ruleCacheOptions);
                        }
                    }

                    PacketFormatted pFormatted = new PacketFormatted
                    {
                        UserId = Settings.UserId,
                        DestinationIp = destIp,
                        DestinationPort = destPort,
                        DestinationMacAddress = destMac,
                        PacketSize = packetSize,
                        TimeToLive = timeToLive,
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
                        Reason = message,
                        RuleApplied = appliedRule != null,
                        IssueDate = issueDate,
                        IcmpType = icmpType
                    };

                    pFormatList.Add(pFormatted);
                }
                catch(Exception ex)
                {
                    // DO nothing
                    //Logger.CreateErrorLog(ex);
                }
            }

            return pFormatList;
        }
    }
}
