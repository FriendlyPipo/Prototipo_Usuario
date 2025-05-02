using Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Users.Core.Database
{
    public interface IUserDbContext
    {
        DbContext DbContext { get; }

        DbSet<User> User { get; set; }

        DbSet<UserRole> Role { get; set; }

        IUserDbContextTransactionProxy BeginTransaction();

        void ChangeEntityState<TEntity>(TEntity entity, EntityState state);

        Task<bool> SaveEfContextChanges(string user, CancellationToken cancellationToken = default);
    }
}