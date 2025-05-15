using System.Net.Mail;
using System.Net;
using Users.Core.Repositories;




namespace Users.Infrastructure.Repositories
{ /*
    public class EmailServiceRepository : IEmailServiceRepository
    {
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly IUserRepository _userRepository;

        public EmailServiceRepository(SmtpClient smtpClient, string fromEmail, string fromName, IUserRepository userRepository)
        {
            _smtpClient = smtpClient;
            _fromEmail = fromEmail;
            _fromName = fromName;
            _userRepository = userRepository;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await _smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationLink)
        {
            var subject = "Confirmación de correo electrónico";
            var body = $"<p>Por favor, confirma tu correo electrónico haciendo clic en el siguiente enlace:</p><p><a href='{confirmationLink}'>Confirmar correo electrónico</a></p>";
            await SendEmailAsync(to, subject, body);
        }
    
        public async Task SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "Restablecimiento de contraseña";
            var body = $"<p>Para restablecer tu contraseña, haz clic en el siguiente enlace:</p><p><a href='{resetLink}'>Restablecer contraseña</a></p>";
            await SendEmailAsync(to, subject, body);
        }
    } */
}