using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Users.Core.Events;
using Microsoft.Extensions.Logging;

namespace Users.Infrastructure.EventBus
{
    public class RabbitMQPublisher : IEventBus
    {
        private readonly IRabbitMQChannelFactory _channelFactory;
        private readonly ILogger<RabbitMQPublisher> _logger; 

        public RabbitMQPublisher(IRabbitMQChannelFactory channelFactory, ILogger<RabbitMQPublisher> logger)
        {
            _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
        }

        public async Task Publish<T>(T message, string queueName, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrEmpty(queueName)) throw new ArgumentNullException(nameof(queueName));
            
            try
            {
                using var channel = await _channelFactory.CreateChannelAsync(cancellationToken);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                var messageJson = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(messageJson);
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName, 
                    mandatory: false,
                    basicProperties: new BasicProperties(),
                    body: body,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al publicar mensaje en RabbitMQ para la cola {QueueName}", queueName);
                throw;
            }
        }

        async Task IEventBus.Publish<T>(T message, string queueName)
        {
            await Publish(message, queueName, CancellationToken.None);
        }
    }
}