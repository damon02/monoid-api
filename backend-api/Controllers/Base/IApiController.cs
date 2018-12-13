/*______________________________*
 *_________© Monoid INC_________*
 *_______IApiController.cs______*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BackendApi.Controllers
{
    [PreProcess(Order = 1)]
    [ExceptionHandling]
    [Authorize]
    public partial class IApiController : Controller
    {
        public EndPointContext Context = new EndPointContext();
        public const string JSON_CONVERT_ERROR = "Invalid JSON supplied";

        /// <summary> Default response </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        public JsonResult CreateResponse(string message = null, string data = null, bool success = false)
        {
            JToken jsonData = null;
            if (!string.IsNullOrWhiteSpace(data))
            {
                jsonData = JToken.Parse(data);
            }

            JObject response = new JObject
            {
                ["success"] = success,
                ["message"] = message,
                ["data"] = jsonData
            };

            return new JsonResult(response);
        }

        /// <summary> Determine the IP address of the current request </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        public string GetClientIP()
        {
            string clientIp = string.Empty;

            if (this.Request.HttpContext?.Connection?.RemoteIpAddress != null)
            {
                clientIp = this.Request.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return clientIp;
        }        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ThrottleAttribute : ActionFilterAttribute
    {
        public string Name { get; set; }
        public int Seconds { get; set; }
        public string Message { get; set; }

        private static MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        public override void OnActionExecuting(ActionExecutingContext c)
        {
            string key = string.Concat(Name, "-", c.HttpContext.Request.HttpContext.Connection.RemoteIpAddress);

            if (!Cache.TryGetValue(key, out bool entry))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(Seconds));

                Cache.Set(key, true, cacheEntryOptions);
            }
            else
            {
                if (string.IsNullOrEmpty(Message))
                    Message = "You are unable to execute this request.";

                c.Result = new JsonResult(new { success = false, message = Message, data = string.Empty });
            }
        }
    }

    /// <summary> Perform actions on initial load of the base controller. </summary>
    public class PreProcessAttribute : Attribute, IActionFilter, IOrderedFilter
    {
        public int Order { get; set; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            (context.Controller as IApiController).Context.ClientIP = (context.Controller as IApiController).GetClientIP();

            if (context.HttpContext?.User != null && context.HttpContext?.User.Identity != null)
            {
                ClaimsIdentity identity = (ClaimsIdentity)context.HttpContext.User.Identity;

                // Parse claims to context
                IEnumerable<Claim> claims = identity.Claims;
                if(claims != null && claims.Count() > 0)
                {
                    (context.Controller as IApiController).Context.UserName = claims.FirstOrDefault(x => x.Type == "userName").Value;
                    (context.Controller as IApiController).Context.UserId = claims.FirstOrDefault(x => x.Type == "id").Value;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (string.IsNullOrWhiteSpace((context.Controller as IApiController).Context.ClientIP))
            {
                throw new Exception("Unable to determine client IP");
            }
        }
    }

    /// <summary>
    /// Override the onexception to write a log and return a generic message to the user.
    /// </summary>
    public class ExceptionHandling : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            Logger logger = new Logger();

            logger.CreateErrorLog(context.Exception);
            context.Result = new JsonResult(new {success = false, message = "Exception has occured", data = string.Empty});
        }
    }
}
