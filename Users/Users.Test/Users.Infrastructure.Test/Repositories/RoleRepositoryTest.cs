using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Infrastructure.Database;
using Users.Infrastructure.Repositories;
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Users.Test.Users.Infrastructure.Test.Repositories
{
    public class RoleRepositoryTest
    {
        private readonly DbContextOptions<UserDbContext> _options;
        private readonly UserDbContext _context;
        private readonly RoleRepository _roleRepository;

        public RoleRepositoryTest()
        {
            _options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "TestRoleDb")
                .Options;

            _context = new UserDbContext(_options);
            _roleRepository = new RoleRepository(_context);

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task GetRoleByNameAsync_ExistingRole_ReturnsRole()
        {
            // Arrange
            var expectedRole = new UserRole(UserRoleName.Administrador);
            await _context.Role.AddAsync(expectedRole);
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleRepository.GetRoleByNameAsync(UserRoleName.Administrador.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedRole.RoleId, result.RoleId);
            Assert.Equal(expectedRole.RoleName, result.RoleName);
        }

        [Fact]
        public async Task GetRoleByNameAsync_NonExistingRole_ReturnsNull()
        {
            // Arrange
            var existingRole = new UserRole(UserRoleName.Administrador);
            await _context.Role.AddAsync(existingRole);
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleRepository.GetRoleByNameAsync("NonExistingRole");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllRolesAsync_WithExistingRoles_ReturnsAllRoles()
        {
            // Arrange
            var roles = new List<UserRole>
            {
                new UserRole(UserRoleName.Administrador),
                new UserRole(UserRoleName.Postor),
                new UserRole(UserRoleName.Subastador)
            };

            await _context.Role.AddRangeAsync(roles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleRepository.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            Assert.Equal(roles.Select(r => r.RoleName), result.Select(r => r.RoleName));
        }

        [Fact]
        public async Task GetAllRolesAsync_WithNoRoles_ReturnsEmptyList()
        {
            // Act
            var result = await _roleRepository.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRoleByNameAsync_WithNullRoleName_ReturnsNull()
        {
            // Act
            var result = await _roleRepository.GetRoleByNameAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRoleByNameAsync_WithEmptyRoleName_ReturnsNull()
        {
            // Act
            var result = await _roleRepository.GetRoleByNameAsync(string.Empty);

            // Assert
            Assert.Null(result);
        }
    }
}
