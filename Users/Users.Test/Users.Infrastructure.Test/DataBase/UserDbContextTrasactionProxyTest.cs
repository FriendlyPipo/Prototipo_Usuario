using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Users.Infrastructure.Database;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test.DataBase
{
    public class UserDbContextTrasactionProxyTest
    {
        private readonly Mock<DbContext> _mockContext;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly Mock<DatabaseFacade> _mockDatabase;

        public UserDbContextTrasactionProxyTest()
        {
            _mockContext = new Mock<DbContext>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockDatabase = new Mock<DatabaseFacade>(_mockContext.Object);

            _mockContext.Setup(c => c.Database).Returns(_mockDatabase.Object);
            _mockDatabase.Setup(d => d.BeginTransaction()).Returns(_mockTransaction.Object);
        }

        [Fact]
        public void Constructor_ShouldBeginTransaction()
        {
            // Act
            var proxy = new UserDbContextTransactionProxy(_mockContext.Object);

            // Assert
            _mockDatabase.Verify(d => d.BeginTransaction(), Times.Once);
        }

        [Fact]
        public void Commit_ShouldCommitTransaction()
        {
            // Arrange
            var proxy = new UserDbContextTransactionProxy(_mockContext.Object);

            // Act
            proxy.Commit();

            // Assert
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        [Fact]
        public void Rollback_ShouldRollbackTransaction()
        {
            // Arrange
            var proxy = new UserDbContextTransactionProxy(_mockContext.Object);

            // Act
            proxy.Rollback();

            // Assert
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldDisposeTransaction()
        {
            // Arrange
            var proxy = new UserDbContextTransactionProxy(_mockContext.Object);

            // Act
            proxy.Dispose();

            // Assert
            _mockTransaction.Verify(t => t.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldNotDisposeTransactionTwice()
        {
            // Arrange
            var proxy = new UserDbContextTransactionProxy(_mockContext.Object);

            // Act
            proxy.Dispose();
            proxy.Dispose();

            // Assert
            _mockTransaction.Verify(t => t.Dispose(), Times.Once);
        }
    }
}
