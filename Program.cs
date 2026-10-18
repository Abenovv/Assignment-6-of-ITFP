using System;
using System.Globalization;
using Tracker.Domain;
using Tracker.Input;

namespace Tracker
{
    class Program
    {
        private static Result<string> ReadInput(string path)
        {
            try { return Result<string>.Ok(File.ReadAllText(path)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return Result<string>.Fail($"Cannot read input file: {error.Message}");
            }
        }

        static int Main(string[] args)
        {
            if (args is ["--workflow-demo"])
                return RunWorkflowDemo();

            if (args.Length > 5)
            {
                Console.Error.WriteLine("Usage: DailyPlanner [file] [yyyy-MM-dd] [both|importance|short] [minimum importance] [available minutes]");
                return 1;
            }

            string path = args.Length > 0 ? args[0] : "samples/tasks.json";
            string dateText = args.Length > 1 ? args[1] : "2026-10-13";
            string policyName = args.Length > 2 ? args[2] : "both";
            string minimumText = args.Length > 3 ? args[3] : "3";
            string availableText = args.Length > 4 ? args[4] : "180";

            if (!int.TryParse(availableText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int availableMinutes))
            {
                Console.Error.WriteLine("Available minutes must be an integer.");
                return 1;
            }

            if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly date))
            {
                Console.Error.WriteLine("Date must be a real date in yyyy-MM-dd format.");
                return 1;
            }

            if (!int.TryParse(minimumText, NumberStyles.None, CultureInfo.InvariantCulture, out int minimum)
                || minimum < 1 || minimum > 5)
            {
                Console.Error.WriteLine("Minimum importance must be an integer from 1 to 5.");
                return 1;
            }

            // Policy selection belongs to the application, outside the pure stages.
            var policies = new List<(string Name, Func<TaskItem, int> Score)>();
            switch (policyName)
            {
                case "both":
                    policies.Add(("Importance first", ScoringPolicies.ImportanceFirst));
                    policies.Add(("Short tasks first", ScoringPolicies.ShortTasksFirst));
                    break;
                case "importance":
                    policies.Add(("Importance first", ScoringPolicies.ImportanceFirst));
                    break;
                case "short":
                    policies.Add(("Short tasks first", ScoringPolicies.ShortTasksFirst));
                    break;
                default:
                    Console.Error.WriteLine("Policy must be both, importance, or short.");
                    return 1;
            }

            var plans = ReadInput(path)
                .Bind(TaskParser.Parse)
                .Map(tasks => policies.Select(policy =>
                    (policy.Name, Plan: TaskPipeline.CreatePlan(tasks, date, minimum, policy.Score))).ToArray());

            if (plans is Result<(string Name, DailyPlan Plan)[]>.Failure failure)
            {
                Console.Error.WriteLine(failure.Error);
                return 1;
            }
            if (plans is not Result<(string Name, DailyPlan Plan)[]>.Success success)
                throw new InvalidOperationException("Unknown planning result.");

            bool rejected = false;
            foreach (var policy in success.Value)
            {
                DailyPlan plan = policy.Plan;
                Console.WriteLine($"{policy.Name} | {plan.Date:yyyy-MM-dd}");
                foreach (RankedTask item in plan.Tasks)
                {
                    Console.WriteLine($"{item.Position}. {item.Task.Id} | {item.Task.Title} | score {item.Score} | {item.Task.Minutes} min");
                }
                Console.WriteLine($"Selected: {plan.Tasks.Count}; total: {plan.TotalMinutes} min");
                var preparation = PlanningWorkflow.Prepare(PlanningState.Start(plan, availableMinutes));
                Console.WriteLine(PlanningPresentation.Describe(preparation));
                switch (preparation)
                {
                    case PlanningTransition.Changed { Next: PlanningState.Prepared prepared }:
                        Console.WriteLine(PlanningPresentation.Describe(PlanningWorkflow.Confirm(prepared)));
                        break;
                    case PlanningTransition.Rejected:
                        rejected = true;
                        break;
                    default:
                        throw new InvalidOperationException("Preparation returned an unexpected state.");
                }
                Console.WriteLine();
            }
            return rejected ? 1 : 0;
        }

        private static int RunWorkflowDemo()
        {
            string json;
            try { json = File.ReadAllText("samples/tasks.json"); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
            if (!TaskParser.TryParse(json, out var tasks, out string errorMessage))
            {
                Console.Error.WriteLine(errorMessage);
                return 1;
            }

            var plan = TaskPipeline.CreatePlan(tasks, new DateOnly(2026, 10, 13), 3, ScoringPolicies.ImportanceFirst);
            var draft = PlanningState.Start(plan, 180);
            Console.WriteLine("Missing prerequisite: " + PlanningPresentation.Describe(PlanningWorkflow.Confirm(draft)));
            Console.WriteLine("Factory failure: " + PlanningPresentation.Describe(ValidatedPlanningSession.Create(plan, 119)));
            Console.WriteLine("Rejected preparation: " + PlanningPresentation.Describe(PlanningWorkflow.Prepare(PlanningState.Start(plan, 119))));

            switch (PlanningWorkflow.Prepare(draft))
            {
                case PlanningTransition.Changed { Next: PlanningState.Prepared prepared }:
                    Console.WriteLine(PlanningPresentation.Describe(prepared));
                    switch (PlanningWorkflow.Confirm(prepared))
                    {
                        case PlanningTransition.Changed { Next: PlanningState.Confirmed confirmed }:
                            Console.WriteLine(PlanningPresentation.Describe(confirmed));
                            Console.WriteLine("Repeated operation: " + PlanningPresentation.Describe(PlanningWorkflow.Confirm(confirmed)));
                            Console.WriteLine("Still confirmed: " + PlanningPresentation.Describe(confirmed));
                            return 0;
                        case PlanningTransition.Rejected failure:
                            Console.Error.WriteLine(PlanningPresentation.Describe(failure));
                            return 1;
                        default:
                            throw new InvalidOperationException("Confirmation returned an unexpected state.");
                    }
                case PlanningTransition.Rejected failure:
                    Console.Error.WriteLine(PlanningPresentation.Describe(failure));
                    return 1;
                default:
                    throw new InvalidOperationException("Preparation returned an unexpected state.");
            }
        }
    }
}
