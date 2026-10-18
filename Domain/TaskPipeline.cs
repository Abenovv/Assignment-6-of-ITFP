namespace Tracker.Domain;

public static partial class TaskPipeline
{
    public static DailyPlan CreatePlan(
        IReadOnlyList<TaskItem> tasks, DateOnly date, int minimumImportance,
        Func<TaskItem, int> scorePolicy)
    {
        var selected = SelectTasks(tasks, date, minimumImportance);
        var scored = ScoreTasks(selected, scorePolicy);
        return BuildPlan(scored, date);
    }
}
