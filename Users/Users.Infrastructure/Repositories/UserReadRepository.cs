
using MongoDB.Driver;
using Users.Domain.Entities;
using Users.Core.Repositories;  
using Users.Infrastructure.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Users.Infrastructure.Repositories
{
    public class UserReadRepository : IUserReadRepository
    {
        private readonly IMongoCollection<MongoUserDocument> _usersCollection;
        private readonly IMongoCollection<MongoRoleDocument> _rolesCollection;

        public UserReadRepository(MongoDbContext dbContext)
        {
            _usersCollection = dbContext.User;
            _rolesCollection = dbContext.Role;
        }

        private async Task RoleForUserAsync(MongoUserDocument? userDocument)
        {
            if (userDocument == null) return;

            var rolesFilter = Builders<MongoRoleDocument>.Filter.Eq(r => r.UserId, userDocument.UserId);
            var userRoleDocument = await _rolesCollection.Find(rolesFilter).FirstOrDefaultAsync();

            userDocument.UserRole = userRoleDocument?.RoleName;
        }

        private async Task RolesForUsersAsync(List<MongoUserDocument> userDocuments)
        {
            if (userDocuments == null || !userDocuments.Any()) return;

            var userIds = userDocuments.Select(u => u.UserId).Distinct().ToList();
            if (!userIds.Any()) return;

            var rolesFilter = Builders<MongoRoleDocument>.Filter.In(r => r.UserId, userIds);
            var allRelevantRoleDocuments = await _rolesCollection.Find(rolesFilter).ToListAsync();

            var rolesByUserId = allRelevantRoleDocuments
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.RoleName); 

            foreach (var userDoc in userDocuments)
            {
                if (rolesByUserId.TryGetValue(userDoc.UserId, out var roleNameForUser))
                {
                    userDoc.UserRole = roleNameForUser;
                }
                else
                {
                    userDoc.UserRole = null;
                }
            }
        }

        public async Task<MongoUserDocument?> GetByIdAsync(Guid userId)
        {
            var userFilter = Builders<MongoUserDocument>.Filter.Eq(u => u.UserId, userId);
            var userDocument = await _usersCollection.Find(userFilter).FirstOrDefaultAsync();

            await RoleForUserAsync(userDocument);

            return userDocument;
        }

        public async Task<MongoUserDocument?> GetByEmailAsync(string userEmail)
        {
            var userFilter = Builders<MongoUserDocument>.Filter.Eq(u => u.UserEmail, userEmail);
            var userDocument = await _usersCollection.Find(userFilter).FirstOrDefaultAsync();

            await RoleForUserAsync(userDocument);

            return userDocument;
        }

        public async Task<IEnumerable<MongoUserDocument>> GetAllAsync()
        {
            var userDocuments = await _usersCollection.Find(Builders<MongoUserDocument>.Filter.Empty).ToListAsync();

            if (userDocuments.Any())
            {
                await RolesForUsersAsync(userDocuments); 
            }

            return userDocuments;
        }
    }
}