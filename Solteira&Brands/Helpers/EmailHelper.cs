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
        public static bool SendEmail(string toEmail, string subject, string body, bool isBodyHtml = false)
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
                        IsBodyHtml = isBodyHtml,
                        BodyEncoding = System.Text.Encoding.UTF8,
                        SubjectEncoding = System.Text.Encoding.UTF8
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

        public static string BuildOrderApprovedEmailHtml(string username, string orderNumber, string payNowUrl)
        {
            var safeName = string.IsNullOrWhiteSpace(username) ? "Customer" : username.Trim();
            var safeOrder = string.IsNullOrWhiteSpace(orderNumber) ? "—" : orderNumber.Trim();
            var safeUrl = string.IsNullOrWhiteSpace(payNowUrl) ? "#" : payNowUrl.Trim();

            var subjectTitle = "Your order has been approved";
            var logoUrl = "https://i.imgur.com/gHQJtBc.png"; // same logo as OTP template

            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(subjectTitle) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 16px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:26px; line-height:32px; color:#87ae73; font-weight:600;"">Your order has been approved</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeName) + @",</div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          Great news &mdash; your order <strong>#"
                            + System.Net.WebUtility.HtmlEncode(safeOrder) +
@"</strong> has been approved and is ready for payment.
                        </div>
                        <div style=""color:#444444;"">
                          Please click the button below to review your order and complete your payment in your Solteira &amp; Brands account.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <a href=""" + System.Net.WebUtility.HtmlEncode(safeUrl) + @""" target=""_blank""
                         style=""background-color:#87ae73; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:4px; font-family:Arial, Helvetica, sans-serif; font-size:14px; font-weight:600; display:inline-block;"">
                        Pay now
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:8px 32px 0 32px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:12px; color:#666666; line-height:18px;"">
                        If you have already completed payment, you can safely ignore this email.
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public static string BuildPaymentConfirmedEmailHtml(string username, string orderNumber, string myOrdersUrl)
        {
            var safeName = string.IsNullOrWhiteSpace(username) ? "Customer" : username.Trim();
            var safeOrder = string.IsNullOrWhiteSpace(orderNumber) ? "—" : orderNumber.Trim();
            var safeUrl = string.IsNullOrWhiteSpace(myOrdersUrl) ? "#" : myOrdersUrl.Trim();

            var subjectTitle = "Payment received for your order";
            var logoUrl = "https://i.imgur.com/gHQJtBc.png";

            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(subjectTitle) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 16px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:26px; line-height:32px; color:#87ae73; font-weight:600;"">Thank you for your payment</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeName) + @",</div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          Thank you for your payment! We are pleased to confirm that we have successfully received the payment for your order <strong>#"
                            + System.Net.WebUtility.HtmlEncode(safeOrder) +
@"</strong>.
                        </div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          We&rsquo;re happy to confirm that you&rsquo;ve completed the payment. You may view updates anytime on the My Orders page of our website to track your order.
                        </div>
                        <div style=""color:#444444;"">
                          Thank you for choosing Solteira &amp; Brands, where elegance meets exceptional craftsmanship.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <a href=""" + System.Net.WebUtility.HtmlEncode(safeUrl) + @""" target=""_blank""
                         style=""background-color:#87ae73; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:4px; font-family:Arial, Helvetica, sans-serif; font-size:14px; font-weight:600; display:inline-block;"">
                        View my orders
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:24px 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        Warm regards,<br />
                        <span style=""font-weight:600;"">Solteira &amp; Brands Team</span>
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public static string BuildPaymentRejectedEmailHtml(string username, string orderNumber, string myOrdersUrl, string rejectionReason)
        {
            var safeName = string.IsNullOrWhiteSpace(username) ? "Customer" : username.Trim();
            var safeOrder = string.IsNullOrWhiteSpace(orderNumber) ? "—" : orderNumber.Trim();
            var safeUrl = string.IsNullOrWhiteSpace(myOrdersUrl) ? "#" : myOrdersUrl.Trim();
            var safeReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();

            var subjectTitle = "Action needed: please resubmit your payment";
            var logoUrl = "https://i.imgur.com/gHQJtBc.png";

            var reasonBlock = safeReason == null
                ? ""
                : "<div style=\"color:#b02a37; margin-bottom:10px;\"><strong>Reason:</strong> " + System.Net.WebUtility.HtmlEncode(safeReason) + "</div>";

            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(subjectTitle) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 16px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:26px; line-height:32px; color:#87ae73; font-weight:600;"">We couldn&rsquo;t verify your payment</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeName) + @",</div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          We reviewed the payment you submitted for order <strong>#"
                            + System.Net.WebUtility.HtmlEncode(safeOrder) +
@"</strong>, but unfortunately we were not able to verify it.
                        </div>"
                        + reasonBlock +
@"                        <div style=""color:#444444; margin-bottom:10px;"">
                          Please submit your payment again using a clear and accurate proof of payment so we can continue processing your order.
                        </div>
                        <div style=""color:#444444;"">
                          You can retry your payment anytime from the My Orders page.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <a href=""" + System.Net.WebUtility.HtmlEncode(safeUrl) + @""" target=""_blank""
                         style=""background-color:#87ae73; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:4px; font-family:Arial, Helvetica, sans-serif; font-size:14px; font-weight:600; display:inline-block;"">
                        Retry payment
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:24px 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        Warm regards,<br />
                        <span style=""font-weight:600;"">Solteira &amp; Brands Team</span>
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public static string BuildPaymentDeadlineReminderFullEmailHtml(string username, string orderNumber, decimal amountDue, string deadlineDate, string myOrdersUrl)
        {
            var safeName = string.IsNullOrWhiteSpace(username) ? "Customer" : username.Trim();
            var safeOrder = string.IsNullOrWhiteSpace(orderNumber) ? "—" : orderNumber.Trim();
            var safeDeadline = string.IsNullOrWhiteSpace(deadlineDate) ? "your payment deadline" : deadlineDate.Trim();
            var safeUrl = string.IsNullOrWhiteSpace(myOrdersUrl) ? "#" : myOrdersUrl.Trim();
            var amountText = amountDue > 0 ? "&#8369; " + amountDue.ToString("0.##") : "your outstanding balance";

            var subjectTitle = "Payment due soon";
            var logoUrl = "https://i.imgur.com/gHQJtBc.png";

            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(subjectTitle) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 16px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:24px; line-height:30px; color:#87ae73; font-weight:600;"">Payment reminder</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeName) + @",</div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          This is a reminder that your payment for order <strong>#"
                            + System.Net.WebUtility.HtmlEncode(safeOrder) +
@"</strong> is due on <strong>" + System.Net.WebUtility.HtmlEncode(safeDeadline) + @"</strong>.
                        </div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          Please pay <strong>" + amountText + @"</strong> to keep your order reserved.
                        </div>
                        <div style=""color:#b02a37; font-weight:600;"">
                          If we do not receive your payment before the deadline, your order may be cancelled.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <a href=""" + System.Net.WebUtility.HtmlEncode(safeUrl) + @""" target=""_blank""
                         style=""background-color:#87ae73; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:4px; font-family:Arial, Helvetica, sans-serif; font-size:14px; font-weight:600; display:inline-block;"">
                        Pay now
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:0 32px 0 32px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:12px; color:#666666; line-height:18px; padding-top:4px;"">
                        You can also review the latest payment status from your My Orders page.
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:24px 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        Warm regards,<br />
                        <span style=""font-weight:600;"">Solteira &amp; Brands Team</span>
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public static string BuildPaymentDeadlineReminderLayawayEmailHtml(string username, string orderNumber, decimal amountDue, string deadlineDate, string myOrdersUrl)
        {
            var safeName = string.IsNullOrWhiteSpace(username) ? "Customer" : username.Trim();
            var safeOrder = string.IsNullOrWhiteSpace(orderNumber) ? "—" : orderNumber.Trim();
            var safeDeadline = string.IsNullOrWhiteSpace(deadlineDate) ? "your payment deadline" : deadlineDate.Trim();
            var safeUrl = string.IsNullOrWhiteSpace(myOrdersUrl) ? "#" : myOrdersUrl.Trim();
            var amountText = amountDue > 0 ? "&#8369; " + amountDue.ToString("0.##") : "your next layaway installment";

            var subjectTitle = "Layaway installment due soon";
            var logoUrl = "https://i.imgur.com/gHQJtBc.png";

            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(subjectTitle) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 16px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:24px; line-height:30px; color:#87ae73; font-weight:600;"">Layaway reminder</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeName) + @",</div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          This is a reminder that your next layaway installment for order <strong>#"
                            + System.Net.WebUtility.HtmlEncode(safeOrder) +
@"</strong> is due on <strong>" + System.Net.WebUtility.HtmlEncode(safeDeadline) + @"</strong>.
                        </div>
                        <div style=""color:#444444; margin-bottom:10px;"">
                          Please pay <strong>" + amountText + @"</strong> to keep your order active.
                        </div>
                        <div style=""color:#b02a37; font-weight:600; margin-bottom:10px;"">
                          If the installment is not settled by the deadline, your order will be cancelled.
                        </div>
                        <div style=""color:#444444;"">
                          You can submit your payment from your My Orders page.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <a href=""" + System.Net.WebUtility.HtmlEncode(safeUrl) + @""" target=""_blank""
                         style=""background-color:#87ae73; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:4px; font-family:Arial, Helvetica, sans-serif; font-size:14px; font-weight:600; display:inline-block;"">
                        Pay now
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:24px 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        Warm regards,<br />
                        <span style=""font-weight:600;"">Solteira &amp; Brands Team</span>
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public static string BuildOtpEmailHtml(string otp, string purposeTitle, string recipientLabel)
        {
            var safeOtp = (otp ?? "").Trim();
            var safePurpose = string.IsNullOrWhiteSpace(purposeTitle) ? "Your One-Time Password is here!" : purposeTitle.Trim();
            var safeRecipient = string.IsNullOrWhiteSpace(recipientLabel) ? "User" : recipientLabel.Trim();

            var logoUrl = "https://i.imgur.com/gHQJtBc.png"; // transparent PNG logo
            var lockIconUrl = "https://img.icons8.com/ios-filled/50/87ae73/lock.png"; // padlock icon

            // Email-client friendly HTML: table layout, inline styles, minimal CSS.
            return
@"<!doctype html>
<html>
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>" + System.Net.WebUtility.HtmlEncode(safePurpose) + @"</title>
  </head>
  <body style=""margin:0; padding:0; background-color:#87ae73;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#87ae73;"">
      <tr>
        <td align=""center"" style=""padding:24px 12px 24px 12px;"">
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px; max-width:600px;"">
            <tr>
              <td align=""center"" bgcolor=""#87ae73"" style=""background-color:#87ae73; padding:30px 0 30px 0;"">
                <img src=""" + logoUrl + @""" alt=""Solteira &amp; Brands"" style=""max-width:190px; height:auto; display:block; margin:0 auto; border:0;"" />
              </td>
            </tr>
            <tr>
              <td bgcolor=""#ffffff"" style=""background-color:#ffffff; padding:28px 32px 32px 32px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                  <tr>
                    <td align=""center"" style=""padding:0 0 12px 0;"">
                      <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:26px; line-height:32px; color:#87ae73; font-weight:600;"">"
                        + System.Net.WebUtility.HtmlEncode(safePurpose) +
@"</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:4px 0 20px 0;"">
                      <img src=""" + lockIconUrl + @""" alt=""Secure"" width=""40"" height=""40"" style=""display:block; margin:0 auto; border:0;"" />
                    </td>
                  </tr>
                  <tr>
                    <td align=""left"" style=""padding:0 16px 0 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333333; line-height:22px;"">
                        <div style=""margin-bottom:10px;"">Dear " + System.Net.WebUtility.HtmlEncode(safeRecipient) + @",</div>
                        <div style=""color:#444444;"">
                          For your security, we have generated a one-time password to verify your identity and complete your request.
                          This code is valid for a limited time and can only be used once. Please do not share this code with anyone to protect your account.
                        </div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:28px 16px 10px 16px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:22px; letter-spacing:4px; color:#111111; font-weight:800;"">"
                        + System.Net.WebUtility.HtmlEncode(safeOtp) +
@"</div>
                    </td>
                  </tr>
                  <tr>
                    <td align=""center"" style=""padding:8px 32px 0 32px;"">
                      <div style=""font-family:Arial, Helvetica, sans-serif; font-size:12px; color:#666666; line-height:18px;"">
                        If you did not request this code, you can safely ignore this email.
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:14px 10px 0 10px; font-family:Arial, Helvetica, sans-serif; font-size:11px; color:#f3f7f1;"">
                © " + DateTime.Now.Year + @" Solteira &amp; Brands
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }
    }
}
