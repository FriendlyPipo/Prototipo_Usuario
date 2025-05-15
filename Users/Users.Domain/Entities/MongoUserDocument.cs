using MongoDB.Bson.Serialization.Attributes;

namespace Users.Domain.Entities
{
    public class MongoUserDocument
    {
        [BsonId]
        public Guid UserId { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; }

        [BsonElement("userLastName")]
        public string UserLastName { get; set; }

        [BsonElement("userEmail")]
        public string UserEmail { get; set; }

        [BsonElement("userPhoneNumber")]
        public string UserPhoneNumber { get; set; }

        [BsonElement("userDirection")]
        public string UserDirection { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("updatedBy")]
        public string? UpdatedBy { get; set; }

        [BsonElement("userConfirmation")]
        public bool UserConfirmation { get; set; }

        [BsonElement("userPassword")]
        public string UserPassword { get; set; }
        public string? UserRole { get; set; }

    }
}