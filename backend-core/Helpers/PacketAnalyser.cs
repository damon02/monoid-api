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

        public PacketAnalyser(List<Rule> _rules)
        {
            this.Rules = _rules;
        }

        public List<PacketFormatted> Analyse(List<Packet> packets)
        {
            if (packets == null || packets.Count < 1) return null;

            List<PacketFormatted> pFormatList = new List<PacketFormatted>();

            foreach(Packet packet in packets)
            {
                BsonDocument cPacket = packet.PacketData;

                string packetIndex = cPacket["_index"].AsString;
                string packetType = cPacket["_type"].AsString;

                MainProtocol mainProtocol = MainProtocol.Undefined;
                Protocol protocol = Protocol.Undefined;
                string ipSource = null;
                string ipDest = null;
                string dnsRequest = null;
                int portSource = 0;
                int portDest = 0;
                int packetSize = 0;
                bool hasSynFlag = false;
                bool hasAckFlag = false;

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
                    ipSource = packetIp["ip_src"].AsString;
                    if(packetIp.Contains("ip_dest"))
                    {
                        ipDest = packetIp["ip_dest"].AsString;
                    }
                }

                // https://www.wireshark.org/docs/dfref/t/tcp.html
                if (packetLayers.Contains("tcp"))
                {
                    BsonDocument packetTcp = packetLayers["tcp"].AsBsonDocument;
                    portSource = Convert.ToInt32(packetTcp["tcp_srcport"].AsString);
                    portDest = Convert.ToInt32(packetTcp["tcp_dstport"].AsString);

                    BsonDocument flagTree = packetTcp["tcp_flags_tree"].AsBsonDocument;
                    hasSynFlag = flagTree["tcp_flags_syn"].AsString == "1" ? true : false;
                    hasAckFlag = flagTree["tcp_flags_ack"].AsString == "1" ? true : false;
                }
                // https://www.wireshark.org/docs/dfref/u/udp.html
                else if (packetLayers.Contains("udp"))
                {
                    BsonDocument packetUdp = packetLayers["udp"].AsBsonDocument;
                    portSource = Convert.ToInt32(packetUdp["udp_srcport"].AsString);
                    portDest = Convert.ToInt32(packetUdp["udp_dstport"].AsString);
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
                    if(packetDns.Contains("Queries"))
                    {
                        string json = packetDns["Queries"].AsBsonDocument.ToString();
                        JToken queries = JToken.Parse(json);
                        if(queries is JObject)
                        {
                            JObject dnsQueries = queries as JObject;
                            foreach(JProperty prop in dnsQueries.Properties())
                            {
                                JObject dnsRecord = prop.Value is JObject ? (JObject)prop.Value : null;

                                if (dnsRecord == null) continue;

                                if(dnsRecord.ContainsKey("dns_qry_name"))
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

                // 

                #endregion

                /*
                 * BsonDocument packetEth = packetLayers["eth"].AsBsonDocument;
                BsonDocument packetIcmp = packetLayers["icmp"].AsBsonDocument;
                BsonDocument packetLlc = packetLayers["llc"].AsBsonDocument;
                BsonDocument packetStp = packetLayers["stp"].AsBsonDocument;
                BsonDocument packetArp = packetLayers["arp"].AsBsonDocument;
                BsonDocument packetSsl = packetLayers["ssl"].AsBsonDocument;
                BsonDocument packetTcpSegments = packetLayers["tcp_segments"].AsBsonDocument;
                */

                PacketFormatted pFormatted = new PacketFormatted
                {
                    DestinationIp = ipDest,
                    DestinationPort = portDest,
                    PacketSize = packetSize,
                    Protocol = protocol,
                    MainProtocol = mainProtocol,
                    SourceIp = ipSource,
                    SourcePort = portSource,
                    HasAckFlag = hasAckFlag,
                    HasSynFlag = hasSynFlag
                };

                pFormatList.Add(pFormatted);
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
        DNS = 9
    }

    public enum Risk
    {
        Information,
        Low,
        Medium,
        High,
        Critical
    }
}
