using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Users.Infrastructure.Database;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();

        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Users;Username=postgres;Password=1234");

        return new UserDbContext(optionsBuilder.Options);
    }
}