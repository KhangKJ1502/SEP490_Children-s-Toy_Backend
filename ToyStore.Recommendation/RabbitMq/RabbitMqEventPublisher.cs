using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ToyStore.Recommendation.RabbitMq;

public class RabbitMqEventPublisher : IRabbitMqEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void PublishEvents<T>(string queueName, IEnumerable<T> events)
    {
        try
        {
            var host = _configuration["RabbitMq:Host"] ?? "localhost";
            var port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672");
            var username = _configuration["RabbitMq:UserName"] ?? "guest";
            var password = _configuration["RabbitMq:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Khai báo Queue (Durable=true để lưu vào đĩa cứng, chống mất tin)
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true; // Tin nhắn được ghi vào đĩa

            foreach (var ev in events)
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ev));
                channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish events to RabbitMQ");
        }
    }
}
