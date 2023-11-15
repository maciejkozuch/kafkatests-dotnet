namespace TasksCore;

public class ExampleTask
{
    public string? Id { get; set; }
    public TaskStatus? Status { get; set; }
    public string? Desc { get; set; }

    public static readonly string TopicIn = "kafka-tests.tasks-in";
    public static readonly string TopicOut = "kafka-tests.tasks-out";
    public static readonly string Table = "kafka-tests.tasks-store";
}

public enum TaskStatus
{
    New = 0,
    InProgress = 1,
    Done = 2, 
    Paused = 3, 
    Canceled = 4
}