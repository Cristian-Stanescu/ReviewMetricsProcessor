using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor;
using ReviewMetricsProcessor.Consumers;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Endpoints;
using ReviewMetricsProcessor.Endpoints.Reviews;
using ReviewMetricsProcessor.Validators;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation
builder.Services.AddScoped<IValidator<ReviewEvent>, ReviewEventValidator>();

// Configure MassTransit with In-Memory transport
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<ReviewStartedConsumer>();
    x.AddConsumer<ReviewCompletedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        // Configure in-memory transport for scalable internal event processing
        cfg.ConfigureEndpoints(context);

        // Configure retry policy for resilience
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(10)));

        // Configure concurrency for better throughput
        cfg.ConcurrentMessageLimit = 100;

        // Configure concurrent consumer limit for better throughput
        cfg.UseConcurrencyLimit(10);
    });
});

// Configure JSON options to handle camelCase and DateTime conversion
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
});

builder.Services.AddDbContext<ReviewMetricsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("ReviewMetricsProcessor.Migrations"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseReviewMetricsEndpoints();

app.Run();