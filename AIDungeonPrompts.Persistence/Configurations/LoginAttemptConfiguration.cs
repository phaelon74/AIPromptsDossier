using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDungeonPrompts.Persistence.Configurations
{
	public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
	{
		public void Configure(EntityTypeBuilder<LoginAttempt> builder)
		{
			builder.HasKey(e => e.Id);
			
			builder.Property(e => e.UserId).IsRequired();
			builder.Property(e => e.Success).IsRequired();
			builder.Property(e => e.AttemptDate).IsRequired();
			builder.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 max length

			builder.HasOne(e => e.User)
				.WithMany(u => u.LoginAttempts)
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasIndex(e => e.UserId);
			builder.HasIndex(e => e.AttemptDate);
		}
	}
}
