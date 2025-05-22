using Users.Infrastructure.EventBus.Events;
using Users.Infrastructure.Settings;
using Users.Domain.Entities;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Users.Infrastructure.EventBus.Consumers
{
    public class CreateUserConsumer
    {
        private readonly IMongoCollection<MongoUserDocument> _userCollection;
        private readonly IMongoCollection<MongoRoleDocument> _roleCollection;

        public CreateUserConsumer(IMongoClient client, IOptions<MongoDBSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _userCollection = database.GetCollection<MongoUserDocument>("User");
            _roleCollection = database.GetCollection<MongoRoleDocument>("Role");
        }

        public async Task Start(IConnection connection)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync("user.created", true, false, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    var @event = JsonSerializer.Deserialize<UserCreatedEvent>(json);
                    if (@event != null)
                    {
                        var userDoc = new MongoUserDocument
                        {
                            UserId = @event.UserId,
                            UserName = @event.UserName,
                            UserLastName = @event.UserLastName,
                            UserEmail = @event.UserEmail,
                            UserPhoneNumber = @event.UserPhoneNumber,
                            UserDirection = @event.UserDirection,
                            CreatedAt = @event.CreatedAt,
                            CreatedBy = @event.CreatedBy,
                            UpdatedAt = @event.UpdatedAt,
                            UpdatedBy = @event.UpdatedBy
                        };

                        var roleDoc = new MongoRoleDocument
                        {
                            RoleId = @event.RoleId,
                            RoleName = @event.RoleName,
                            UserId = @event.UserId
                        };

                        await _userCollection.InsertOneAsync(userDoc);
                        await _roleCollection.InsertOneAsync(roleDoc);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Error processing message: {ex.Message}");
                }
            };
            await channel.BasicConsumeAsync("user.created", true, consumer);
        }
    }   
}