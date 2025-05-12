namespace Users.Core.Repositories
{
    public interface IEmailServiceRepository
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendConfirmationEmailAsync(string to, string confirmationLink);
        Task SendPasswordResetEmailAsync(string to, string resetLink);
    }
}