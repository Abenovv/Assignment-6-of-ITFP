using System.Text.Json.Nodes;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static void TwoPolicies()
    {
        var tasks = Sample();
        var importance = TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ImportanceFirst);
        var shortTasks = TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ShortTasksFirst);
        Sequence(new[] { "T3", "T7", "T2", "T1" }, shortTasks.Tasks.Select(item => item.Task.Id));
        Sequence(new[] { 80, 80, 70, 50 }, shortTasks.Tasks.Select(item => item.Score));
        Equal(120, shortTasks.TotalMinutes);
        Equal("T1", importance.Tasks[0].Task.Id);
        Equal("T3", shortTasks.Tasks[0].Task.Id);
    }

    private static void ScoringStage()
    {
        var selected = TaskPipeline.SelectTasks(Sample(), Day, 3);
        int calls = 0;
        // The counter is test instrumentation, not a production policy.
        var scored = TaskPipeline.ScoreTasks(selected, task => { calls++; return task.Importance + 7; });
        Equal(4, calls);
        Sequence(new[] { 12, 11, 10, 11 }, scored.Select(item => item.Score));
        Sequence(selected, scored.Select(item => item.Task));
    }
}
