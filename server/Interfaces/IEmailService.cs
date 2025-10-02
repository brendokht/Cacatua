using server.Models;

namespace server.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string displayName, string verificationLink);
        Task SendAccountDeletionEmailAsync(string email, string firstName);
    }
}
