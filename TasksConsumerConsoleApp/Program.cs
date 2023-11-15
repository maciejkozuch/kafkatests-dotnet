using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TasksCore;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
services.AddSingleton<KafkaManageService>();
services.AddSingleton<KafkaConsumerService>();
var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetService<ILogger<Program>>();

using var kafkaManageService = serviceProvider.GetService<KafkaManageService>();
using var kafkaConsumerService = serviceProvider.GetService<KafkaConsumerService>();

logger?.LogInformation("Starting the tasks consumer!");

await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.TopikTable)!;

var cancellationTokenSrc = new CancellationTokenSource();

Task.Run(() =>
{
    kafkaConsumerService.StartReceiving<ExampleTask>(ExampleTask.TopikTable, (key, value) =>
    {
        logger?.LogInformation("Received: '{desc}' with status '{status}'.", value.Desc, value.Status);
        Thread.Sleep(2000);
    }, cancellationTokenSrc.Token);
});

Console.ReadLine();
cancellationTokenSrc.Cancel();

logger?.LogInformation("Stopping the tasks consumer!");
