namespace Tracker.Domain;

public abstract class PlanningState
{
    private PlanningState() { }

    public static Draft Start(DailyPlan plan, int availableMinutes)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new Draft(plan, availableMinutes);
    }

    public static Prepared FromValidated(ValidatedPlanningSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new Prepared(session);
    }

    public sealed class Draft : PlanningState
    {
        public DailyPlan Plan { get; }
        public int AvailableMinutes { get; }
        internal Draft(DailyPlan plan, int availableMinutes)
        {
            Plan = plan;
            AvailableMinutes = availableMinutes;
        }
    }

    public sealed class Prepared : PlanningState
    {
        public ValidatedPlanningSession Session { get; }
        internal Prepared(ValidatedPlanningSession session) => Session = session;
    }

    public sealed class Confirmed : PlanningState
    {
        public ValidatedPlanningSession Session { get; }
        public string Reference { get; }
        internal Confirmed(ValidatedPlanningSession session)
        {
            Session = session;
            Reference = $"PLAN-{session.Plan.Date:yyyyMMdd}";
        }
    }
}
