using Users.Domain.Entities;

namespace Users.Core.Repositories
{
    public interface IUserWriteRepository
    {
        Task UpdateAsync(User user); // Actualiza datos de usuario
        Task CreateAsync(User user); // Crea usuario
        Task DeleteAsync(Guid userId); // Borra usuario
        Task<User?> GetByIdAsync(Guid userId);
    }
}