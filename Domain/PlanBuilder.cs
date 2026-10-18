namespace Tracker.Domain;

public static partial class TaskPipeline
{
    public static DailyPlan BuildPlan(IReadOnlyList<ScoredTask> tasks, DateOnly date)
    {
        var ranked = tasks
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Task.Id, StringComparer.Ordinal)
            .Select((item, index) => new RankedTask(index + 1, item.Task, item.Score));

        return new DailyPlan(date, ranked);
    }
}
