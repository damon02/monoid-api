using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace backend_core
{
    public class UserCore : BaseCore
    {
        private static UserCore instance = null;
        private static readonly object padlock = new object();

        public static UserCore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new UserCore();
                    }
                    return instance;
                }
            }
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

            DataResult<User> drUserUpdated = database.UpdateUser(user);

            if (!drUserUpdated.Success) return drDefaultResponse;

            DataResult<RecoveryRequest> drDeleteRecoveryRequest = database.DeleteRecoveryRequest(recoveryRequest);

            return drDeleteRecoveryRequest;
        }

        /// <summary> Generates a token and sends an email to the supplied emailaddress with the link </summary>
        public KeyValuePair<bool, string> GenerateRecoveryLink(string emailAddress)
        {
            bool succeeded = true;
            KeyValuePair<bool, string> defaultResponse = new KeyValuePair<bool, string>(succeeded, "If your email address exists an email has been sent containing a link to recover your password");

            DataResult<User> drUser = database.GetUser(new User { EmailAddress = emailAddress });

            if (!drUser.Success) return defaultResponse;

            if (drUser.Data.Count > 1) return defaultResponse;

            User user = drUser.Data.FirstOrDefault();

            if (user == null) return defaultResponse;

            string recoveryToken = new Validator().RandomTemporaryString(12);

            RecoveryRequest rRequest = new RecoveryRequest()
            {
                UserId = user.Id,
                ExpiryDate = DateTime.Now.AddHours(1),
                Token = recoveryToken
            };

            DataResult<RecoveryRequest> drRecoveryRequest = database.StoreRecoveryRequest(rRequest);

            string link = DASHBOARD_URL+"recovery/" + recoveryToken;

            string body = string.Empty;
            body += "Hi, \n\n";
            body += "<a href=" + link + ">Click here</a> to recover your password \n\n";
            body += "Monoid Inc.";

            string subject = "Monoid Dashboard: Password recovery";
            string recipient = user.EmailAddress;

            Mailer mailer = new Mailer();
            succeeded = mailer.SendEmail(body, subject, recipient);

            return defaultResponse;
        }

        /// <summary> Get user settings </summary>
        public Settings GetSettings(ObjectId userId)
        {
            DataResult<Settings> dr = database.GetSettings(userId);

            if (!dr.Success) return null;

            return dr.Data.FirstOrDefault();
        }

        /// <summary> Store user settings </summary>
        public bool SaveSettings(Settings settings)
        {
            DataResult<Settings> dr = database.StoreSettings(settings);

            return dr.Success;
        }
    }
}
