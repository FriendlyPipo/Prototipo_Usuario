using EntityFramework.Exceptions.PostgreSQL;   
using Microsoft.EntityFrameworkCore; 
using Users.Domain.Entities;

namespace Users.Infrastructure.Data
{
    public class UserDbContext : DbContext
    {
         public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options){}
        public DbSet<User> Users { get;set; }
        public DbSet<UserRole> Role { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserLastName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserEmail).IsRequired().HasMaxLength(50);
                entity.Property(u => u.UserPhoneNumber).IsRequired().HasMaxLength(25);
                entity.Property(u => u.UserDirection).IsRequired().HasMaxLength(200);
                entity.Property(u => u.createdAt).IsRequired();
                entity.Property(u => u.UserRoleId);

                 // Configuración de la relación con UserRole
                entity.HasOne(u => u.UserRole) 
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.UserRoleId)
                .IsRequired(false) // La clave foránea no es obligatoria
                .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleName).IsRequired();
            });
        }
    }
}