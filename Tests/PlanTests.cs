using System.Text.Json.Nodes;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static void NormalWorkflow()
    {
        var plan = TaskPipeline.CreatePlan(Sample(), Day, 3, ScoringPolicies.ImportanceFirst);
        Sequence(new[] { "T1", "T2", "T7", "T3" }, plan.Tasks.Select(item => item.Task.Id));
        Sequence(new[] { 50, 40, 40, 30 }, plan.Tasks.Select(item => item.Score));
        Sequence(new[] { 1, 2, 3, 4 }, plan.Tasks.Select(item => item.Position));
        Equal(120, plan.TotalMinutes);
        Equal(Day, plan.Date);
    }

    private static void EmptyDataset()
    {
        Assert(TaskParser.TryParse("[]", out var tasks, out string error), error);
        foreach (Func<TaskItem, int> policy in new Func<TaskItem, int>[]
                 { ScoringPolicies.ImportanceFirst, ScoringPolicies.ShortTasksFirst })
        {
            var plan = TaskPipeline.CreatePlan(tasks, Day, 3, policy);
            Equal(0, plan.Tasks.Count);
            Equal(0, plan.TotalMinutes);
        }
    }

    private static void NoMutation()
    {
        var input = Sample().ToList();
        var original = input.ToArray();
        var selected = TaskPipeline.SelectTasks(input, Day, 3);
        var selectionBefore = selected.ToArray();
        var scored = TaskPipeline.ScoreTasks(selected, ScoringPolicies.ShortTasksFirst);
        var scoresBefore = scored.ToArray();
        TaskPipeline.BuildPlan(scored, Day);
        TaskPipeline.CreatePlan(input, Day, 3, ScoringPolicies.ImportanceFirst);
        Sequence(original, input);
        Sequence(selectionBefore, selected);
        Sequence(scoresBefore, scored);
    }

    private static void DefensiveCopies()
    {
        var input = Sample().ToList();
        var selected = TaskPipeline.SelectTasks(input, Day, 3);
        input.Clear();
        Equal(4, selected.Count);

        var scores = TaskPipeline.ScoreTasks(selected, ScoringPolicies.ImportanceFirst).ToList();
        var plan = TaskPipeline.BuildPlan(scores, Day);
        scores.Clear();
        Equal(4, plan.Tasks.Count);
        Equal(120, plan.TotalMinutes);

        var ranked = plan.Tasks.ToList();
        var copy = new DailyPlan(Day, ranked);
        ranked.Clear();
        Equal(4, copy.Tasks.Count);
        Throws<NotSupportedException>(() => ((IList<RankedTask>)copy.Tasks).Clear());
        Throws<NotSupportedException>(() => ((IList<TaskItem>)selected).Clear());
        Throws<NotSupportedException>(() => ((IList<TaskItem>)Sample()).Clear());
    }

    private static void Determinism()
    {
        var input = Sample();
        foreach (Func<TaskItem, int> policy in new Func<TaskItem, int>[]
                 { ScoringPolicies.ImportanceFirst, ScoringPolicies.ShortTasksFirst })
        {
            var first = TaskPipeline.CreatePlan(input, Day, 3, policy);
            var second = TaskPipeline.CreatePlan(input, Day, 3, policy);
            Sequence(first.Tasks, second.Tasks);
            Equal(first.Date, second.Date);
            Equal(first.TotalMinutes, second.TotalMinutes);
        }
    }

    private static void TieOrder()
    {
        var task = new TaskItem("B", "Test", Day, 3, 10, false);
        var plan = TaskPipeline.CreatePlan(new[] { task, task with { Id = "A" } }, Day, 3, ScoringPolicies.ImportanceFirst);
        Sequence(new[] { "A", "B" }, plan.Tasks.Select(item => item.Task.Id));
    }
}
