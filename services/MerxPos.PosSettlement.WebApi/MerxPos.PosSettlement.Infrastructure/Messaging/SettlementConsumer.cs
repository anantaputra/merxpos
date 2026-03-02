using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MerxPos.PosSettlement.Contracts.Events;
using MerxPos.PosSettlement.Application.Abstractions;

namespace MerxPos.PosSettlement.Infrastructure.Messaging;

public class SettlementConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public SettlementConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMqAsync();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var message = JsonSerializer.Deserialize<TransactionCreatedEvent>(json);

                if (message != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider
                        .GetRequiredService<ISettlementService>();

                    await service.HandleTransactionCreatedAsync(
                        message.TransactionId,
                        message.Amount);
                }

                // ✅ ACK only after success
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {
                // ❌ Requeue if error
                await _channel!.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: "settlement.queue",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: "pos.exchange",
            type: ExchangeType.Direct,
            durable: true);

        await _channel.QueueDeclareAsync(
            queue: "settlement.queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _channel.QueueBindAsync(
            queue: "settlement.queue",
            exchange: "pos.exchange",
            routingKey: "transaction.created");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();

        await base.StopAsync(cancellationToken);
    }
}