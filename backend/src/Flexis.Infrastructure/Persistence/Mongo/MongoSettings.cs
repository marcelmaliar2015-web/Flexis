namespace Flexis.Infrastructure.Persistence.Mongo;

public sealed class MongoSettings
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;
}
