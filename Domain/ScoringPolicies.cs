namespace Tracker.Domain;

public static class ScoringPolicies
{
    public static int ImportanceFirst(TaskItem task) => task.Importance * 10;

    public static int ShortTasksFirst(TaskItem task) =>
        task.Importance * 10 + Math.Max(0, 60 - task.Minutes);
}
