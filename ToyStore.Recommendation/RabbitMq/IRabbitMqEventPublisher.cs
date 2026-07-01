using System.Collections.Generic;

namespace ToyStore.Recommendation.RabbitMq;

public interface IRabbitMqEventPublisher
{
    void PublishEvents<T>(string queueName, IEnumerable<T> events);
}
