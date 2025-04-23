using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Users.Domain.Entities;
using Users.Infrastructure.Data;


namespace Users.Infrastructure.Interfaces
{
    public interface IAuthService
    {
        Task<string> CreateUserAsync(string userEmail, string userPassword);
        Task<Guid> GetUserByEmailAsync(string userEmail);
        Task UpdateUser(Guid userId,User user);
        Task<string> DeleteUserAsync(Guid userId);
        Task GiveRolUser(Guid userId, string rol);

    }
}