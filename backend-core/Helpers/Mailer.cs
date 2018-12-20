using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace backend_core
{
    public class Mailer
    {
        private SmtpClient smtpClient;
        private const string SENDER_EMAIL = "no-reply@monoidinc.nl";
        private const string SENDER_DISPLAY_NAME = "Monoid Inc";

        public Mailer(SmtpClient _smtpClient = null)
        {
            smtpClient = _smtpClient != null ? _smtpClient : new SmtpClient("127.0.0.1", 25);
        }

        public bool SendEmail(string body, string subject, string[] recipient)
        {
            bool succeeded = false;

            MailMessage mm = new MailMessage();
            if(recipient.Length > 0)
            {
                foreach(string r in recipient)
                {
                    mm.To.Add(r);
                }
            }
            else
            {
                mm.To.Add(recipient[0]);
            }
            
            mm.Body = body;
            mm.Subject = subject;
            mm.BodyEncoding = Encoding.UTF8;
            mm.IsBodyHtml = true;
            mm.From = new MailAddress(SENDER_EMAIL, SENDER_DISPLAY_NAME);

            try
            {
                smtpClient.Send(mm);
                succeeded = true;
            }
            catch(SmtpException ex)
            {
                new Logger().CreateErrorLog(ex);
            }

            return succeeded;
        }

        public void SendSystemNotification(Settings settings, Risk type)
        {
            // If user does not wish to receive notifications stop
            if (!settings.EnabledNotifications) return;

            string body = string.Empty;
            string subject = "Monoid Notification: " + type.ToString() + "";

            SendEmail(body, subject, settings.NotificationRecipients);
        }
    }
}
