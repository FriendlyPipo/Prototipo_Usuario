using RabbitMQ.Client;
using System.Threading.Tasks;
using System.Threading;

public interface IRabbitMQChannelFactory
{
    Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
}