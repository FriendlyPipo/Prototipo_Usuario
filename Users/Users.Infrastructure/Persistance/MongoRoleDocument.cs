using MongoDB.Bson.Serialization.Attributes;

namespace Users.Infrastructure.Persistance
{
    public class MongoRoleDocument

    {
        [BsonId]
        public Guid RoleId { get; set; }

        [BsonElement("roleName")]
        public string RoleName { get; set; } = null!;

        [BsonElement("userId")]
        public Guid UserId { get; set; } 

    }
}