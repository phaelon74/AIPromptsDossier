using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDungeonPrompts.Persistence.Configurations
{
	public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
	{
		public void Configure(EntityTypeBuilder<SystemSetting> builder)
		{
			builder.HasKey(e => e.Id);
			builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
			builder.Property(e => e.Value).IsRequired().HasMaxLength(1000);
			builder.Property(e => e.Description).HasMaxLength(500);

			builder.HasIndex(e => e.Key).IsUnique();
		}
	}
}
