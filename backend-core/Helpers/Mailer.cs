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
        private const string EMAIL_TEMPLATE = "<html><head><meta http-equiv='Content-Type' content='text/html; charset=UTF-8' /><meta name='viewport' content='width=device-width, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0' /><link href='https://fonts.googleapis.com/css?family=Open+Sans' rel='stylesheet'><title>Monoid</title><style>body{font-family:'Open Sans',sans-serif}td[class='text']{color:#000;font-size:13px}html,body{margin:0;padding:0}table{border-collapse:collapse}tr[class='border-color']{border-color:#F1C232!important}tr[class='banner']{background-color:#FFF;height:50px;width:100%}td[class='titlebanner']{color:#000;font-size:24px;font-weight:700;vertical-align:middle}td[class='banner-logo']{vertical-align:middle}td[class='button']{height:50px}td[class='button'] a{background-color:#F1C232;text-decoration:none;font-size:14px;color:#000;padding:10px}td[class='riskbanner']{background-color:*|RISKCOLOR|*;width:50px;position:relative}td[class='riskbanner']:after{content:'';position:absolute;left:0;bottom:0;width:0;height:0;border-bottom:13px solid #FFF;border-left:25px solid transparent;border-right:25px solid transparent}td[class='main-spacing']{padding-left:30px;padding-right:30px;padding-top:20px;padding-bottom:10px}td[class='content-spacing']{padding-left:30px;padding-right:30px;padding-top:10px;padding-bottom:20px}</style></head><body bgcolor='#f5f5f7'><table width='600' cellspacing='0' cellpadding='0' border='0' bgcolor='#ffffff' align='center'><tbody><tr class='border-color' style='border-top: 4px solid;'><td class='main-spacing'><table><tbody><tr class='banner'><td class='banner-logo'><img src='https://i.imgur.com/PcbgYeE.png' width='100' /></td><td width='27'></td><td class='titlebanner' width='393'>*|TITLE|*</td><td class='riskbanner' width='50'></td><td width='20'></td></tr></tbody></table></td></tr><tr><td class='content-spacing'><table width='540' align='left'><tbody><tr><td class='text'>Dear *|RECIPIENT|*,<br /><br /></td></tr><tr><td class='text'>*|BODY|*</td></tr><tr><td width='200'></td><td class='button'><a href='https://dashboard.monoidinc.nl'>View in dashboard</a></td><td></td></tr><tr><td class='text'><br />Best regards,<br />Monoid Inc<br /><br /></td></tr></tbody></table></td></tr><tr class='border-color' style='border-bottom: 4px solid;'><td></td></tr></tbody></table></body></html>";

        private const string INFORMATION_RISK_COLOR = "#CCCCCC";
        private const string LOW_RISK_COLOR = "#9FC5E8";
        private const string MEDIUM_RISK_COLOR = "#FFE599";
        private const string HIGH_RISK_COLOR = "#E06666";
        private const string CRITICAL_RISK_COLOR = "#674EA7";

        public Mailer(SmtpClient _smtpClient = null)
        {
            smtpClient = _smtpClient != null ? _smtpClient : new SmtpClient("127.0.0.1", 25);
        }

        public bool SendEmail(string body, string subject, string title, string[] recipient, string riskcolor = INFORMATION_RISK_COLOR)
        {
            bool succeeded = false;

            foreach (string r in recipient)
            {
                MailMessage mm = new MailMessage();

                string emailBody = EMAIL_TEMPLATE;
                Dictionary<string, object> emailVars = new Dictionary<string, object>();
                emailVars.Add("RECIPIENT", r);
                emailVars.Add("TITLE", title);
                emailVars.Add("BODY", body);
                emailVars.Add("RISKCOLOR", riskcolor);

                mm.Body = ResolveEmailVars(emailBody, emailVars);
                mm.Subject = subject;
                mm.BodyEncoding = Encoding.UTF8;
                mm.IsBodyHtml = true;
                mm.From = new MailAddress(SENDER_EMAIL, SENDER_DISPLAY_NAME);
                mm.To.Add(r);
                
                try
                {
                    smtpClient.Send(mm);
                    succeeded = true;
                }
                catch (SmtpException ex)
                {
                    new Logger().CreateErrorLog(ex);
                }
            }

            return succeeded;
        }

        public void SendSystemNotification(Settings settings, string message, Risk type, string title)
        {
            // If user does not wish to receive notifications stop
            if (!settings.EnabledNotifications) return;

            string subject = "Monoid: " + title;

            string riskColor = INFORMATION_RISK_COLOR;

            if (type == Risk.Low)
            {
                riskColor = LOW_RISK_COLOR;
            }
            else if(type == Risk.Medium)
            {
                riskColor = MEDIUM_RISK_COLOR;
            }
            else if(type == Risk.High)
            {
                riskColor = HIGH_RISK_COLOR;
            }
            else if(type == Risk.Critical)
            {
                riskColor = CRITICAL_RISK_COLOR;
            }

            SendEmail(message, subject, title, settings.NotificationRecipients, riskColor);
        }

        public string ResolveEmailVars(string body, Dictionary<string, object> values)
        {
            if (string.IsNullOrWhiteSpace(body)) return body;

            if (body.Contains("*|") && body.Contains("|*"))
            {
                // Do resolver stuff
                foreach(KeyValuePair<string,object> kv in values)
                {
                    var key = "*|" + kv.Key + "|*";

                    if(body.Contains(key))
                    {
                        body = body.Replace(key, Convert.ToString(kv.Value));
                    }
                }
            }
            else
            {
                return body;
            }

            return body;
        }
    }
}
