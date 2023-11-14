using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace TasksCore;

public class KafkaConsumerService : IDisposable
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;

    private bool _receiving = true;
    
    public KafkaConsumerService(ILogger<KafkaConsumerService> logger)
    {
        _logger = logger;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig()
        {
            BootstrapServers = "localhost:9092",
            GroupId = "tasks-consumer-group",
            ClientId = "tasks-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = false,
            EnableAutoCommit = false,
            AutoCommitIntervalMs = 0
        }).Build();
    }

    public async void StartReceiving<TValue>(string topicName, Action<string, TValue> action, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Starting receiving {type} messages ...", typeof(TValue));
        try
        {
            _consumer.Subscribe(topicName);
            while (_receiving)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result?.Message != null)
                    {
                        var key = result.Message.Key;
                        var valueJson = result.Message.Value;
                        var value = JsonSerializer.Deserialize<TValue>(valueJson);
                        action.Invoke(key, value);
                        _consumer.Commit(result);
                    }
                }
                catch (OperationCanceledException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error when trying consume a message. {message}", e.Message);
                    throw;
                }
            } 
        }
        catch (ObjectDisposedException e)
        {
            _logger.LogTrace("Stopping receiving {type} messages ...", typeof(TValue));
        }
    }

    public void StopReceiving()
    {
        _receiving = false;
        
    }
    
    public void Dispose()
    {
        _consumer?.Dispose();
        _logger.LogTrace("KafkaConsumerService was disposed.");
    }
}
