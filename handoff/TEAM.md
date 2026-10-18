# Team handoff

**Current team: four students, all IT-2511.**

| Member | Name | Work | Tests |
| --- | --- | --- | --- |
| 1 | Yerkebulan Abenov | Input and CLI; validated session factory and time budget | `InputTests.cs`, `PlanningFactoryTests.cs` |
| 2 | Nurbergen Assan | Selection; closed planning-state model | `SelectionTests.cs`, `PlanningStateTests.cs` |
| 3 | Amanalina Tumargul | Scoring; prepare and confirm transitions | `ScoringTests.cs`, `PlanningWorkflowTests.cs` |
| 4 | Khalid Ahmad Ahmadi | Plan construction; eligibility rules and result presentation | `PlanTests.cs`, `PlanningRulesTests.cs`, `PlanningPresentationTests.cs` |

This is the planned split for the prepared changes. Each person reviews and adapts their logic, improves a useful test, runs the complete project and explains their part during the defense. The existing DailyPlanner baseline remains attributed to Yerkebulan.

Exact file lists are in `ownership.json`, case `4`. Member 1 also handles `common`: project setup, samples, documentation and the shared test runner. Integrate work in order: Yerkebulan, Nurbergen, Amanalina, Khalid. Each later member waits for the previous change to be merged.

Use the `team-4` packages:

- `common-bootstrap.zip`: shared setup, used by Yerkebulan.
- `member-N-contribution.zip`: assigned files for that person's Git change.
- `member-N-submission.zip`: complete runnable project for Moodle.

Each person's combined bundle also contains both assignments, `START_HERE.md` and a `Prepare-Commit.ps1` helper. The helper prepares a branch and stages assigned paths; you inspect the diff, commit and push yourself. Follow `GIT_STEPS.md` for the manual sequence. Refresh each submission ZIP from the final tested team version before uploading.

## Smaller-team alternatives

These remain available for a different team arrangement:

| Size | Member 1 | Member 2 | Member 3 |
| --- | --- | --- | --- |
| 2 | Groups 1 and 2 above | Groups 3 and 4 above | — |
| 3 | Group 1 above | Group 2 above | Groups 3 and 4 above |

Use the matching `ownership.json` case and `team-2` or `team-3` packages. The current submission uses four members.
