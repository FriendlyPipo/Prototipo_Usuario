using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Domain.Interfaces;
using Users.Infrastructure.Data;

namespace Users.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly UserDbContext _dbcontext;

        public RoleRepository(UserDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task<UserRole> GetRoleByNameAsync(string roleName)
        {
            return await _dbcontext.Role.FirstOrDefaultAsync(r => r.RoleName.ToString() == roleName);
        }

        public async Task<IEnumerable<UserRole>> GetAllRolesAsync()
        {
            return await _dbcontext.Role.ToListAsync();
        }
    }
}