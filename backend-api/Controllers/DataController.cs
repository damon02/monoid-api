﻿/*______________________________*
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

            if (json == null || json.Type == JTokenType.Null) return CreateResponse("Unable to verify json");

            DataResult<Packet> dr = dataCore.StorePackets(json, user.Id);

            Settings settings = UserCore.Instance.GetSettings(user.Id);
            List<Rule> rules = dataCore.GetRules(user.Id);

            PacketAnalyser pa = new PacketAnalyser(rules, settings, Cache);
            List<PacketFormatted> pFormatList = pa.Analyse(dr.Data);

            DataResult<PacketFormatted> drpFormat = dataCore.StorePacketsFormatted(pFormatList);

            Detector detector = new Detector(Cache, settings);
            detector.DetectSynFlood(pFormatList);
            detector.DetectPingSweep(pFormatList);
            detector.DetectPortScan(pFormatList);

            return CreateResponse(drpFormat.ErrorMessage, success: drpFormat.Success);
        }

        // Test connection for the embedded system
        [HttpGet]
        [Route("test-connection")]
        [AllowAnonymous]
        public ActionResult TestConnection([FromBody]JToken json)
        {
            if (!Request.Headers.ContainsKey("Authorization")) return CreateResponse("No authorization header present");

            string token = Request.Headers.FirstOrDefault(x => x.Key == "Authorization").Value;

            if (string.IsNullOrWhiteSpace(token)) return CreateResponse("No valid token present");

            User user = AuthorizeCore.Instance.AuthorizeToken(token);

            if (user == null) return CreateResponse("Unable to authorize given token");

            return CreateResponse(success: true);
        }

        [HttpGet]
        [Route("get-all-notifications")]
        public ActionResult GetAllNotifications()
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<DataLog> dataLogList = dataCore.GetDataLogs(ObjectId.Parse(Context.UserId));
            
            return CreateResponse(data: JsonConvert.SerializeObject(dataLogList), success: true);
        }

        [HttpGet]
        [Route("get-all-counters")]
        public ActionResult GetAllCounters()
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            Counters counters = dataCore.GetAllCounters(ObjectId.Parse(Context.UserId));

            return CreateResponse(data:JsonConvert.SerializeObject(counters), success: true);
        }

        [HttpGet]
        [Route("get-line-graph-data")]
        public ActionResult GetLineGraphData(DateTime startDateTime, DateTime endDateTime)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<LineGraphData> lineGraphData = dataCore.GetLineGraphData(ObjectId.Parse(Context.UserId), startDateTime, endDateTime);

            return CreateResponse(data: JsonConvert.SerializeObject(lineGraphData), success: true);
        }

        [HttpGet]
        [Route("get-packets")]
        public ActionResult GetPackets()
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            // Default behaviour => get from last week
            List<PacketFormatted> packets = dataCore.GetPacketsFormatted(ObjectId.Parse(Context.UserId), DateTime.Now.AddDays(-7), DateTime.Now);

            if (packets == null || packets.Count < 1) return CreateResponse("No data found");

            return CreateResponse(data: JsonConvert.SerializeObject(packets), success: true);
        }

        [HttpGet]
        [Route("get-traffic-count-ip")]
        public ActionResult GetTrafficCountIp(DateTime startDateTime, DateTime endDateTime)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<TrafficCountPerIp> data = dataCore.GetTrafficCountIp(ObjectId.Parse(Context.UserId), startDateTime, endDateTime);

            return CreateResponse(data: JsonConvert.SerializeObject(data), success: true);
        }

        [HttpGet]
        [Route("get-traffic-size-ip")]
        public ActionResult GetTrafficSizeIp(DateTime startDateTime, DateTime endDateTime)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<TrafficSizePerIp> data = dataCore.GetTrafficSizeIp(ObjectId.Parse(Context.UserId), startDateTime, endDateTime);

            return CreateResponse(data: JsonConvert.SerializeObject(data), success: true);
        }

        [HttpGet]
        [Route("get-traffic-by-protocol")]
        public ActionResult GetTrafficByProtocol(DateTime startDateTime, DateTime endDateTime)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<TrafficByProtocol> data = dataCore.GetTrafficByProtocol(ObjectId.Parse(Context.UserId), startDateTime, endDateTime);

            return CreateResponse(data: JsonConvert.SerializeObject(data), success: true);
        }

        [HttpGet]
        [Route("get-traffic-by-tlsversion")]
        public ActionResult GetTrafficByTlsVersion(DateTime startDateTime, DateTime endDateTime)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unauthorized");

            List<TrafficByTlsVersion> data = dataCore.GetTrafficByTlsVersion(ObjectId.Parse(Context.UserId), startDateTime, endDateTime);

            return CreateResponse(data: JsonConvert.SerializeObject(data), success: true);
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
                Notify = model.Notify,
                Log = model.Log,
                SourceIp = model.SourceIp,
                SourcePort = model.SourcePort,
                Message = model.Message,
                Protocol = model.Protocol,
                MainProtocol = model.MainProtocol,
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
