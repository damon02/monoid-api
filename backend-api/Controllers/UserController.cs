﻿/*______________________________*
 *_________© Monoid INC_________*
 *______UserController.cs_______*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BackendApi.Controllers
{
    [Route("user")]
    public class UserController : IApiController
    {
        private UserCore userCore = UserCore.Instance;

        [Route("password-recovery")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult RecoverPassword([FromBody] RecoverPasswordModel model)
        {
            if (model == null
                || string.IsNullOrWhiteSpace(model.Password)
                || string.IsNullOrWhiteSpace(model.Token)) return CreateResponse("None of the parameters can be null.");

            Validator validator = new Validator();

            List<string> passwordErrors = validator.IsValidPassword(model.Password);

            if (passwordErrors.Count() > 0)
            {
                return CreateResponse(string.Join("\n", passwordErrors));
            }

            DataResult<RecoveryRequest> dr = userCore.Recovery(model.Password, model.Token);

            return CreateResponse(success: dr.Success);
        }

        [Route("request-password-recovery")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult RequestPasswordRecovery([FromBody] RequestPasswordRecoveryModel model)
        {
            if (model == null
                || string.IsNullOrWhiteSpace(model.EmailAddress)) return CreateResponse("None of the parameters can be null");

            Validator validator = new Validator();

            if (!validator.IsValidEmail(model.EmailAddress)) return CreateResponse("Invalid email address");

            KeyValuePair<bool, string> kv = userCore.GenerateRecoveryLink(model.EmailAddress);

            return CreateResponse(kv.Value, success: kv.Key);
        }

        [Route("save-settings")]
        [HttpPost]
        public ActionResult SaveUserSettings([FromBody] SaveUserSettingsModel model)
        {
            if (Context == null || string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unable to get token");
            if (model == null) return CreateResponse("None of the parameters can be null");

            Validator validator = new Validator();

            bool passedEmailValidation = model.NotificationRecipients.All(x => validator.IsValidEmail(x));

            if (!passedEmailValidation) return CreateResponse("Invalid email address in recipients list");

            bool saved = userCore.SaveSettings(new Settings { EnabledNotifications = model.EnabledNotifications, NotificationRecipients = model.NotificationRecipients, UserId = ObjectId.Parse(Context.UserId) });

            return CreateResponse(success: saved);
        }

        [Route("get-settings")]
        [HttpGet]
        public ActionResult GetUserSettings()
        {
            if(string.IsNullOrWhiteSpace(Context.UserId)) return CreateResponse("Unable to get token");

            Settings settings = userCore.GetSettings(ObjectId.Parse(Context.UserId));

            return CreateResponse(data: JsonConvert.SerializeObject(new { enabledNotifications = settings.EnabledNotifications, notificationRecipients = settings.NotificationRecipients }), success: true);
        }
    }
}
