namespace Tracker.Domain;

public static partial class TaskPipeline
{
    public static IReadOnlyList<TaskItem> SelectTasks(
        IReadOnlyList<TaskItem> tasks, DateOnly date, int minimumImportance)
    {
        if (minimumImportance < 1 || minimumImportance > 5)
            throw new ArgumentOutOfRangeException(nameof(minimumImportance), "Use importance 1 to 5.");

        return Array.AsReadOnly(tasks
            .Where(task => task.Date == date && !task.Completed && task.Importance >= minimumImportance)
            .ToArray());
    }
}
