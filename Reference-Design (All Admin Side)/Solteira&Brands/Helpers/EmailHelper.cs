using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace Solteira_Brands.Helpers
{
    /// <summary>
    /// Shared SMTP email sender using Web.config appSettings (SmtpHost, SmtpPort, SmtpUser, SmtpPassword, FromEmail, FromDisplayName).
    /// </summary>
    public static class EmailHelper
    {
        public static bool SendEmail(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return false;
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var portStr = ConfigurationManager.AppSettings["SmtpPort"];
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var password = ConfigurationManager.AppSettings["SmtpPassword"];
            var fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            var fromName = ConfigurationManager.AppSettings["FromDisplayName"];
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                return false;
            if (!int.TryParse(portStr, out int port))
                port = 587;
            var useSsl = string.Equals(ConfigurationManager.AppSettings["SmtpEnableSsl"], "true", StringComparison.OrdinalIgnoreCase);
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = useSsl;
                    client.Credentials = new NetworkCredential(user, password);
                    var fromAddr = string.IsNullOrEmpty(fromEmail) ? user : fromEmail;
                    var msg = new MailMessage
                    {
                        From = new MailAddress(fromAddr, string.IsNullOrEmpty(fromName) ? null : fromName),
                        Subject = subject ?? "",
                        Body = body ?? "",
                        IsBodyHtml = false
                    };
                    msg.To.Add(toEmail.Trim());
                    client.Send(msg);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EmailHelper.SendEmail failed: " + ex.Message);
                return false;
            }
        }
    }
}
