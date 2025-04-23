using Users.Domain.Entities;

namespace Users.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid userId); // Usuario por su id
        Task<IEnumerable<User>> GetAllAsync(); // Todos los usuarios
        Task UpdateAsync(User user); // Actualiza datos de usuario
        Task CreateAsync (User user); // Crea usuario
        Task DeleteAsync (Guid userId); // Borra usuario
    }

    
}