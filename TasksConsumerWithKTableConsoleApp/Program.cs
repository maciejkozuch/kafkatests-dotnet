using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.Errors;
using Streamiz.Kafka.Net.SerDes;
using Streamiz.Kafka.Net.State;
using Streamiz.Kafka.Net.Table;
using TasksCore;
using TaskStatus = TasksCore.TaskStatus;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
services.AddSingleton<KafkaManageService>();
services.AddSingleton<KafkaConsumerService>();
var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetService<ILogger<Program>>();

using var kafkaManageService = serviceProvider.GetService<KafkaManageService>();
using var kafkaConsumerService = serviceProvider.GetService<KafkaConsumerService>();

logger?.LogInformation("Starting the tasks consumer with KTable stream!");

await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.TopicIn)!;
await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.TopicOut)!;

var cancellationTokenSrc = new CancellationTokenSource();

var stringSerDes = new StringSerDes();
var streamConfig = new StreamConfig<StringSerDes, StringSerDes>
{
    ApplicationId = "ktable-tasks",
    BootstrapServers = "localhost:9092",
    DefaultKeySerDes = stringSerDes,
    DefaultValueSerDes = stringSerDes,
    Acks = Acks.All
};

var streamBuilder = new StreamBuilder();
var stream = streamBuilder.Stream(ExampleTask.TopicIn, stringSerDes, stringSerDes);
/*
stream.Filter((key, value) =>
{
    var task = JsonSerializer.Deserialize<ExampleTask>(value);
    return task?.Status == TaskStatus.New;
}).To(ExampleTask.TopicOut, stringSerDes, stringSerDes);
*/
var table = stream.ToTable(InMemory.As<string, string>(ExampleTask.Table));
table.ToStream().To(ExampleTask.TopicOut, stringSerDes, stringSerDes);

var topology = streamBuilder.Build();
var streams = new KafkaStream(topology, streamConfig);

await streams.StartAsync();

IReadOnlyKeyValueStore<string, string>? store = default;
while (store == default)
{
    try
    {
        store = streams.Store(StoreQueryParameters.FromNameAndType(ExampleTask.Table,
            QueryableStoreTypes.KeyValueStore<string, string>()));
    }
    catch (InvalidStateStoreException e)
    {
        logger?.LogWarning(e.Message);
        Thread.Sleep(TimeSpan.FromSeconds(1));
    }
}

Task.Run(() =>
{
    kafkaConsumerService.StartReceiving<ExampleTask>(ExampleTask.TopicOut, (key, value) =>
    {
        logger?.LogTrace("Received: '{desc}' with status '{status}'.", value.Desc, value.Status);
        
        var storeValueJson = store.Get(value.Id);
        var storeValue = JsonSerializer.Deserialize<ExampleTask>(storeValueJson);
        if (storeValue?.Status == TaskStatus.New)
        {
            logger?.LogInformation("To execute: '{desc}' with status '{status}'.", value.Desc, storeValue.Status);
        }
        else
        {
            logger?.LogWarning("Rejected: '{desc}' with status '{status}'.", value.Desc, storeValue?.Status);
        }
        Thread.Sleep(2000);
    }, cancellationTokenSrc.Token);
});

Console.ReadLine();
streams.Dispose();
cancellationTokenSrc.Cancel();

logger?.LogInformation("Stopping the tasks consumer with KTable stream!");
