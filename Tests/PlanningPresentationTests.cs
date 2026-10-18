using Tracker.Domain;

namespace Tracker.Tests;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> PlanningPresentationCases()
    {
        yield return ("Every state uses the expected matching branch", () =>
        {
            var draft = PlanningState.Start(OneTaskPlan(), 120);
            var prepared = Changed(PlanningWorkflow.Prepare(draft));
            var confirmed = Changed(PlanningWorkflow.Confirm(prepared));
            Equal("2026-10-13: waiting for preparation", PlanningPresentation.Describe(draft));
            Equal("2026-10-13: ready to confirm; 110 minutes left", PlanningPresentation.Describe(prepared));
            Equal("PLAN-20261013: confirmed 1 tasks, 10 minutes", PlanningPresentation.Describe(confirmed));
        });
        yield return ("Both factory cases produce the expected matching result", () =>
        {
            var good = ValidatedPlanningSession.Create(OneTaskPlan(), 120);
            var bad = ValidatedPlanningSession.Create(OneTaskPlan(minutes: 121), 120);
            Equal("2026-10-13: validation passed", PlanningPresentation.Describe(good));
            Equal("Invalid: The selected tasks exceed the available time.", PlanningPresentation.Describe(bad));
        });
        yield return ("Both transition cases determine the displayed result", () =>
        {
            var draft = PlanningState.Start(OneTaskPlan(), 120);
            Equal("2026-10-13: ready to confirm; 110 minutes left", PlanningPresentation.Describe(PlanningWorkflow.Prepare(draft)));
            Equal("Rejected: Prepare the plan before confirmation.", PlanningPresentation.Describe(PlanningWorkflow.Confirm(draft)));
        });
    }
}
