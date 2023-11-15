namespace TasksCore;

public class ExampleTask
{
    public string? Id { get; set; }
    public TaskStatus? Status { get; set; }
    public string? Desc { get; set; }

    public static readonly string Topic = "kafka-tests.tasks";
    public static readonly string TopikTable = "kafka-tests.tasks-table";
    public static readonly string Table = "kafka-tests.table-tasks";
}

public enum TasksCore
{
    New = 0,
    InProgress = 1,
    Done = 2, 
    Paused = 3, 
    Canceled = 4
}