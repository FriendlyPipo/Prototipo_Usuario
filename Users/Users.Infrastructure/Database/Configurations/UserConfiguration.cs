using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain.Entities;

namespace Users.Infrastructure.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
            builder.Property(u => u.UserLastName).IsRequired().HasMaxLength(50);
            builder.Property(u => u.UserEmail).IsRequired().HasMaxLength(50);
            builder.Property(u => u.UserPhoneNumber).IsRequired().HasMaxLength(25);
            builder.Property(u => u.UserDirection).IsRequired().HasMaxLength(200);
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.UserConfirmation).IsRequired();

            builder.HasMany(u => u.UserRoles) // Un Usuario puede tener muchos Roles
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}