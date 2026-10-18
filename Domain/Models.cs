namespace Tracker.Domain;

public sealed record TaskItem(
    string Id, string Title, DateOnly Date, int Importance, int Minutes, bool Completed);

public sealed record ScoredTask(TaskItem Task, int Score);

public sealed record RankedTask(int Position, TaskItem Task, int Score);

public sealed record DailyPlan
{
    public DateOnly Date { get; }
    public IReadOnlyList<RankedTask> Tasks { get; }
    public int TotalMinutes { get; }

    public DailyPlan(DateOnly date, IEnumerable<RankedTask> tasks)
    {
        Date = date;
        // Copy first: a read-only view of a caller's list would still share its storage.
        Tasks = Array.AsReadOnly(tasks.ToArray());
        TotalMinutes = Tasks.Sum(item => item.Task.Minutes);
    }
}
