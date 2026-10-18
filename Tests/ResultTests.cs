using Tracker.Domain;
using Tracker.Input;

namespace Tracker.Tests;

internal static partial class Program
{
    private static void ResultSuccessWorkflow()
    {
        var result = Result<string>.Ok(File.ReadAllText("samples/tasks.json"))
            .Bind(TaskParser.Parse)
            .Map(tasks => TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ImportanceFirst))
            .Map(plan => plan.TotalMinutes);
        Assert(result is Result<int>.Success { Value: 120 }, "Valid input did not produce 120 minutes.");
    }

    private static void ParseFailureSkipsPlanning()
    {
        int plans = 0, scores = 0;
        var result = Result<string>.Ok(File.ReadAllText("samples/invalid.json"))
            .Bind(TaskParser.Parse)
            .Map(tasks =>
            {
                plans++;
                return TaskPipeline.CreatePlan(tasks, Day, 3, task =>
                {
                    scores++;
                    return ScoringPolicies.ImportanceFirst(task);
                });
            });
        Assert(result is Result<DailyPlan>.Failure failure && failure.Error.Contains("minutes"),
            "Invalid duration must remain a failure.");
        Equal(0, plans);
        Equal(0, scores);
    }

    private static void ReadFailureSkipsParsing()
    {
        int parses = 0, plans = 0;
        var result = Result<string>.Fail("Cannot read input file.")
            .Bind(text => { parses++; return TaskParser.Parse(text); })
            .Map(tasks => { plans++; return TaskPipeline.CreatePlan(tasks, Day, 3, ScoringPolicies.ImportanceFirst); });
        Assert(result is Result<DailyPlan>.Failure { Error: "Cannot read input file." }, "Original error was lost.");
        Equal(0, parses);
        Equal(0, plans);
    }

    private static void BindFailureSkipsFollowingStep()
    {
        int binds = 0, maps = 0;
        var result = Result<string>.Ok("not JSON")
            .Bind(TaskParser.Parse)
            .Bind(tasks => { binds++; return Result<int>.Ok(tasks.Count); })
            .Map(count => { maps++; return count + 1; });
        Assert(result is Result<int>.Failure, "Malformed JSON must fail.");
        Equal(0, binds);
        Equal(0, maps);
    }
}
