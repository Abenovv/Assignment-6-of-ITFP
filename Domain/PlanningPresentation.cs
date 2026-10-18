namespace Tracker.Domain;

public static class PlanningPresentation
{
    public static string Describe(PlanningState state) => state switch
    {
        PlanningState.Draft draft => $"{draft.Plan.Date:yyyy-MM-dd}: waiting for preparation",
        PlanningState.Prepared prepared => $"{prepared.Session.Plan.Date:yyyy-MM-dd}: ready to confirm; {prepared.Session.RemainingMinutes} minutes left",
        PlanningState.Confirmed confirmed => $"{confirmed.Reference}: confirmed {confirmed.Session.Plan.Tasks.Count} tasks, {confirmed.Session.Plan.TotalMinutes} minutes",
        _ => throw new ArgumentException("Unknown planning state.", nameof(state))
    };

    public static string Describe(PlanningTransition transition) => transition switch
    {
        PlanningTransition.Changed changed => Describe(changed.Next),
        PlanningTransition.Rejected rejected => $"Rejected: {string.Join(" ", rejected.Reasons)}",
        _ => throw new ArgumentException("Unknown planning transition.", nameof(transition))
    };

    public static string Describe(PlanningCreation creation) => creation switch
    {
        PlanningCreation.Valid valid => $"{valid.Session.Plan.Date:yyyy-MM-dd}: validation passed",
        PlanningCreation.Invalid invalid => $"Invalid: {string.Join(" ", invalid.Reasons)}",
        _ => throw new ArgumentException("Unknown factory outcome.", nameof(creation))
    };
}
