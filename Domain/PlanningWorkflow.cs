namespace Tracker.Domain;

public static class PlanningWorkflow
{
    public static PlanningTransition Prepare(PlanningState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state switch
        {
            PlanningState.Draft draft => PrepareDraft(draft),
            PlanningState.Prepared prepared => Reject(prepared, "The plan is already prepared."),
            PlanningState.Confirmed confirmed => Reject(confirmed, "The plan is already confirmed."),
            _ => throw new ArgumentException("Unknown planning state.", nameof(state))
        };
    }

    public static PlanningTransition Confirm(PlanningState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state switch
        {
            PlanningState.Draft draft => Reject(draft, "Prepare the plan before confirmation."),
            PlanningState.Prepared prepared => new PlanningTransition.Changed(new PlanningState.Confirmed(prepared.Session)),
            PlanningState.Confirmed confirmed => Reject(confirmed, "The plan is already confirmed."),
            _ => throw new ArgumentException("Unknown planning state.", nameof(state))
        };
    }

    private static PlanningTransition PrepareDraft(PlanningState.Draft draft) =>
        ValidatedPlanningSession.Create(draft.Plan, draft.AvailableMinutes) switch
        {
            PlanningCreation.Valid valid => new PlanningTransition.Changed(PlanningState.FromValidated(valid.Session)),
            PlanningCreation.Invalid invalid => new PlanningTransition.Rejected(draft, invalid.Reasons),
            _ => throw new InvalidOperationException("Unknown factory outcome.")
        };

    private static PlanningTransition.Rejected Reject(PlanningState state, string reason) => new(state, [reason]);
}

public abstract class PlanningTransition
{
    private PlanningTransition() { }

    public sealed class Changed : PlanningTransition
    {
        public PlanningState Next { get; }
        internal Changed(PlanningState next) => Next = next;
    }

    public sealed class Rejected : PlanningTransition
    {
        public PlanningState Original { get; }
        public IReadOnlyList<string> Reasons { get; }
        internal Rejected(PlanningState original, IEnumerable<string> reasons)
        {
            Original = original;
            Reasons = Array.AsReadOnly(reasons.ToArray());
        }
    }
}
