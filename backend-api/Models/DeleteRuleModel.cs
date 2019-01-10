using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendApi
{
    public class DeleteRuleModel
    {
        [JsonProperty("ruleId")]
        public string RuleId { get; set; }
    }
}
