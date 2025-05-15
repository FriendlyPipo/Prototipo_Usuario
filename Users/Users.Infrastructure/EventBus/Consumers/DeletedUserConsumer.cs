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
    public class DeletedUserConsumer
    {
        private readonly IMongoCollection<MongoUserDocument> _userCollection;
        private readonly IMongoCollection<MongoRoleDocument> _roleCollection;

        public DeletedUserConsumer(IMongoClient client, IOptions<MongoDBSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _userCollection = database.GetCollection<MongoUserDocument>("User");
            _roleCollection = database.GetCollection<MongoRoleDocument>("Role");
        }

        public async Task Start(IConnection connection)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync("user.deleted", true, false, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    var @event = JsonSerializer.Deserialize<UserDeletedEvent>(json);
                    if (@event != null)
                    {
                        var filter = Builders<MongoUserDocument>.Filter.Eq(x => x.UserId, @event.UserId);
                        await _userCollection.DeleteOneAsync(filter);

                        var roleFilter = Builders<MongoRoleDocument>.Filter.Eq(x => x.UserId, @event.UserId);
                        await _roleCollection.DeleteManyAsync(roleFilter);
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
            channel.BasicConsumeAsync("user.deleted", false, consumer);
        }
    }
}
