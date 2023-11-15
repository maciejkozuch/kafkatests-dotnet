using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;
using Streamiz.Kafka.Net.State;
using Streamiz.Kafka.Net.Table;
using TasksCore;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
services.AddSingleton<KafkaManageService>();
services.AddSingleton<KafkaConsumerService>();
IServiceProvider serviceProvider = services.BuildServiceProvider();
ILogger<Program> logger = serviceProvider.GetService<ILogger<Program>>();

using var kafkaManageService = serviceProvider.GetService<KafkaManageService>();
using var kafkaConsumerService = serviceProvider.GetService<KafkaConsumerService>();

logger?.LogInformation("Starting the tasks consumer!");

await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.Topic)!;
await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.TopikTable)!;

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
var stream = streamBuilder.Stream(ExampleTask.Topic, stringSerDes, stringSerDes);
var table = stream.ToTable(InMemory.As<string, string>(ExampleTask.Table));
table.ToStream().To(ExampleTask.TopikTable, stringSerDes, stringSerDes);

var topology = streamBuilder.Build();
var streams = new KafkaStream(topology, streamConfig);

await streams.StartAsync();

var store = streams.Store(StoreQueryParameters.FromNameAndType(ExampleTask.Table,
    QueryableStoreTypes.KeyValueStore<string, string>()));

Task.Run(() =>
{
    kafkaConsumerService.StartReceiving<ExampleTask>(ExampleTask.TopikTable, (key, value) =>
    {
        logger?.LogInformation("Received: '{desc}' with status '{status}'.", value.Desc, value.Status);
        var stateValueJson = store.Get(value.Id);
        var stateValue = JsonSerializer.Deserialize<ExampleTask>(stateValueJson);
        logger?.LogInformation("Actual status: {state}", stateValue.Status);
        
        
        Thread.Sleep(2000);
    }, cancellationTokenSrc.Token);
});

Console.ReadLine();
streams.Dispose();
cancellationTokenSrc.Cancel();

logger?.LogInformation("Stopping the tasks consumer!");
