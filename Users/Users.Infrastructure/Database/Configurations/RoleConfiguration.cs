using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain.Entities;

namespace Users.Infrastructure.Database.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(r => r.RoleId);
            builder.Property(r => r.RoleName)
                .IsRequired()
                .HasColumnType("varchar(10)");

            builder.Property(r => r.UserId)
                .IsRequired()
                .HasColumnName("FK_UserId");

            builder.HasOne(r => r.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(r => r.UserId)
                .HasConstraintName("FK_UserId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}