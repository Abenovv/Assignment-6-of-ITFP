using Tracker.Domain;

namespace Tracker.Tests;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> PlanningRulesCases()
    {
        yield return ("Eligibility checks task dates and completion through the factory", () =>
        {
            InvalidSession(OneTaskPlan(date: Day.AddDays(1)), 120, "date");
            InvalidSession(OneTaskPlan(completed: true), 120, "Completed");
        });
        yield return ("Eligibility checks task duration and importance through the factory", () =>
        {
            foreach (int duration in new[] { -1, 481 })
                InvalidSession(OneTaskPlan(minutes: duration), 480, "duration");
            foreach (int importance in new[] { 0, 6 })
                InvalidSession(OneTaskPlan(importance: importance), 120, "importance");
        });
        yield return ("Eligibility checks IDs titles duplicates and positions through the factory", () =>
        {
            InvalidSession(OneTaskPlan(id: " "), 120, "ID and title");
            InvalidSession(OneTaskPlan(title: " "), 120, "ID and title");
            var task = new TaskItem("T1", "Read notes", Day, 3, 10, false);
            InvalidSession(new DailyPlan(Day, [new(1, task, 30), new(2, task, 30)]), 120, "unique");
            InvalidSession(new DailyPlan(Day, [new(2, task, 30)]), 120, "positions");
        });
        yield return ("Eligibility reasons are read only and a valid plan has none", () =>
        {
            Equal(0, PlanningRules.Check(OneTaskPlan()).Count);
            var plan = OneTaskPlan(completed: true);
            var reasons = PlanningRules.Check(plan);
            Throws<NotSupportedException>(() => ((IList<string>)reasons).Clear());
            Equal(true, plan.Tasks[0].Task.Completed);
        });
    }
}
