namespace Users.Core.Events
{
    public interface IEventBus
    {
        Task Publish<T> (T message, string queueName);
    }
}