using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Users.Domain.Entities; 
using Users.Infrastructure.Settings;

namespace Users.Infrastructure.Database 
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDBSettings> mongoDbSettings)
        {
            if (mongoDbSettings?.Value == null)
            {
                throw new ArgumentNullException(nameof(mongoDbSettings), "Configuración de MongoDB no proporcionada.");
            }
            var settings = mongoDbSettings.Value;   
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public virtual IMongoCollection<MongoUserDocument> User => _database.GetCollection<MongoUserDocument>("User");
        public virtual IMongoCollection<MongoRoleDocument> Role => _database.GetCollection<MongoRoleDocument>("Role");
    }
}