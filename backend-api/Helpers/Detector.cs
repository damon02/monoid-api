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
        private const Risk SYN_RISK = Risk.Critical;
        private DateTime cTime = DateTime.Now;
        private MemoryCache Cache;
        private Mailer Mailer = new Mailer();
        private Settings Settings;

        public Detector(MemoryCache _cache, Settings _settings)
        {
            Cache = _cache;
            Settings = _settings;
        }

        /// <summary> Detect a synflood based on the Syn and Ack flags in the data </summary>
        public void DetectSynFlood(List<PacketFormatted> pFormatList, string userId)
        {
            if (pFormatList == null || pFormatList.Count < 1) return;

            // Synflood
            string key = userId + "-synflood-detection";

            int synCount = pFormatList.Count(x => x.HasSynFlag && !x.HasAckFlag);
            int synAckCount = pFormatList.Count(x => x.HasAckFlag && x.HasSynFlag);
            int ackCount = pFormatList.Count(x => x.HasAckFlag && !x.HasSynFlag);

            List<Tuple<int[], DateTime>> synFloodData;

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                                    .SetAbsoluteExpiration(new DateTimeOffset(DateTime.Now.AddMinutes(1)));

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
                synFloodData.RemoveAll(x => DateTime.Compare(x.Item2.AddMinutes(1), DateTime.Now) < 2);

                synCount += synFloodData.Sum(x => x.Item1[0]);
                synAckCount += synFloodData.Sum(x => x.Item1[1]);
                ackCount += synFloodData.Sum(x => x.Item1[2]);

                if(synCount > synAckCount && synCount > ackCount && synCount > MIN_SYNF_AMOUNT)
                {
                    string notifyKey = userId + "-synflood-detection-notified";
                    if (Cache.Get(key) == null)
                    {
                        string message = "Synflood detected in your network";
                        Mailer.SendSystemNotification(Settings, message, SYN_RISK);

                        MemoryCacheEntryOptions notifyEntryOptions = new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                        // Register entry in cache so notifications are not being spammed
                        Cache.Set(key, true, notifyEntryOptions);
                    }
                }
            }
        }
    }
}
