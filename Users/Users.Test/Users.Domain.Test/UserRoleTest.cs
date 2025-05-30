using Xunit;
using Users.Domain.Entities;

namespace Users.Test.Domain
{
    public class UserRoleTest
    {

        [Fact]
        public void Constructor_WithRoleName_ShouldSetRoleNameAndGenerateRoleId()
        {
            //Arrange
            var roleName = UserRoleName.Administrador;
            var userId = Guid.NewGuid();

            //Act
            var userRole = new UserRole(roleName)
            {
                UserId = userId
            };

            //Assert
            Assert.NotEqual(Guid.Empty, userRole.RoleId);
            Assert.Equal(roleName, userRole.RoleName);
            Assert.Equal(userId, userRole.UserId);
        }
        
                [Fact]
        public void ParameterlessConstructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var userRole = new UserRole();

            // Assert
            Assert.Equal(Guid.Empty, userRole.RoleId);
            Assert.Equal(default(UserRoleName), userRole.RoleName); 
            Assert.Equal(Guid.Empty, userRole.UserId);
            Assert.Null(userRole.User);
        }
    }
}
