using Users.Core.DTO;

namespace Users.Core.Repositories
{
    public interface IKeycloakRepository
    {
        Task<string> GetTokenAsync();
        Task<string> CreateUserAsync(KcCreateUserDTO user, string token);   
        Task AssignRoleToUserAsync(string keycloakUserId, string role, string token);

        /* Para los otros casos de uso
         Task<string> DeleteUserAsync(Guid userId);
         Task UpdateUser(Guid id, UpdateUserDTO userDTO);
         Task<string> LoginAsync(string username, string password);
         Task<string> LogOutAsync();
         */
    }
}