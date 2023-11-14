using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace TasksCore;

public class KafkaManageService : IDisposable
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger<KafkaManageService> _logger;
    
    public KafkaManageService(ILogger<KafkaManageService> logger)
    {
        _logger = logger;
        _adminClient = new AdminClientBuilder(new AdminClientConfig()
        {
            BootstrapServers = "localhost:9092"
        }).Build();
    }
    
    public async Task CreateTopicIfDoesntExists(string topicName)
    {
        var topics = _adminClient.GetMetadata(TimeSpan.FromSeconds(60)).Topics;
        var topic = topics.Find(t => t.Topic == topicName);
        if (topic == null)
        {
            _logger.LogWarning("The topic '{topicName}' doesn't exists. Creating topic ...", topicName);
            await _adminClient.CreateTopicsAsync(new []
            {
                new TopicSpecification()
                {
                    Name = topicName,
                    NumPartitions = 1
                }
            });
            _logger.LogInformation("The topic '{topicName}' was created", topicName);
        }
        else
        {
            _logger.LogTrace("The topic '{topicName}' exists", topicName);
        }
    }

    public void Dispose()
    {
        _adminClient.Dispose();
        _logger.LogTrace("KafkaManageService was disposed.");
    }
}