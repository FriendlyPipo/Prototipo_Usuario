using Xunit;
using Users.Domain.Entities;

namespace Users.Test.Domain
{
    public class MongoRoleDocumentTest
    {
        [Fact]
        public void MongoRoleDocument_Should_SetAndGetProperties_Correctly()
        {
            // Arrange
            var role = new MongoRoleDocument();
            var roleId = Guid.NewGuid();
            var roleName = "Administrador";
            var expectedUserId = Guid.NewGuid();

            // Act
            role.RoleId = roleId;
            role.RoleName = roleName;
            role.UserId = expectedUserId;

            // Assert
            Assert.Equal(roleId, role.RoleId);
            Assert.Equal(roleName, role.RoleName);
            Assert.Equal(expectedUserId, role.UserId);
        }
    }
}