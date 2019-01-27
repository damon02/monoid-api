/*______________________________*
 *_________© Monoid INC_________*
 *____________Rule.cs___________*
 *______________________________*/

using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class Rule
    {
        public ObjectId Id { get; set; }
        [JsonIgnore]
        public ObjectId UserId { get; set; }
        public MainProtocol MainProtocol { get; set; }
        public Protocol Protocol { get; set; }
        public string[] DestIp { get; set; }
        public string[] SourceIp { get; set; }
        public int[] DestPort { get; set; }
        public int[] SourcePort { get; set; }
        public bool Notify { get; set; }
        public bool Log { get; set; }
        public string Message { get; set; }
        public Risk Risk { get; set; }
    }
}