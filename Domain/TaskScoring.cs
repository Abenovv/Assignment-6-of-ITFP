namespace Tracker.Domain;

public static partial class TaskPipeline
{
    public static IReadOnlyList<ScoredTask> ScoreTasks(
        IReadOnlyList<TaskItem> tasks, Func<TaskItem, int> scorePolicy) =>
        Array.AsReadOnly(tasks.Select(task => new ScoredTask(task, scorePolicy(task))).ToArray());
}
