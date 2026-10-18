using Tracker.Domain;

namespace Tracker.Tests;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> PlanningWorkflowCases()
    {
        yield return ("Actual pipeline plan is prepared and confirmed", () =>
        {
            var input = Sample().ToList();
            var before = input.ToArray();
            var plan = TaskPipeline.CreatePlan(input, Day, 3, ScoringPolicies.ImportanceFirst);
            var draft = PlanningState.Start(plan, 180);
            var prepared = Changed(PlanningWorkflow.Prepare(draft));
            Assert(prepared is PlanningState.Prepared, "Expected Prepared.");
            var confirmed = Changed(PlanningWorkflow.Confirm(prepared));
            Assert(confirmed is PlanningState.Confirmed { Session.Plan.TotalMinutes: 120 }, "Unexpected confirmed plan.");
            Sequence(before, input);
            Equal(120, draft.Plan.TotalMinutes);
        });
        yield return ("Missing preparation rejects confirmation and preserves the draft", () =>
        {
            var draft = PlanningState.Start(OneTaskPlan(), 120);
            Rejected(PlanningWorkflow.Confirm(draft), draft, "Prepare");
            Equal(10, draft.Plan.TotalMinutes);
            Equal(120, draft.AvailableMinutes);
        });
        yield return ("Overloaded plan rejects preparation without partial state", () =>
        {
            var draft = PlanningState.Start(OneTaskPlan(minutes: 121), 120);
            var before = draft.Plan.Tasks.ToArray();
            Rejected(PlanningWorkflow.Prepare(draft), draft, "exceed");
            Rejected(PlanningWorkflow.Confirm(draft), draft, "Prepare");
            Sequence(before, draft.Plan.Tasks);
            Equal(121, draft.Plan.TotalMinutes);
        });
        yield return ("Repeated confirmation is rejected without changing confirmation", () =>
        {
            var prepared = PlanningState.FromValidated(ValidSession(OneTaskPlan()));
            var confirmed = Changed(PlanningWorkflow.Confirm(prepared));
            Rejected(PlanningWorkflow.Confirm(confirmed), confirmed, "already confirmed");
            Assert(confirmed is PlanningState.Confirmed { Reference: "PLAN-20261013", Session.Plan.TotalMinutes: 10 }, "Confirmation changed.");
        });
        yield return ("Repeated preparation and preparation after confirmation are rejected", () =>
        {
            var prepared = PlanningState.FromValidated(ValidSession(OneTaskPlan()));
            Rejected(PlanningWorkflow.Prepare(prepared), prepared, "already prepared");
            var confirmed = Changed(PlanningWorkflow.Confirm(prepared));
            Rejected(PlanningWorkflow.Prepare(confirmed), confirmed, "already confirmed");
        });
    }

    private static PlanningState Changed(PlanningTransition transition) => transition switch
    {
        PlanningTransition.Changed changed => changed.Next,
        _ => throw new Exception("Expected a changed state.")
    };

    private static void Rejected(PlanningTransition transition, PlanningState original, string reason)
    {
        if (transition is not PlanningTransition.Rejected rejected)
            throw new Exception("Expected Rejected.");
        Assert(ReferenceEquals(original, rejected.Original), "Failure replaced the original state.");
        Assert(rejected.Reasons.Any(message => message.Contains(reason, StringComparison.OrdinalIgnoreCase)), "Wrong rejection reason.");
    }
}
