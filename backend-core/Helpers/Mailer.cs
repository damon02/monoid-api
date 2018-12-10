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
        private const string SENDER_EMAIL = "mail@monoid.nl";
        private const string SENDER_DISPLAY_NAME = "monoid";

        public Mailer(SmtpClient _smtpClient = null)
        {
            smtpClient = _smtpClient != null ? _smtpClient : new SmtpClient("localhost", 25);
        }

        public bool SendEmail(string message, string subject, string recipient)
        {
            bool succeeded = false;

            MailMessage mm = new MailMessage();
            mm.To.Add(recipient);
            mm.Body = message;
            mm.Subject = subject;
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
    }
}
