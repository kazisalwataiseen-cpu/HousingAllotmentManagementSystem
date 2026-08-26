using System.Net;
using System.Net.Mail;

namespace HousingAllotmentManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            string senderEmail =
                _configuration["EmailSettings:SenderEmail"]
                ?? throw new InvalidOperationException(
                    "Sender email is not configured.");

            string appPassword =
                _configuration["EmailSettings:AppPassword"]
                ?? throw new InvalidOperationException(
                    "Gmail App Password is not configured.");

            using var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,

                EnableSsl = true,

                UseDefaultCredentials = false,

                Credentials = new NetworkCredential(
                    senderEmail,
                    appPassword),

                DeliveryMethod =
                    SmtpDeliveryMethod.Network,

                Timeout = 30000
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(
                    senderEmail,
                    "Housing Allotment Management System"),

                Subject = subject,

                Body = body,

                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await smtp.SendMailAsync(mail);
        }
    }
}