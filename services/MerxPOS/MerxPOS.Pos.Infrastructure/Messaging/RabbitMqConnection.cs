using RabbitMQ.Client;

namespace MerxPOS.Pos.Infrastructure.Messaging;

public class RabbitMqConnection : IAsyncDisposable
{
    private readonly IConnection _connection;
    public IChannel Channel { get; }

    public RabbitMqConnection()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        Channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        Channel.QueueDeclareAsync(
            queue: "transaction-events",
            durable: true,
            exclusive: false,
            autoDelete: false
        ).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.CloseAsync();
        await _connection.CloseAsync();
    }
}