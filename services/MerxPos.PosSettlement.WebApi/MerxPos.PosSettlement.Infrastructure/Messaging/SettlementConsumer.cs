using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MerxPos.PosSettlement.Contracts.Events;
using MerxPos.PosSettlement.Application.Abstractions;

namespace MerxPos.PosSettlement.Infrastructure.Messaging;

public class SettlementConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    private const string MainExchange = "pos.exchange";
    private const string MainQueue = "settlement.queue";
    private const string MainRoutingKey = "transaction.created";

    private const string RetryExchange = "settlement.retry.exchange";
    private const string RetryQueue = "settlement.retry.queue";
    private const string RetryRoutingKey = "settlement.retry";

    private const string DlxExchange = "settlement.dlx.exchange";
    private const string DlxQueue = "settlement.dlq";
    private const string DlxRoutingKey = "settlement.failed";

    private const int MaxRetry = 3;
    private const int RetryDelayMilliseconds = 15000;

    public SettlementConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<SettlementConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeRabbitMq();

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (sender, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<TransactionCreatedEvent>(json);

                if (message == null)
                    throw new Exception("Deserialization failed");

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<ISettlementService>();

                await service.HandleTransactionCreatedAsync(
                    message.TransactionId,
                    message.Amount);

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Settlement processing failed");

                HandleRetryOrDlq(ea);

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
        };

        _channel!.BasicConsume(
            queue: MainQueue,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private void HandleRetryOrDlq(BasicDeliverEventArgs ea)
    {
        var retryCount = GetRetryCount(ea);

        if (retryCount >= MaxRetry)
        {
            _logger.LogWarning("Moved to DLQ after {RetryCount} retries", retryCount);

            _channel!.BasicPublish(
                exchange: DlxExchange,
                routingKey: DlxRoutingKey,
                basicProperties: ea.BasicProperties,
                body: ea.Body);
        }
        else
        {
            _logger.LogWarning("Retry attempt {RetryCount}", retryCount + 1);

            var props = _channel!.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>
            {
                { "x-retry-count", retryCount + 1 }
            };
            props.Persistent = true;

            _channel.BasicPublish(
                exchange: RetryExchange,
                routingKey: RetryRoutingKey,
                basicProperties: props,
                body: ea.Body);
        }
    }

    private static int GetRetryCount(BasicDeliverEventArgs ea)
    {
        if (ea.BasicProperties.Headers != null &&
            ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return Convert.ToInt32(value);
        }

        return 0;
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 🔥 QoS WAJIB setelah channel dibuat
        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false);

        // Main Exchange
        _channel.ExchangeDeclare(MainExchange, ExchangeType.Direct, true);

        // Main Queue
        _channel.QueueDeclare(
            MainQueue,
            true,
            false,
            false,
            new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", DlxRoutingKey }
            });
        _channel.QueueBind(MainQueue, MainExchange, MainRoutingKey);

        // Retry Exchange
        _channel.ExchangeDeclare(RetryExchange, ExchangeType.Direct, true);

        // Retry Queue
        _channel.QueueDeclare(
            RetryQueue,
            true,
            false,
            false,
            new Dictionary<string, object>
            {
                { "x-message-ttl", RetryDelayMilliseconds },
                { "x-dead-letter-exchange", MainExchange },
                { "x-dead-letter-routing-key", MainRoutingKey }
            });

        _channel.QueueBind(RetryQueue, RetryExchange, RetryRoutingKey);

        // DLX
        _channel.ExchangeDeclare(DlxExchange, ExchangeType.Direct, true);

        _channel.QueueDeclare(DlxQueue, true, false, false);
        _channel.QueueBind(DlxQueue, DlxExchange, DlxRoutingKey);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        return base.StopAsync(cancellationToken);
    }
}