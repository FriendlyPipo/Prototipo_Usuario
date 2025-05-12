namespace Users.Infrastructure.Settings
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
    }
}