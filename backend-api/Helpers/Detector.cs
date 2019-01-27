/*______________________________*
 *_________© Monoid INC_________*
 *__________Detector.cs_________*
 *______________________________*/

using backend_core;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackendApi
{
    public class Detector
    {
        private const int MIN_SYNF_AMOUNT = 1000;
        private const int MIN_PORT_AMOUNT = 100;
        private const Risk SYN_RISK = Risk.Critical;
        private const Risk SYN_RISK_KEEPUP = Risk.High;
        private const Risk PING_SWEEP_RISK = Risk.Low;
        private const Risk SYN_SCAN_RISK = Risk.Medium;
        private DateTime cTime = DateTime.Now;
        private MemoryCache Cache;
        private Mailer Mailer = new Mailer();
        private Logger Logger = new Logger();
        private Settings Settings;

        public Detector(MemoryCache _cache, Settings _settings)
        {
            Cache = _cache;
            Settings = _settings;
        }

        /// <summary> Detect a synflood based on the Syn and Ack flags in the data </summary>
        public void DetectSynFlood(List<PacketFormatted> pFormatList)
        {
            if (pFormatList == null || pFormatList.Count < 1) return;

            // Synflood
            string key = Settings.UserId + "-synflood-detection";

            int synCount = pFormatList.Count(x => x.HasSynFlag && !x.HasAckFlag);
            int synAckCount = pFormatList.Count(x => x.HasAckFlag && x.HasSynFlag);
            int ackCount = pFormatList.Count(x => x.HasAckFlag && !x.HasSynFlag);

            List<Tuple<int[], DateTime>> synFloodData;

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                                    .SetAbsoluteExpiration(new DateTimeOffset(cTime.AddMinutes(1)));

            if (Cache.Get(key) == null)
            {
                synFloodData = new List<Tuple<int[], DateTime>>() { new Tuple<int[], DateTime>(new int[] { synCount, synAckCount, ackCount }, cTime) };
                Cache.Set(key, synFloodData, cacheEntryOptions);
            }
            else
            {
                synFloodData = (List<Tuple<int[], DateTime>>)Cache.Get(key);
                synFloodData.Add(new Tuple<int[], DateTime>(new int[] { synCount, synAckCount, ackCount }, cTime));

                Cache.Set(key, synFloodData, cacheEntryOptions);
            }

            if(synFloodData.Count > 0)
            {
                synFloodData.RemoveAll(x => DateTime.Compare(x.Item2.AddMinutes(1), cTime) < 1);

                synCount = synFloodData.Sum(x => x.Item1[0]);
                synAckCount = synFloodData.Sum(x => x.Item1[1]);
                ackCount = synFloodData.Sum(x => x.Item1[2]);

                if(synCount > ackCount && synCount > MIN_SYNF_AMOUNT)
                {
                    string message = null;
                    Risk riskLevel = Risk.Information;
                    if(synCount > synAckCount)
                    {
                        riskLevel = SYN_RISK;
                        message = "Synflood detected in your network.";
                    }
                    else
                    {
                        riskLevel = SYN_RISK_KEEPUP;
                        message = "Synflood detected in your network but the network is keeping up.";
                    }

                    string notifyKey = Settings.UserId + "-synflood-detection-notified";
                    if (Cache.Get(notifyKey) == null)
                    {
                        Mailer.SendSystemNotification(Settings, message, riskLevel, "Synflood detected");

                        Logger.CreateDataLog(Settings.UserId, message, riskLevel);

                        MemoryCacheEntryOptions notifyEntryOptions = new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                        // Register entry in cache so notifications are not being spammed
                        Cache.Set(notifyKey, true, notifyEntryOptions);
                    }
                }
            }
        }

        public void DetectPingSweep(List<PacketFormatted> pFormatList)
        {
            if (pFormatList == null || pFormatList.Count < 1) return;

            string key = Settings.UserId + "-pingsweep-detection";

            string[] IcmpIps = null;
            bool tcpPing = false;
            bool udpPing = false;
            string possibleSourceIp = null;

            IcmpIps = pFormatList.Where(x => x.MainProtocol == MainProtocol.ICMP && (x.IcmpType == 8 || x.IcmpType == 0)).Select(x => x.SourceIp).ToArray();

            if(IcmpIps != null && IcmpIps.Count() > 0)
            {
                possibleSourceIp = IcmpIps.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
            }  

            tcpPing = pFormatList.Any(x => x.MainProtocol == MainProtocol.TCP && x.DestinationPort == 7);

            if(tcpPing)
            {
                PacketFormatted pfTcp = pFormatList.FirstOrDefault(x => x.MainProtocol == MainProtocol.TCP && x.DestinationPort == 7);
                possibleSourceIp = pfTcp?.SourceIp;
            }

            udpPing = pFormatList.Any(x => x.MainProtocol == MainProtocol.UDP && x.DestinationPort == 7);

            if(udpPing)
            {
                PacketFormatted pfUdp = pFormatList.FirstOrDefault(x => x.MainProtocol == MainProtocol.TCP && x.DestinationPort == 7);
                possibleSourceIp = pfUdp?.SourceIp;
            }

            if(!string.IsNullOrWhiteSpace(possibleSourceIp) && Cache.TryGetValue(key, out bool notify))
            {
                string message = "Your network is possibly being ping sweeped by the following IP: "+possibleSourceIp;
                Mailer.SendSystemNotification(Settings, message, PING_SWEEP_RISK, "Ping sweep detected");

                Logger.CreateDataLog(Settings.UserId, message, PING_SWEEP_RISK);

                MemoryCacheEntryOptions notifyEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                Cache.Set(key, true, notifyEntryOptions);
            }
        }

        public void DetectPortScan(List<PacketFormatted> pFormatList)
        {
            if (pFormatList == null || pFormatList.Count < 1) return;

            string key = Settings.UserId + "-synscan-detection";

            string[] sourceIps = pFormatList.Where(x => x.HasSynFlag && !x.HasAckFlag && !x.HasRstFlag).Select(x => x.SourceIp).ToArray();

            if (sourceIps == null || sourceIps.Length < 1) return;

            string possibleSourceIp = sourceIps.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;

            int synCount = pFormatList.Count(x => x.HasSynFlag && !x.HasAckFlag && !x.HasRstFlag && x.SourceIp == possibleSourceIp);

            // Amount of responses from open ports
            int synAckCount = pFormatList.Count(x => x.HasSynFlag && x.HasAckFlag && x.DestinationIp == possibleSourceIp);

            int rstCountFromAttacker = pFormatList.Count(x => x.HasRstFlag && x.SourceIp == possibleSourceIp);
            int rstCountFromHost = pFormatList.Count(x => x.HasRstFlag && x.DestinationIp == possibleSourceIp);

            List<Tuple<int[], DateTime>> portScanData;

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                                    .SetAbsoluteExpiration(new DateTimeOffset(cTime.AddMinutes(2)));

            if (Cache.Get(key) == null)
            {
                portScanData = new List<Tuple<int[], DateTime>>() { new Tuple<int[], DateTime>(new int[] { synCount, synAckCount, rstCountFromAttacker, rstCountFromHost }, cTime) };
                Cache.Set(key, portScanData, cacheEntryOptions);
            }
            else
            {
                portScanData = (List<Tuple<int[], DateTime>>)Cache.Get(key);
                portScanData.Add(new Tuple<int[], DateTime>(new int[] { synCount, synAckCount, rstCountFromAttacker, rstCountFromHost }, cTime));

                Cache.Set(key, portScanData, cacheEntryOptions);
            }

            if(portScanData.Count > 0)
            {
                portScanData.RemoveAll(x => DateTime.Compare(x.Item2.AddMinutes(2), DateTime.Now) < 1);

                synCount = portScanData.Sum(x => x.Item1[0]);
                synAckCount = portScanData.Sum(x => x.Item1[1]);
                rstCountFromAttacker = portScanData.Sum(x => x.Item1[2]);
                rstCountFromHost = portScanData.Sum(x => x.Item1[3]);

                if(synCount > MIN_PORT_AMOUNT)
                {
                    // Possible attack ongoing.
                    string notifyKey = Settings.UserId + "-synscan-detection-notified";
                    if (Cache.Get(notifyKey) == null)
                    {
                        string message = "Syn scan detected in your network: Open ports response: ("+Convert.ToString(synAckCount)+")";
                        Mailer.SendSystemNotification(Settings, message, SYN_SCAN_RISK, "Syn scan detected");

                        Logger.CreateDataLog(Settings.UserId, message, SYN_SCAN_RISK);

                        MemoryCacheEntryOptions notifyEntryOptions = new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                        // Register entry in cache so notifications are not being spammed
                        Cache.Set(notifyKey, true, notifyEntryOptions);
                    }
                }
            }
        }
    }
}
