using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendApi
{
    public class RequestPasswordRecoveryModel
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }
    }
}
