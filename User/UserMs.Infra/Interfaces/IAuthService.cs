using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UserMs.Domain.Entities;
using UserMs.Infra.Data;


namespace UserMs.Infra.Interfaces
{
    public interface IAuthService
    {
        Task<string> CreateUserAsync(string userCorreo, string userPassword);
        Task<Guid> GetUserByEmailAsync(string userCorreo);
        Task UpdateUser(Guid userId,User user);
        Task<string> DeleteUserAsync(Guid userId);
        Task GiveRolUser(Guid userId, string rol);

    }
}