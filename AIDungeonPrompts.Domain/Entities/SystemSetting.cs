using AIDungeonPrompts.Domain.Entities.Abstract;

namespace AIDungeonPrompts.Domain.Entities
{
	public class SystemSetting : BaseDomainEntity
	{
		public string Key { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string? Description { get; set; }
	}
}
