
using Users.Domain.Entities;

namespace Users.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<UserRole> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<UserRole>> GetAllRolesAsync();
    }
}