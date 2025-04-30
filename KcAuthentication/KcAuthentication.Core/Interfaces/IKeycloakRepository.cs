using KcAuthentication.Common.Dtos;

namespace KcAuthentication.Core.Interfaces
{
    public interface IKeycloakRepository
    {
        Task<string> GetTokenAsync();
        Task<string> CreateUserAsync(CreateUserDto user,string token);   
        Task AssignRoleToUserAsync(string email,string token);

        /* Para los otros casos de uso
         Task<string> DeleteUserAsync(Guid userId);
         Task UpdateUser(Guid id, UpdateUserDTO userDTO);
         Task<string> LoginAsync(string username, string password);
         Task<string> LogOutAsync();
         */
    }
}