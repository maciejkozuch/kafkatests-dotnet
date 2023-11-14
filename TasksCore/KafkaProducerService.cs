using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace TasksCore;

public class KafkaProducerService : IDisposable
{
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IProducer<string, string> _producer;
    
    public KafkaProducerService(ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig()
        {
            BootstrapServers = "localhost:9092",
            Acks = Acks.All,
            AllowAutoCreateTopics = false
        }).Build();
    }

    public async Task<bool> SendAsync<TValue>(string topicName, string key, TValue value)
    {
        try
        {
            var valueJson = JsonSerializer.Serialize(value, value.GetType());
            var result = await _producer.ProduceAsync(topicName, new Message<string, string>()
            {
                Key = key,
                Value = valueJson
            });
            return result.Status == PersistenceStatus.Persisted;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error when publishing the '{key}' message. '{message}'", key, e.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
        _logger.LogTrace("KafkaProducerService was disposed.");
    }
}