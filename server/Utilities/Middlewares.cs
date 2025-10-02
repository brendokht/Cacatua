using System.Net.Mail;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using System.Security.Principal;
using Auth0.ManagementApi.Models.Keys;

namespace server.Utilities
{
    public class Middlewares
    {
        private readonly IConfiguration _configuration;

        // Constructor with dependency injection for RequestDelegate and IConfiguration
        public Middlewares(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //Email logic doesn't belong in Middleware -Garen
        //// Method to send verification email
        //public async Task SendVerificationEmailAsync(string email, string verificationLink)
        //{
        //    try
        //    {
        //        var smtpClient = new SmtpClient("smtp.gmail.com")
        //        {
        //            Port = 587,
        //            Credentials = new NetworkCredential("test.cacatua@gmail.com", "njmtxtvpitwsoicy"),
        //            EnableSsl = true,
        //        };

        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress("test.cacatua@gmail.com"),
        //            Subject = "Verify your email",
        //            Body = $"Click the link below to verify your email address:\n{verificationLink}",
        //            IsBodyHtml = false,
        //        };

        //        mailMessage.To.Add(email);
        //        await smtpClient.SendMailAsync(mailMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error sending verification email: {ex.Message}");
        //        throw;
        //    }
        //}

        // Method to generate JWT token
        public string GenerateJwtToken(string uid)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:JWT_SECRET"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var secToken = new JwtSecurityToken(
                signingCredentials: credentials,
                issuer: "Cacatua.com",
                audience: "cacatua-9d6e6",
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, uid)
                },
            expires: DateTime.UtcNow.AddHours(int.Parse(_configuration["JWT:VALID_HOURS"])));

            return tokenHandler.WriteToken(secToken);
        }

        public bool ValidateJwtToken(string authToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();

                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }
        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateLifetime = false,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidIssuer = "Cacatua.com",
                ValidAudience = "cacatua-9d6e6",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:JWT_SECRET"]))
            };
        }
    }
}
