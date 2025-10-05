using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDungeonPrompts.Persistence.Configurations
{
	public class AccountLockoutConfiguration : IEntityTypeConfiguration<AccountLockout>
	{
		public void Configure(EntityTypeBuilder<AccountLockout> builder)
		{
			builder.HasKey(e => e.Id);
			
			builder.Property(e => e.UserId).IsRequired();
			builder.Property(e => e.LockoutStart).IsRequired();
			builder.Property(e => e.FailedAttempts).IsRequired();
			builder.Property(e => e.IsActive).IsRequired();

			builder.HasOne(e => e.User)
				.WithMany(u => u.AccountLockouts)
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasIndex(e => e.UserId);
			builder.HasIndex(e => new { e.UserId, e.IsActive });
		}
	}
}
