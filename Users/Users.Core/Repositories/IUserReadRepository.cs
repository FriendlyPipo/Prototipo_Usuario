using Users.Domain.Entities;

namespace Users.Core.Repositories
{
    public interface IUserReadRepository
    {
        Task<MongoUserDocument?> GetByIdAsync(Guid userId); // Usuario por su id

        Task<MongoUserDocument?> GetByEmailAsync(string userEmail); // Usuario por su Correo
        Task<IEnumerable<MongoUserDocument>> GetAllAsync(); // Todos los usuarios
    }
}