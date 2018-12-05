/*______________________________*
 *_________© Monoid INC_________*
 *______DataController.cs_______*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendApi.Controllers
{
    [Route("api/data")]
    public class DataController : IApiController
    {
        DataCore dataCore = DataCore.Instance;

        [HttpPost]
        [Route("store-packets")]
        public ActionResult StorePackets([FromBody]JToken json)
        {        
            DataResult<Packet> dr = dataCore.StorePackets(json);

            return CreateResponse(dr.ErrorMessage, success: dr.Success);
        }
    }
}
