/*______________________________*
 *_________© Monoid INC_________*
 *____AuthorizeController.cs____*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace BackendApi.Controllers
{
    [Route("authorize")]
    public class AuthorizeController : IApiController
    {
        private AuthorizeCore authorizeCore = AuthorizeCore.Instance;
        private readonly IOptions<AppSettings> _appSettings;

        public AuthorizeController(IOptions<AppSettings> appSettings)
        {
            this._appSettings = appSettings;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("register-user")]
        [Throttle(Name = "register", Seconds = 2)]
        /// <summary> Register a new user </summary>
        public ActionResult Register([FromBody]RegisterUserModel model)
        {
            if (model == null || (string.IsNullOrWhiteSpace(model.EmailAddress) 
                || string.IsNullOrWhiteSpace(model.Password) 
                || string.IsNullOrWhiteSpace(model.UserName))) return CreateResponse("None of the parameters can be null");

            Validator validator = new Validator();

            if (!validator.IsValidUserName(model.UserName)) return CreateResponse("Invalid username");

            if (!validator.IsValidEmail(model.EmailAddress)) return CreateResponse("Invalid email address");

            List<string> passwordErrors = validator.IsValidPassword(model.Password);

            if (passwordErrors.Count() > 0)
            {
                return CreateResponse(string.Join("\n", passwordErrors));
            }

            // Model is validated => handle 
            DataResult<User> dr = authorizeCore.RegisterUser(
                new User() { UserName = model.UserName, EmailAddress = model.EmailAddress, Password = model.Password });

            if (!dr.Success) return CreateResponse(dr.ErrorMessage);

            User createdUser = dr.Data.FirstOrDefault();

            // Initialize user settings
            UserCore.Instance.SaveSettings(new Settings { UserId = createdUser.Id, EnabledNotifications = true, NotificationRecipients = new string[] { createdUser.EmailAddress } });
            
            return CreateResponse(data: JsonConvert.SerializeObject(new { userName = createdUser.UserName, emailAddress = createdUser.EmailAddress }), success: dr.Success);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("activate-user")]
        public ActionResult ActivateUser([FromBody] ActivateUserModel model)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.Token)) return CreateResponse("None of the parameters can be null");

            DataResult<ActivationRequest> dr = authorizeCore.ActivateAccount(model.Token);

            return CreateResponse(dr.ErrorMessage, success: dr.Success);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("request-token")]
        /// <summary> Request a token for a user (Login) </summary>
        public ActionResult RequestToken([FromBody]RequestTokenModel model)
        {
            if (model == null ||
                (string.IsNullOrWhiteSpace(model.UserName))
                || string.IsNullOrWhiteSpace(model.Password)) return CreateResponse("None of the parameters can be null");

            Validator validator = new Validator();

            DataResult<User> dr = authorizeCore.Authenticate(new User() { UserName = model.UserName, Password = model.Password }, _appSettings.Value.Secret);

            if (!dr.Success) return CreateResponse(dr.ErrorMessage);

            User user = dr.Data.FirstOrDefault();

            Settings settings = UserCore.Instance.GetSettings(user.Id);

            JObject data = new JObject();
            data["user"] = JToken.FromObject(new { userName = user.UserName, token = user.Token });

            if (settings != null)
            {
                data["settings"] = JToken.FromObject(new { enabledNotifications = settings.EnabledNotifications,
                                                           notificationRecipients = settings.NotificationRecipients});
            }

            return CreateResponse(data: JsonConvert.SerializeObject(data), success: dr.Success);
        }

        [HttpGet]
        [Route("get-token")]
        /// <summary> Get an already existing token for an embedded system refresh if requested </summary>
        public ActionResult GetToken(bool refresh)
        {
            if (string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unable to get token");

            string token = string.Empty;

            if(refresh)
            {
                token = authorizeCore.GenerateToken(new User { Id = ObjectId.Parse(Context.UserId) });
            }
            else
            {
                token = authorizeCore.GetToken(ObjectId.Parse(Context.UserId));
            }

            return CreateResponse(data: JsonConvert.SerializeObject(new { token = token }), success: true);
        }
    }
}
