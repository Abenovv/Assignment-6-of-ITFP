using System.Text.Json.Nodes;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static readonly DateOnly Day = new(2026, 10, 13);

    private static int Main()
    {
        (string Name, Action Run)[] tests =
        [
            ("Normal workflow matches the sample plan", NormalWorkflow),
            ("An empty dataset gives an empty plan", EmptyDataset),
            ("The exact importance threshold is included", ImportanceBoundary),
            ("Zero minutes and false are valid values", ZeroAndFalse),
            ("Both policies produce the expected different orders", TwoPolicies),
            ("Selection excludes completed, other-day and low-importance tasks", SelectionStage),
            ("Scoring invokes the supplied delegate for every task", ScoringStage),
            ("Stages leave caller-owned values and collections unchanged", NoMutation),
            ("Results own their collections", DefensiveCopies),
            ("Repeating the workflow gives the same values", Determinism),
            ("Malformed JSON and field types are rejected", MalformedInput),
            ("Missing input and null records are rejected", MissingInput),
            ("Every required field is checked for missing and null", MissingFields),
            ("Invalid field values and duplicate IDs are rejected", InvalidValues),
            ("480 minutes is accepted and 481 is rejected", DurationBoundary),
            ("Equal scores use ordinal ID order", TieOrder),
            ("Invalid selection thresholds are rejected", InvalidThreshold),
            .. PlanningFactoryCases(),
            .. PlanningRulesCases(),
            .. PlanningStateCases(),
            .. PlanningWorkflowCases(),
            .. PlanningPresentationCases()
        ];

        int failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception error)
            {
                failures++;
                Console.WriteLine($"FAIL {test.Name}: {error.Message}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static IReadOnlyList<TaskItem> Sample()
    {
        Assert(TaskParser.TryParse(File.ReadAllText("samples/tasks.json"), out var tasks, out string error), error);
        return tasks;
    }

    private static string Record(string id = "T1", string title = "Test task", string date = "2026-10-13",
        int importance = 3, int minutes = 10) =>
        System.Text.Json.JsonSerializer.Serialize(new[] { new { id, title, date, importance, minutes, completed = false } });

    private static void Reject(string? json, string expectedMessage)
    {
        Assert(!TaskParser.TryParse(json, out var tasks, out string error), "Invalid input was accepted.");
        Assert(error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase), $"Unexpected error: {error}");
        Equal(0, tasks.Count);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Equal<T>(T expected, T actual) =>
        Assert(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}; got {actual}.");

    private static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual) =>
        Assert(expected.SequenceEqual(actual), $"Expected [{string.Join(", ", expected)}]; got [{string.Join(", ", actual)}].");

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
