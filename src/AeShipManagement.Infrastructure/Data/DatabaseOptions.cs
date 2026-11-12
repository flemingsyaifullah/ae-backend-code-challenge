using System.ComponentModel.DataAnnotations;

namespace AeShipManagement.Infrastructure.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
