/*______________________________*
 *_________© Monoid INC_________*
 *______DataController.cs_______*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendApi.Controllers
{
    [Route("data")]
    public class DataController : IApiController
    {
        private DataCore dataCore = DataCore.Instance;
        private static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        [HttpPost]
        [Route("store-packets")]
        [AllowAnonymous]
        public ActionResult StorePackets([FromBody]JToken json)
        {
            if (!Request.Headers.ContainsKey("Authorization")) return CreateResponse("No authorization header present");

            string token = Request.Headers.FirstOrDefault(x => x.Key == "Authorization").Value;

            if (string.IsNullOrWhiteSpace(token)) return CreateResponse("No valid token present");

            User user = AuthorizeCore.Instance.AuthorizeToken(token);

            if (user == null) return CreateResponse("Unable to authorize given token");

            Logger logger = new Logger();
            logger.CreateEndPointLog(this.Context, json.ToString(), EndPointType.StorePackets);

            DataResult<Packet> dr = dataCore.StorePackets(json, user.Id);

            return CreateResponse(dr.ErrorMessage, success: dr.Success);
        }

        [HttpGet]
        [Route("get-packets")]
        public ActionResult GetPackets(int seconds = 5)
        {
            if (string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<PacketFormatted> pFormatList = dataCore.GetPackets(ObjectId.Parse(Context.UserId), seconds);

            Settings settings = UserCore.Instance.GetSettings(ObjectId.Parse(Context.UserId));

            Detector detector = new Detector(Cache, settings);
            detector.DetectSynFlood(pFormatList, Context.UserId);

            return CreateResponse(data: JsonConvert.SerializeObject(pFormatList), success: true);
        }

        [HttpPost]
        [Route("store-rule")]
        /// <summary> Updates or creates a new rule </summary>
        public ActionResult StoreRule([FromBody]SingleRuleModel model)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");
            if (model == null) return CreateResponse("Invalid model");

            ObjectId userId = ObjectId.Parse(Context.UserId);
            string ruleId = string.IsNullOrWhiteSpace(model.RuleId) ? null : model.RuleId;

            Rule rule = new Rule
            {
                DestPort = model.DestPort,
                DestIp = model.DestIp,
                SourceIp = model.SourceIp,
                SourcePort = model.SourcePort,
                Message = model.Message,
                Protocol = model.Protocol,
                Risk = model.Risk,
                UserId = userId
            };

            bool success = dataCore.StoreRule(rule, userId, ruleId);

            return CreateResponse(success: success);
        }

        [HttpPost]
        [Route("delete-rule")]
        public ActionResult DeleteRule([FromBody]DeleteRuleModel model)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) CreateResponse("Unauthorized");
            if (model == null || string.IsNullOrWhiteSpace(model.RuleId)) CreateResponse("Invalid model");

            bool success = dataCore.DeleteRule(ObjectId.Parse(model.RuleId), ObjectId.Parse(Context.UserId));

            return CreateResponse(success: success);
        }

        [HttpGet]
        [Route("get-rules")]
        public ActionResult GetRules()
        {
            if (string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<Rule> rules = dataCore.GetRules(ObjectId.Parse(Context.UserId));
            
            return CreateResponse(data: JsonConvert.SerializeObject(rules), success: true);
        }
    }
}
