using Microsoft.EntityFrameworkCore;
using UserMs.Domain.Entities;


namespace UserMs.Infra.Data 
{
    public class UserDbContext : DbContext
    {
         public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get;set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.UserNombre).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserApellido).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserCorreo).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserTelefono).IsRequired().HasMaxLength(25);
                entity.Property(u => u.UserDireccion).IsRequired().HasMaxLength(200);
                entity.Property(u => u.createdAt);
            }); 
        } 
    }
}