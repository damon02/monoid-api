/*______________________________*
 *_________© Monoid INC_________*
 *_______AuthorizeCore.cs_______*
 *______________________________*/

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace backend_core
{
    public sealed class AuthorizeCore : BaseCore
    {
        private static AuthorizeCore instance = null;
        private static readonly object padlock = new object();

        public static AuthorizeCore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AuthorizeCore();
                    }
                    return instance;
                }
            }
        }
        
        /// <summary>
        /// Register a user 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public DataResult<User> RegisterUser(User user)
        {
            Validator validator = new Validator();

            user.CreationDate = DateTime.Now;

            string hashedPassword = validator.PasswordHasher(user);

            user.Password = hashedPassword;

            user.Activated = true;

            DataResult<User> dr = database.CreateUser(user);

            // Todo => sent an email to the user before the account is activated


            return dr;
        }

        public DataResult<User> Authenticate(User user, string appSecret)
        {
            Validator validator = new Validator();
            user.Password = validator.PasswordHasher(user);

            DataResult<User> dr = database.GetUser(user);

            if (!dr.Success) return dr;

            User authorizedUser = dr.Data.FirstOrDefault();

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] secretKey = Encoding.ASCII.GetBytes(appSecret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("userName", authorizedUser.UserName),
                    new Claim("id", authorizedUser.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            user.Password = null;

            dr.Data = new List<User>() { user };

            return dr;           
        }

        /// <summary> Recovery process -> validate token -> save new password </summary>
        public DataResult<RecoveryRequest> Recovery(string newPassword, string token)
        {
            DataResult<RecoveryRequest> drDefaultResponse = new DataResult<RecoveryRequest>() { Success = false, ErrorMessage = "Unable to process your recovery request" };
            DataResult<RecoveryRequest> drRecoveryRequest = database.GetRecoveryRequest(new RecoveryRequest { Token = token });

            if (!drRecoveryRequest.Success) return drDefaultResponse;

            RecoveryRequest recoveryRequest = drRecoveryRequest.Data.FirstOrDefault();

            if (recoveryRequest == null) return drDefaultResponse;

            if (DateTime.Compare(recoveryRequest.ExpiryDate, DateTime.Now) > 1) return drDefaultResponse;

            DataResult<User> drUser = database.GetUser(new User { Id = recoveryRequest.UserId });

            if (!drUser.Success) return drDefaultResponse;

            User user = drUser.Data.FirstOrDefault();

            if (user == null) return drDefaultResponse;

            Validator validator = new Validator();
            user.Password = newPassword;
            string hashedPassword = validator.PasswordHasher(user);
            user.Password = hashedPassword;

            DataResult<User> drUserUpdated = database.UpdateUserPassword(user);

            if (!drUserUpdated.Success) return drDefaultResponse;

            DataResult<RecoveryRequest> drDeleteRecoveryRequest = database.DeleteRecoveryRequest(recoveryRequest);

            return drDeleteRecoveryRequest;
        }

        /// <summary> Generates a token and sends an email to the supplied emailaddress with the link </summary>
        public KeyValuePair<bool, string> GenerateRecoveryLink(string emailAddress)
        {
            bool succeeded = false;
            KeyValuePair<bool, string> defaultResponse = new KeyValuePair<bool, string>(succeeded, "Unable to generate recovery link");      

            DataResult<User> drUser = database.GetUser(new User { EmailAddress = emailAddress });

            if (!drUser.Success) return defaultResponse;

            if (drUser.Data.Count > 1) return defaultResponse;

            User user = drUser.Data.FirstOrDefault();

            if (user == null) return defaultResponse;

            string token = Guid.NewGuid().ToString();

            RecoveryRequest rRequest = new RecoveryRequest()
            {
                UserId = user.Id,
                ExpiryDate = DateTime.Now.AddHours(1),
                Token = token
            };

            DataResult<RecoveryRequest> drRecoveryRequest = database.StoreRecoveryRequest(rRequest);

            string link = "{url}?token="+token;

            string message = string.Empty;
            message += "<a href=" + link + ">Click here</a> to recover your password";
            
            string subject = "Monoid password recovery";
            string recipient = user.EmailAddress;

            Mailer mailer = new Mailer();
            succeeded = mailer.SendEmail(message, subject, recipient);

            return new KeyValuePair<bool, string>(succeeded, link);
        }

        /// <summary> Generate a token for usage on the embedded system </summary>
        public string GenerateToken(User user)
        {
            DataResult<User> dr = database.GetUser(user);

            if (!dr.Success) return null;

            User nUser = dr.Data.FirstOrDefault();

            string token = Guid.NewGuid().ToString();

            nUser.Token = token;

            database.UpdateUser(nUser);

            return token;
        }
    }
}
