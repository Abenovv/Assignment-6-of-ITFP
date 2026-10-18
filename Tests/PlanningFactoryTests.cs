using Tracker.Domain;

namespace Tracker.Tests;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> PlanningFactoryCases()
    {
        yield return ("Factory creates a usable session from a pipeline plan", () =>
        {
            var session = ValidSession(TaskPipeline.CreatePlan(Sample(), Day, 3, ScoringPolicies.ImportanceFirst), 180);
            Equal(Day, session.Plan.Date);
            Equal(120, session.Plan.TotalMinutes);
            Equal(180, session.AvailableMinutes);
            Equal(60, session.RemainingMinutes);
        });
        yield return ("Factory rejects an empty plan and missing plan", () =>
        {
            InvalidSession(new DailyPlan(Day, []), 120, "at least one");
            Assert(ValidatedPlanningSession.Create(null, 120) is PlanningCreation.Invalid, "Missing plan accepted.");
        });
        yield return ("Factory rejects each invalid capacity", () =>
        {
            foreach (int capacity in new[] { -1, 0, 481 })
                InvalidSession(OneTaskPlan(), capacity, "Available time");
        });
        yield return ("Factory rejects an overloaded plan", () => InvalidSession(OneTaskPlan(minutes: 121), 120, "exceed"));
        yield return ("Factory accepts exact limits and zero minute reminders", () =>
        {
            Equal(0, ValidSession(OneTaskPlan(minutes: 480), 480).RemainingMinutes);
            Equal(0, ValidSession(OneTaskPlan(minutes: 1), 1).RemainingMinutes);
            Equal(1, ValidSession(OneTaskPlan(minutes: 0), 1).RemainingMinutes);
        });
        yield return ("Factory failure preserves the plan and returns read only reasons", () =>
        {
            var plan = OneTaskPlan(minutes: 121);
            var before = plan.Tasks.ToArray();
            if (ValidatedPlanningSession.Create(plan, 120) is not PlanningCreation.Invalid invalid)
                throw new Exception("Expected Invalid.");
            Sequence(before, plan.Tasks);
            Equal(121, plan.TotalMinutes);
            Throws<NotSupportedException>(() => ((IList<string>)invalid.Reasons).Add("changed"));
        });
        yield return ("Validated sessions cannot be constructed or edited unchecked", () =>
        {
            Equal(0, typeof(ValidatedPlanningSession).GetConstructors().Length);
            Assert(typeof(ValidatedPlanningSession).IsSealed, "Session must be sealed.");
            Assert(typeof(ValidatedPlanningSession).GetProperties().All(property => property.SetMethod is null), "Session has a setter.");
            var ranked = OneTaskPlan().Tasks.ToList();
            var session = ValidSession(new DailyPlan(Day, ranked), 120);
            ranked.Clear();
            Equal(1, session.Plan.Tasks.Count);
            Throws<NotSupportedException>(() => ((IList<RankedTask>)session.Plan.Tasks).Clear());
        });
    }

    private static DailyPlan OneTaskPlan(int minutes = 10, DateOnly? date = null, bool completed = false,
        int importance = 3, string id = "T1", string title = "Read notes") =>
        new(Day, [new(1, new TaskItem(id, title, date ?? Day, importance, minutes, completed), 30)]);

    private static ValidatedPlanningSession ValidSession(DailyPlan plan, int capacity = 120) =>
        ValidatedPlanningSession.Create(plan, capacity) switch
        {
            PlanningCreation.Valid valid => valid.Session,
            _ => throw new Exception("Expected a valid session.")
        };

    private static void InvalidSession(DailyPlan plan, int capacity, string reason)
    {
        if (ValidatedPlanningSession.Create(plan, capacity) is not PlanningCreation.Invalid invalid)
            throw new Exception("Expected Invalid.");
        Assert(invalid.Reasons.Any(message => message.Contains(reason, StringComparison.OrdinalIgnoreCase)),
            $"Expected {reason}; got {string.Join(" ", invalid.Reasons)}");
    }
}
