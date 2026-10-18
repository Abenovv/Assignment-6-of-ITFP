namespace Tracker.Domain;

public static class PlanningRules
{
    public static IReadOnlyList<string> Check(DailyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var reasons = new List<string>();
        if (plan.Tasks.Any(item => item.Task.Date != plan.Date))
            reasons.Add("Every task must belong to the plan date.");
        if (plan.Tasks.Any(item => item.Task.Completed))
            reasons.Add("Completed tasks cannot be confirmed in a new plan.");
        if (plan.Tasks.Any(item => item.Task.Minutes < 0 || item.Task.Minutes > 480))
            reasons.Add("Each task duration must be between 0 and 480 minutes.");
        if (plan.Tasks.Any(item => item.Task.Importance < 1 || item.Task.Importance > 5))
            reasons.Add("Each task importance must be between 1 and 5.");
        if (plan.Tasks.Any(item => string.IsNullOrWhiteSpace(item.Task.Id) || string.IsNullOrWhiteSpace(item.Task.Title)))
            reasons.Add("Every task needs an ID and title.");
        if (plan.Tasks.Select(item => item.Task.Id).Distinct(StringComparer.Ordinal).Count() != plan.Tasks.Count)
            reasons.Add("Task IDs must be unique within the plan.");
        if (!plan.Tasks.Select(item => item.Position).SequenceEqual(Enumerable.Range(1, plan.Tasks.Count)))
            reasons.Add("Task positions must start at 1 and remain consecutive.");
        return Array.AsReadOnly(reasons.ToArray());
    }
}
