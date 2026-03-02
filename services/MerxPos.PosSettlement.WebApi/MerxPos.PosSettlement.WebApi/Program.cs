using MerxPos.PosSettlement.Application.Abstractions;
using MerxPos.PosSettlement.Application.Services;
using MerxPos.PosSettlement.Domain.Repositories;
using MerxPos.PosSettlement.Infrastructure.Messaging;
using MerxPos.PosSettlement.Infrastructure.Persistence;
using MerxPos.PosSettlement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SettlementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository
builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
builder.Services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

// Register Service
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();


builder.Services.AddHostedService<SettlementConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
