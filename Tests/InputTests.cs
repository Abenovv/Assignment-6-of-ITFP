using System.Text.Json.Nodes;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static void ZeroAndFalse()
    {
        Assert(TaskParser.TryParse(Record(minutes: 0), out var tasks, out string error), error);
        Equal(false, tasks[0].Completed);
        Equal(0, tasks[0].Minutes);
        var plan = TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ShortTasksFirst);
        Equal(1, plan.Tasks.Count);
        Equal(90, plan.Tasks[0].Score);
        Equal(0, plan.TotalMinutes);
    }

    private static void MalformedInput()
    {
        Reject("[", "Invalid JSON");
        Reject("{}", "Invalid JSON");
        Reject(Record().Replace("\"importance\":3", "\"importance\":\"high\""), "importance");
        Reject(Record().Replace("\"minutes\":10", "\"minutes\":1.5"), "minutes");
        Reject(Record().Replace("\"completed\":false", "\"completed\":\"false\""), "completed");
        Reject(Record().Replace("\"title\"", "\"titel\""), "titel");
    }

    private static void MissingInput()
    {
        Reject(null, "required");
        Reject("", "required");
        Reject("  ", "required");
        Reject("null", "cannot be null");
        Reject("[null]", "task object");
    }

    private static void MissingFields()
    {
        foreach (string field in new[] { "id", "title", "date", "importance", "minutes", "completed" })
        {
            var array = JsonNode.Parse(Record())?.AsArray() ?? throw new Exception("Bad test fixture.");
            var row = array[0]?.AsObject() ?? throw new Exception("Bad test fixture.");
            row.Remove(field);
            Reject(array.ToJsonString(), field);
            row[field] = null;
            Reject(array.ToJsonString(), field);
        }
    }

    private static void InvalidValues()
    {
        Reject(Record(id: " "), "id");
        Reject(Record(title: " "), "title");
        Reject(Record(date: "2026-02-30"), "date");
        Reject(Record(date: "13/10/2026"), "date");
        Reject(Record(importance: 0), "importance");
        Reject(Record(importance: 6), "importance");
        Reject(Record(minutes: -1), "minutes");
        string row = Record()[1..^1];
        Reject($"[{row},{row}]", "unique");
    }

    private static void DurationBoundary()
    {
        Assert(TaskParser.TryParse(Record(minutes: 480), out var tasks, out string error), error);
        var plan = TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ShortTasksFirst);
        Equal(480, plan.TotalMinutes);
        Equal(30, plan.Tasks[0].Score);
        Reject(Record(minutes: 481), "minutes");
    }
}
