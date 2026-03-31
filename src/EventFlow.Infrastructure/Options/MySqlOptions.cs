namespace EventFlow.Infrastructure.Options;

public sealed class MySqlOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = string.Empty;
}