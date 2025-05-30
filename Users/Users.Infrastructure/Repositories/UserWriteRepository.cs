using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Core.Repositories;
using Users.Infrastructure.Database;

namespace Users.Infrastructure.Repositories
{
    public class UserWriteRepository : IUserWriteRepository
    {
        
        private readonly UserDbContext _dbContext;

        public UserWriteRepository(UserDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAsync(User user)
        {
            _dbContext.User.Add(user);
        }

        public async Task DeleteAsync(Guid userId)
        {
            var user = await _dbContext.User.FindAsync(userId);
            if (user != null)
            {
                _dbContext.User.Remove(user);
            }
        }

        public async Task UpdateAsync(User user)
        {
            _dbContext.User.Update(user);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _dbContext.User
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}