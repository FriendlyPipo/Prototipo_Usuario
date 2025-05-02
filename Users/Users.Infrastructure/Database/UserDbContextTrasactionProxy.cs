using Users.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Users.Infrastructure.Database
{
    public class UserDbContextTransactionProxy : IUserDbContextTransactionProxy
    {

        private readonly IDbContextTransaction _transaction;

        private bool _disposed;

        public UserDbContextTransactionProxy(DbContext context)
        {
            _transaction = context.Database.BeginTransaction();
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction.Dispose();
                }

                _disposed = true;
            }
        }
    }
}