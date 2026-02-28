using MerxPos.Pos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Text;

namespace MerxPos.Pos.Infrastructure.Messaging;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnection _rabbit;

    public OutboxPublisher(IServiceScopeFactory scopeFactory, RabbitMqConnection rabbit)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        var channel = _rabbit.Channel;

        await channel.QueueDeclareAsync(
            queue: "transaction-events",
            durable: true,
            exclusive: false,
            autoDelete: false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();

                var messages = await dbContext.OutboxMessages
                    .Where(x => !x.Processed)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var body = Encoding.UTF8.GetBytes(message.Payload);

                        await channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: "transaction-events",
                            body: body,
                            cancellationToken: stoppingToken);

                        message.MarkAsProcessed();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Message publish failed: {ex.Message}");
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Outbox loop error: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}