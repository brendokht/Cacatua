using server.Interfaces;
using System.Net;
using MimeKit;
using MailKit.Net.Smtp;
using server.Models;
using FirebaseAdmin.Auth;

namespace server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IFirestoreService _firestoreService;

        public EmailService(IConfiguration configuration, IFirestoreService firestoreService)
        {
            _configuration = configuration;
            _firestoreService = firestoreService;
        }

        private MimeMessage CreateMessage(string subject, string toEmail, string displayName, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_configuration["EmailConfig:TestCacatuaEmail"]));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            return message;
        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            using (var smtpClient = new SmtpClient())
            {
                // Connect to the SMTP server
                await smtpClient.ConnectAsync(_configuration["EmailConfig:SmtpClient:Host"], 587, MailKit.Security.SecureSocketOptions.StartTls);

                // Authenticate with the SMTP server
                await smtpClient.AuthenticateAsync(
                    _configuration["EmailConfig:SmtpClient:Credentials:UserName"],
                    _configuration["EmailConfig:SmtpClient:Credentials:Password"]);

                // Send the email
                await smtpClient.SendAsync(message);

                // Disconnect from the server
                await smtpClient.DisconnectAsync(true);
            }
        }

        public async Task SendVerificationEmailAsync(string email, string displayName, string verificationLink)
        {
            try
            {
                // If newEmail is not null, use it to update to a new email
                string recipientEmail = email;
                string htmlBody = @$"<p>Hey {displayName},<br>
                    <p>Click the link below to verify your email address:<br>
                    <a href='{verificationLink}'>{verificationLink}</a><br>
                    <p>-- Cacatua<br>";

                var message = CreateMessage("Verify your email", recipientEmail, displayName, htmlBody);

                await SendEmailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending verification email: {ex.Message}");
                throw;
            }
        }

        public async Task SendAccountDeletionEmailAsync(string email, string displayName)
        {
            try
            {
                string htmlBody = @$"<p>Hello {displayName},<br>
                    <p>Your Cacatua account has been deleted.<br>
                    <p>-- Cacatua<br>";

                var message = CreateMessage("Sorry to see you go!", email, displayName, htmlBody);

                await SendEmailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending account deletion email: {ex.Message}");
                throw;
            }
        }
    }
}
