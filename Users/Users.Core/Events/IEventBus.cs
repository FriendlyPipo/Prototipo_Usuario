namespace Users.Core.Events
{
    public interface IEventBus<T>
    {
        Task Publish<T>(T message, string queueName);
    }
}