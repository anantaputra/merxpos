using MerxPos.PosSettlement.Application.Abstractions;
using MerxPos.PosSettlement.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

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

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var correlationId = ea.BasicProperties.CorrelationId ?? "N/A";
            var retryCount = GetRetryCount(ea);

            try
            {
                _logger.LogInformation(
                    "Processing | CorrelationId={CorrelationId} | RetryCount={RetryCount}",
                    correlationId,
                    retryCount);

                await ProcessMessage(message);

                _channel!.BasicAck(ea.DeliveryTag, false);

                _logger.LogInformation(
                    "SUCCESS | CorrelationId={CorrelationId}",
                    correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FAILED | CorrelationId={CorrelationId} | RetryCount={RetryCount}",
                    correlationId,
                    retryCount);

                HandleRetryOrDlq(ea, retryCount);

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
        };

        _channel!.BasicConsume(
            queue: MainQueue,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private void HandleRetryOrDlq(BasicDeliverEventArgs ea, int retryCount)
    {
        var correlationId = ea.BasicProperties.CorrelationId ?? "N/A";

        if (retryCount >= MaxRetry)
        {
            _logger.LogError(
                "SENT TO DLQ | CorrelationId={CorrelationId} | FinalRetry={RetryCount}",
                correlationId,
                retryCount);

            _channel!.BasicPublish(
                exchange: DlxExchange,
                routingKey: DlxRoutingKey,
                basicProperties: ea.BasicProperties,
                body: ea.Body);
        }
        else
        {
            _logger.LogWarning(
                "RETRYING | CorrelationId={CorrelationId} | NextRetry={NextRetry}",
                correlationId,
                retryCount + 1);

            var props = _channel!.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>
            {
                { "x-retry-count", retryCount + 1 }
            };
            props.Persistent = true;
            props.CorrelationId = correlationId;

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
            if (value is byte[] bytes)
                return int.Parse(Encoding.UTF8.GetString(bytes));

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
            Password = "guest",
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.BasicQos(0, 10, false);

        // DLX
        _channel.ExchangeDeclare(DlxExchange, ExchangeType.Direct, true);
        _channel.QueueDeclare(DlxQueue, true, false, false);
        _channel.QueueBind(DlxQueue, DlxExchange, DlxRoutingKey);

        // Main Exchange + Queue
        _channel.ExchangeDeclare(MainExchange, ExchangeType.Direct, true);

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

        // Retry Exchange + Queue
        _channel.ExchangeDeclare(RetryExchange, ExchangeType.Direct, true);

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
    }

    private async Task ProcessMessage(string message)
    {
        using var scope = _scopeFactory.CreateScope();

        var idempotencyService = scope.ServiceProvider
            .GetRequiredService<IIdempotencyService>();

        var settlementService = scope.ServiceProvider
            .GetRequiredService<ISettlementService>();

        var transaction = JsonSerializer.Deserialize<TransactionCreatedEvent>(message);

        var messageId = transaction!.TransactionId.ToString();

        if (await idempotencyService.HasProcessedAsync(messageId))
        {
            _logger.LogWarning(
                "DUPLICATE MESSAGE | TransactionId={TransactionId}",
                messageId);

            return;
        }

        await settlementService.HandleTransactionCreatedAsync(transaction.TransactionId, transaction.Amount);

        await idempotencyService.MarkAsProcessedAsync(messageId);

        _logger.LogInformation(
            "PROCESSED & MARKED | TransactionId={TransactionId}",
            messageId);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }
}