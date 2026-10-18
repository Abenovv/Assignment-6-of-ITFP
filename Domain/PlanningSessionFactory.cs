namespace Tracker.Domain;

public sealed class ValidatedPlanningSession
{
    public DailyPlan Plan { get; }
    public int AvailableMinutes { get; }
    public int RemainingMinutes => AvailableMinutes - Plan.TotalMinutes;

    private ValidatedPlanningSession(DailyPlan plan, int availableMinutes)
    {
        Plan = new DailyPlan(plan.Date, plan.Tasks);
        AvailableMinutes = availableMinutes;
    }

    public static PlanningCreation Create(DailyPlan? plan, int availableMinutes)
    {
        if (plan is null)
            return new PlanningCreation.Invalid(["A daily plan is required."]);

        var reasons = new List<string>();
        if (availableMinutes < 1 || availableMinutes > 480)
            reasons.Add("Available time must be between 1 and 480 minutes.");
        if (plan.Tasks.Count == 0)
            reasons.Add("Select at least one task before confirming a plan.");
        if (plan.TotalMinutes > availableMinutes)
            reasons.Add("The selected tasks exceed the available time.");
        reasons.AddRange(PlanningRules.Check(plan));

        return reasons.Count == 0
            ? new PlanningCreation.Valid(new ValidatedPlanningSession(plan, availableMinutes))
            : new PlanningCreation.Invalid(reasons);
    }
}

public abstract class PlanningCreation
{
    private PlanningCreation() { }

    public sealed class Valid : PlanningCreation
    {
        public ValidatedPlanningSession Session { get; }
        internal Valid(ValidatedPlanningSession session) => Session = session;
    }

    public sealed class Invalid : PlanningCreation
    {
        public IReadOnlyList<string> Reasons { get; }
        internal Invalid(IEnumerable<string> reasons) => Reasons = Array.AsReadOnly(reasons.ToArray());
    }
}
