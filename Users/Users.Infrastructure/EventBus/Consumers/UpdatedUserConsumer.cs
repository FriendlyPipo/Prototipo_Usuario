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
    public class UpdatedUserConsumer
    {
        private readonly IMongoCollection<MongoUserDocument> _userCollection;
        private readonly IMongoCollection<MongoRoleDocument> _roleCollection;

        public UpdatedUserConsumer(IMongoClient client, IOptions<MongoDBSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _userCollection = database.GetCollection<MongoUserDocument>("User");
            _roleCollection = database.GetCollection<MongoRoleDocument>("Role");
        }

        public async Task Start(IConnection connection)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync("user.updated", true, false, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    var @event = JsonSerializer.Deserialize<UserUpdatedEvent>(json);
                    if (@event != null)
                    {
                        var filter = Builders<MongoUserDocument>.Filter.Eq(x => x.UserId, @event.UserId);
                        var update = Builders<MongoUserDocument>.Update
                            .Set(x => x.UserName, @event.UserName)
                            .Set(x => x.UserLastName, @event.UserLastName)
                            .Set(x => x.UserEmail, @event.UserEmail)
                            .Set(x => x.UserPhoneNumber, @event.UserPhoneNumber)
                            .Set(x => x.UserDirection, @event.UserDirection)
                            .Set(x => x.UpdatedAt, @event.UpdatedAt)
                            .Set(x => x.UpdatedBy, @event.UpdatedBy);

                        await _userCollection.UpdateOneAsync(filter, update);

                        var roleFilter = Builders<MongoRoleDocument>.Filter.Eq(x => x.RoleId, @event.RoleId);
                        var roleUpdate = Builders<MongoRoleDocument>.Update
                            .Set(x => x.RoleName, @event.RoleName);

                        await _roleCollection.UpdateOneAsync(roleFilter, roleUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
                finally
                {
                    channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };
            channel.BasicConsumeAsync("user.updated", false, consumer);
        }
    }
}