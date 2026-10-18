# Daily task planner — Assignment 6

Team: Yerkebulan Abenov, Nurbergen Assan, Amanalina Tumargul and Khalid Ahmad Ahmadi — four students, all IT-2511.

Repository: https://github.com/Abenovv/Assignment-6-of-ITFP

This is the same daily planner as Assignment 5. It selects unfinished tasks for a chosen date, scores them with a supplied rule and orders the result. Assignment 6 adds preparation and confirmation: a student must have a nonempty, eligible plan that fits their available time before confirming it.

## Build and run

Use .NET SDK **10.0.401**. No external packages, database or account are needed. Run from the extracted folder containing `DailyPlanner.sln`:

```powershell
dotnet build DailyPlanner.sln --disable-build-servers --maxcpucount:1
dotnet run --project DailyPlanner.csproj --no-build
dotnet run --project DailyPlanner.csproj --no-build -- --workflow-demo
dotnet run --project Tests/DailyPlanner.Tests.csproj --no-build
```

The last command runs assertion tests and exits with code 1 on failure. This project uses its own runner, so use that command instead of `dotnet test`.

Default input is `samples/tasks.json`, date `2026-10-13`, both scoring policies, minimum importance 3 and 180 available minutes. To change these values:

```powershell
dotnet run --project DailyPlanner.csproj --no-build -- samples/tasks.json 2026-10-13 importance 3 120
```

Arguments are the input path, date, policy (`both`, `importance`, `short`), minimum importance and available minutes. Omitted trailing arguments use the defaults. Paths are relative to the working directory.

## Input, rules and expected result

Each task in the JSON array requires `id`, `title`, `date`, `importance`, `minutes` and `completed`. IDs are nonblank and unique; titles are nonblank; dates use `yyyy-MM-dd`; importance is 1–5; minutes are 0–480; completed is a boolean. Unknown, missing or null fields and invalid values reject the complete input. A zero-minute task is a reminder and remains valid.

The Assignment 5 stages remain `SelectTasks`, `ScoreTasks` and `BuildPlan`, composed by `TaskPipeline.CreatePlan`. Selection keeps unfinished tasks on the requested date whose importance meets the threshold. `ScoreTasks` accepts `Func<TaskItem, int>` and calls it for each selected task. The two pure policies are:

- Importance first: `importance * 10`.
- Short tasks first: `importance * 10 + max(0, 60 - minutes)`.

Higher scores rank first. Ties use ordinal ID order; positions begin at 1. The sample produces T1, T2, T7, T3 under importance first and T3, T7, T2, T1 under short tasks first. Both contain four tasks and total 120 minutes.

Confirmation adds these rules: available time is 1–480 minutes; the plan contains at least one task; total minutes do not exceed available time. Tasks must still have valid fields, match the plan date, be unfinished, have unique IDs and use consecutive positions. These checks also protect callers who construct domain records directly rather than using the JSON parser. A nonempty plan of zero-minute reminders can be confirmed.

With the defaults, each policy ends with:

```text
Selected: 4; total: 120 min
2026-10-13: ready to confirm; 60 minutes left
PLAN-20261013: confirmed 4 tasks, 120 minutes
```

The reference labels the plan date. Confirmation returns a new value in memory; it does not save tasks or issue a globally unique database identifier. The two default policies are separate comparisons of the same task list.

## The hidden dependency

An unsafe design would keep the time setting in a mutable object:

```csharp
class UnsafePlanner
{
    private int availableMinutes = 480;
    public void SetAvailableTime(int minutes) => availableMinutes = minutes;
    public bool Confirm(DailyPlan plan) => plan.TotalMinutes <= availableMinutes;
}

var planner = new UnsafePlanner();
bool accepted = planner.Confirm(plan); // 120 minutes passes against the default 480.
planner.SetAvailableTime(119);        // Too late: the student's limit was 119.
```

`SetAvailableTime(119)` must run before `Confirm(plan)`. Changing the order accepts a plan that does not fit. This example is only in the README.

The active path takes the plan and capacity together:

```csharp
var draft = PlanningState.Start(plan, 119);
var result = PlanningWorkflow.Prepare(draft);
// Rejected: The selected tasks exceed the available time.
```

Preparation invokes `ValidatedPlanningSession.Create`. It returns either `PlanningCreation.Valid`, carrying a usable session, or `PlanningCreation.Invalid`, carrying reasons. A valid session has a private constructor, a sealed class and read-only properties. It is not a record, so `with` cannot change a validated value. Its plan owns a copied read-only task collection. No partially validated session is returned. A call such as `new ValidatedPlanningSession(plan, 180)` is blocked because the constructor is private; callers must use `Create`.

## States and transitions

| State | Payload | Meaning |
| --- | --- | --- |
| `Draft` | Proposed plan and available minutes | Validation has not succeeded yet. |
| `Prepared` | `ValidatedPlanningSession` | All preparation rules passed. |
| `Confirmed` | Validated session and date reference | Confirmation succeeded. |

`PlanningState` has a private base constructor; its three nested cases are sealed with internal constructors. External callers cannot add cases or build a confirmed state directly. `FromValidated` only accepts a value created by the validated factory.

`Prepare` matches all three states. It validates a draft, rejects an already prepared plan and rejects a confirmed plan. Its factory match explicitly handles `Valid` and `Invalid`. `Confirm` also matches all three states: it rejects a draft with a missing-prerequisite message, creates a new confirmed value from a prepared state and rejects repeated confirmation. Each fallback throws for an unexpected case.

A transition is either `Changed`, carrying the next state, or `Rejected`, carrying the original state and reasons. `Program.cs` and `PlanningPresentation` match the outcomes used by the application. No cast or null-forgiving access is needed to obtain a successful payload.

Repeated preparation and confirmation are rejected. Rejection preserves the same original state reference and its data. Domain methods do not read files, print, modify task lists or store data; only `Program.cs` performs input and output.

## Demonstrations

`--workflow-demo` uses the real sample parser and pipeline. It prints:

```text
Missing prerequisite: Rejected: Prepare the plan before confirmation.
Factory failure: Invalid: The selected tasks exceed the available time.
Rejected preparation: Rejected: The selected tasks exceed the available time.
2026-10-13: ready to confirm; 60 minutes left
PLAN-20261013: confirmed 4 tasks, 120 minutes
Repeated operation: Rejected: The plan is already confirmed.
Still confirmed: PLAN-20261013: confirmed 4 tasks, 120 minutes
```

For separate command-line checks:

```powershell
dotnet run --project DailyPlanner.csproj --no-build -- samples/tasks.json 2026-10-13 importance 3 119
dotnet run --project DailyPlanner.csproj --no-build -- samples/empty.json
dotnet run --project DailyPlanner.csproj --no-build -- samples/invalid.json
dotnet run --project DailyPlanner.csproj --no-build -- samples/missing-field.json
dotnet run --project DailyPlanner.csproj --no-build -- samples/malformed.json
```

The 119-minute run displays the plan, rejects preparation and exits 1. Empty input still forms an empty pipeline result, but confirmation now rejects it and exits 1. Invalid JSON data exits 1 before the workflow begins. Normal confirmation and the deliberate demonstration command exit 0.

## Requirement map

| Requirement | Implementation |
| --- | --- |
| Before example and order failure | Hidden dependency section above |
| Used validated factory and restricted construction | `Domain/PlanningSessionFactory.cs`: `ValidatedPlanningSession.Create` |
| Capacity and task eligibility checks | Factory plus `Domain/PlanningRules.cs`: `Check` |
| Distinct cases with relevant payloads | `Domain/PlanningState.cs`, `PlanningCreation`, `PlanningTransition` |
| Used exhaustive pattern matching | `PlanningWorkflow.Prepare`, `Confirm`, `PrepareDraft`; `Program.cs` and `PlanningPresentation` |
| Safe transitions, explicit prerequisite and repetition rule | `Domain/PlanningWorkflow.cs` |
| Actual pipeline input used by factory | Normal CLI and `RunWorkflowDemo` in `Program.cs` |
| Pure domain decisions, immutable data | `Domain/`; copied collections and get-only properties |
| Assertion tests and failure exit code | `Tests/Program.cs` and the test files below |

## Tests

Verification on October 18, 2026: build succeeded with **0 warnings and 0 errors; 40/40 tests passed**. The normal run and workflow demonstration matched the expected output above.

| Test file | Expected assertions |
| --- | --- |
| `InputTests.cs` | Reject malformed JSON, missing/null input and fields, invalid fields and duplicate IDs; accept 480 minutes and reject 481; accept zero-minute unfinished reminders. |
| `SelectionTests.cs` | Select only eligible tasks; include exact importance threshold; reject invalid thresholds. |
| `ScoringTests.cs` | Both policies give expected scores and different orders; delegate runs once per task. |
| `PlanTests.cs` | Sample order, totals and positions; empty results; ordinal tie order; repeatability; unchanged inputs and defensive copies. |
| `PlanningFactoryTests.cs` | Valid payload and remaining time; reject missing/empty plans, each invalid capacity and overload; accept exact 1/480 limits and reminders; preserve input on failure; prevent unchecked construction and mutation. |
| `PlanningRulesTests.cs` | Reject every date, completion, duration, importance, ID/title, duplicate and position rule through the used factory; reasons are read-only. |
| `PlanningStateTests.cs` | Reach each of Draft, Prepared and Confirmed with correct payloads; public entry points reject missing values; cases are closed and read-only. |
| `PlanningWorkflowTests.cs` | Pipeline-to-confirmation success; missing prerequisite; failed factory preparation; repeated confirmation/preparation; state and source collection preservation. |
| `PlanningPresentationTests.cs` | Exact expected matching result for every state and both factory and transition outcomes. |

There are 17 retained Assignment 5 tests and 23 Assignment 6 tests. They compare actual and expected values; printing the demonstration alone does not count as a test.

## Team work and defense

The prepared four-person work split builds on Yerkebulan's existing DailyPlanner baseline. Each member reviews, adapts, tests and defends their part.

| Member, IT-2511 | Logic to review and develop | Tests to own |
| --- | --- | --- |
| Yerkebulan Abenov | Input and CLI; validated session factory and time-budget checks | `InputTests.cs`, `PlanningFactoryTests.cs` |
| Nurbergen Assan | Selection; closed planning-state model | `SelectionTests.cs`, `PlanningStateTests.cs` |
| Amanalina Tumargul | Scoring; prepare and confirm transitions | `ScoringTests.cs`, `PlanningWorkflowTests.cs` |
| Khalid Ahmad Ahmadi | Plan construction; eligibility rules and result presentation | `PlanTests.cs`, `PlanningRulesTests.cs`, `PlanningPresentationTests.cs` |

Use case `4` in `handoff/ownership.json` and the `team-4` packages. `handoff/TEAM.md` keeps two- and three-person alternatives. The personal bundle's `START_HERE.md` gives the Git steps for both assignments.

For the defense, trace the sample's 120-minute plan through Draft, factory validation, Prepared and Confirmed. Explain why 119 available minutes fails, why confirming a Draft fails and why confirming a Confirmed plan again is rejected. Point to the private validated constructor and the explicit switch branches. Each person should run and explain their assigned tests.

For a small live change, change the 480-minute session limit in the factory to 420 and update the relevant capacity boundary assertions before rebuilding. The task parser's separate per-task limit stays 480 unless that rule is also intentionally changed. Keep this workflow for Assignment 7.
