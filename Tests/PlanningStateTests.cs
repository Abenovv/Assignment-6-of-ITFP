using Tracker.Domain;

namespace Tracker.Tests;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> PlanningStateCases()
    {
        yield return ("Draft carries the plan and proposed capacity", () =>
        {
            var plan = OneTaskPlan();
            var draft = PlanningState.Start(plan, 120);
            Equal(plan, draft.Plan);
            Equal(120, draft.AvailableMinutes);
            Throws<ArgumentNullException>(() => PlanningState.Start(null!, 120));
        });
        yield return ("Prepared requires a validated session", () =>
        {
            var session = ValidSession(OneTaskPlan());
            Assert(ReferenceEquals(session, PlanningState.FromValidated(session).Session), "Unexpected session.");
            Throws<ArgumentNullException>(() => PlanningState.FromValidated(null!));
        });
        yield return ("Confirmed carries the validated session and a reference", () =>
        {
            var session = ValidSession(OneTaskPlan());
            var transition = PlanningWorkflow.Confirm(PlanningState.FromValidated(session));
            if (transition is not PlanningTransition.Changed { Next: PlanningState.Confirmed confirmed })
                throw new Exception("Expected Confirmed.");
            Equal("PLAN-20261013", confirmed.Reference);
            Assert(ReferenceEquals(session, confirmed.Session), "Unexpected session.");
        });
        yield return ("State hierarchy is closed with read only payloads", () =>
        {
            Equal(0, typeof(PlanningState).GetConstructors().Length);
            foreach (var type in new[] { typeof(PlanningState.Draft), typeof(PlanningState.Prepared), typeof(PlanningState.Confirmed) })
            {
                Assert(type.IsSealed, "A state case is open to inheritance.");
                Equal(0, type.GetConstructors().Length);
                Assert(type.GetProperties().All(property => property.SetMethod is null), "State has a setter.");
            }
        });
    }
}
