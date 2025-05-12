using Users.Domain.Entities;

namespace Users.Core.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid userId); // Usuario por su id

        Task<User?> GetByEmailAsync(string userEmail); // Usuario por su Correo
        Task<IEnumerable<User>> GetAllAsync(); // Todos los usuarios
        Task UpdateAsync(User user); // Actualiza datos de usuario
        Task CreateAsync (User user); // Crea usuario
        Task DeleteAsync (Guid userId); // Borra usuario
    }
}