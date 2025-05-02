
using Users.Domain.Entities;

namespace Users.Core.Repositories
{
    public interface IRoleRepository
    {
        Task<UserRole> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<UserRole>> GetAllRolesAsync();
    }
}