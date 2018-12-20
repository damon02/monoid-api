/*______________________________*
 *_________© Monoid INC_________*
 *______DataController.cs_______*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
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
        private static MemoryCache Cache { get; set; } = new MemoryCache(new MemoryCacheOptions());

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

            Detector detector = new Detector(Cache);
            detector.DetectSynFlood(pFormatList, Context.UserId);

            return CreateResponse("WIP");
        }
    }
}
