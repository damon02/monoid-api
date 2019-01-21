/*______________________________*
 *_________© Monoid INC_________*
 *________EndPointLog.cs________*
 *______________________________*/

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class EndPointLog
    {
        public int UserId { get; set; }
        public string ClientIp { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public EndPointType EndPointType { get; set; }
        public string Body { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    /// <summary>
    /// The EndPoint 
    /// </summary>
    public enum EndPointType
    {
        Default = -1,
        StorePackets = 0,
        RegisterUser = 1,
        ActivateUser = 2,
        RequestToken = 3,
        GetToken = 4,
        GetPacketCount = 5,
        GetPackets = 6,
        StoreRule = 7,
        DeleteRule = 8,
        GetRules = 9,
        PasswordRecovery = 10,
        RequestPasswordRecovery = 11,
        SaveSettings = 12,
        GetSettings = 13,
        GetTrafficCountIp = 14,
        GetTrafficSizeIp = 15
    }
}
