using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Users.Core.Events;

namespace Users.Infrastructure.EventBus
{
    public class RabbitMQPublisher<T> : IEventBus<T>
    {
        private readonly RabbitMQSetting _rabbitMQSetting;

        public RabbitMQPublisher(IOptions<RabbitMQSetting> rabbitMqSetting)
        {
            _rabbitMQSetting = rabbitMqSetting.Value;
        }

        public async Task Publish<T>(T message, string queueName)
        {
             var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSetting.HostName,
                UserName = _rabbitMQSetting.UserName,
                Password = _rabbitMQSetting.Password
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var messageJson = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(messageJson);
            
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body,
                mandatory: false,
                cancellationToken: System.Threading.CancellationToken.None);
        }
    }

}