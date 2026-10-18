using System.Text.Json.Nodes;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static void ImportanceBoundary()
    {
        var selected = TaskPipeline.SelectTasks(Sample(), Day, 3);
        Assert(selected.Any(task => task.Id == "T3"), "Importance 3 must be included.");
        Assert(selected.All(task => task.Id != "T4"), "Importance 2 must be excluded.");
        var plan = TaskPipeline.CreatePlan(Sample(), Day, 4, ScoringPolicies.ImportanceFirst);
        Sequence(new[] { "T1", "T2", "T7" }, plan.Tasks.Select(item => item.Task.Id));
        Equal(110, plan.TotalMinutes);
    }

    private static void SelectionStage() =>
        Sequence(new[] { "T1", "T2", "T3", "T7" }, TaskPipeline.SelectTasks(Sample(), Day, 3).Select(task => task.Id));

    private static void InvalidThreshold()
    {
        Throws<ArgumentOutOfRangeException>(() => TaskPipeline.SelectTasks(Sample(), Day, 0));
        Throws<ArgumentOutOfRangeException>(() => TaskPipeline.SelectTasks(Sample(), Day, 6));
    }
}
