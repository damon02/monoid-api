using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class DataLog
    {
        [JsonIgnore]
        public ObjectId Id { get; set; }
        [JsonIgnore]
        public ObjectId UserId { get; set; }
        public Risk Risk { get; set; }
        public string Message { get; set; }
    }
}
