namespace Users.Core.Database
{
    public interface IUserDbContextTransactionProxy : IDisposable
    {
        void Commit();
        void Rollback();
    }
}