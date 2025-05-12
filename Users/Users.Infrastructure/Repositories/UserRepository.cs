using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Core.Repositories;
using Users.Infrastructure.Database;

namespace Users.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _dbContext;

        public UserRepository(UserDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _dbContext.User
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetByEmailAsync(string userEmail)
        {
            return await _dbContext.User.FirstOrDefaultAsync(u => u.UserEmail == userEmail);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.User.Include(u => u.UserRoles).ToListAsync();
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
    }
}