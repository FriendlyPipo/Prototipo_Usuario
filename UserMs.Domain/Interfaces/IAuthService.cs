using System Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UserMs.Domain.Entities;
using UserMs.Infra.Data;

namespace UserMs.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterUserAsync(string UserCorreo, string UserPassword, string UserNombre);
        Task<User> LoginUserAsync(string email, string password);
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}