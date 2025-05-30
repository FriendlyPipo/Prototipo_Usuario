using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Users.Core.Events;

namespace Users.Infrastructure.EventBus
{
    public class RabbitMQChannelFactory : IRabbitMQChannelFactory, IAsyncDisposable
    {
        private readonly RabbitMQSetting _rabbitMQSetting;
        private IConnection _connection;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public RabbitMQChannelFactory(IOptions<RabbitMQSetting> rabbitMqSettingOptions)
        {
            _rabbitMQSetting = rabbitMqSettingOptions?.Value ?? throw new ArgumentNullException(nameof(rabbitMqSettingOptions.Value));
        }

        public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
        {
            await EnsureConnectionAsync(cancellationToken);
            return await _connection.CreateChannelAsync(options: null, cancellationToken: cancellationToken);
        }
    
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }

            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (_connection != null && _connection.IsOpen)
                {
                    return;
                }

                if (_connection != null)
                {
                    try 
                    { 
                        await _connection.CloseAsync(TimeSpan.FromSeconds(5)); 
                    } 
                    catch (Exception ex) 
                    {
                        throw;
                    }
                    _connection.Dispose();
                    _connection = null;
                }
                
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMQSetting.HostName,
                    UserName = _rabbitMQSetting.UserName,
                    Password = _rabbitMQSetting.Password
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);

            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                    {
                        try { await _connection.CloseAsync(TimeSpan.FromSeconds(10)); } catch { }
                    }
                    _connection.Dispose();
                    _connection = null;
                }
            }
            finally
            {
                _connectionLock.Release();
                _connectionLock.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}