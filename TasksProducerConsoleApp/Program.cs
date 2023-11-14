using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TasksCore;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
services.AddSingleton<KafkaManageService>();
services.AddSingleton<KafkaProducerService>();
IServiceProvider serviceProvider = services.BuildServiceProvider();
ILogger<Program> logger = serviceProvider.GetService<ILogger<Program>>();

using var kafkaManageService = serviceProvider.GetService<KafkaManageService>();
using var kafkaProducerService = serviceProvider.GetService<KafkaProducerService>();

logger?.LogInformation("Starting the tasks producer!");

await kafkaManageService?.CreateTopicIfDoesntExists(ExampleTask.Topic)!;

var messagesCount = 20;
var messages = new ExampleTask[messagesCount];

for(short i = 0; i < messagesCount; i++)
{
    messages[i] = new ExampleTask()
    {
        Id = Guid.NewGuid().ToString(),
        Desc = $"Message task no. {i}.",
        Status = TaskStatus.Created
    };
}

foreach(var message in messages)
{
    var result = await kafkaProducerService.SendAsync<ExampleTask>(ExampleTask.Topic, message.Id, message);
    if (result)
    {
        logger.LogInformation("The message with key '{key}' was created.", message.Id);
    }
}

for (short i = 2; i < messagesCount; i += 2)
{
    var message = messages[i];
    message.Status = TaskStatus.Canceled;
    var result = await kafkaProducerService.SendAsync<ExampleTask>(ExampleTask.Topic, message.Id, message);
    if (result)
    {
        logger.LogInformation("The message with key '{key}' was updated.", message.Id);
    }
}

logger?.LogInformation("Stopping the tasks producer!");