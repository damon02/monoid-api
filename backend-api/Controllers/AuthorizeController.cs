/*______________________________*
 *_________© Monoid INC_________*
 *____AuthorizeController.cs____*
 *______________________________*/

using backend_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BackendApi.Controllers
{
    [Route("api/authorize")]
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

            return CreateResponse(data: JsonConvert.SerializeObject(new { userName = createdUser.UserName, emailAddress = createdUser.EmailAddress }), success: dr.Success);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("request-token")]
        public ActionResult Token([FromBody]RequestTokenModel model)
        {
            if (model == null ||
                (string.IsNullOrWhiteSpace(model.UserName))
                || string.IsNullOrWhiteSpace(model.Password)) return CreateResponse("None of the parameters can be null");

            Validator validator = new Validator();

            DataResult<User> dr = authorizeCore.Authenticate(new User() { UserName = model.UserName, Password = model.Password }, _appSettings.Value.Secret);

            return CreateResponse(dr.ErrorMessage, JsonConvert.SerializeObject(new { userName = dr.Data.FirstOrDefault().UserName, token = dr.Data.FirstOrDefault().Token }), dr.Success);
        }
    }
}
