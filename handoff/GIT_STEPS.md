# Git handoff

Repository: https://github.com/Abenovv/Assignment-6-of-ITFP

Current order: **1. Yerkebulan Abenov → 2. Nurbergen Assan → 3. Amanalina Tumargul → 4. Khalid Ahmad Ahmadi**, all IT-2511. Each person uses their own Git identity. A later member waits for the previous change to reach the default branch before cloning or pulling.

## Personal-bundle helper

From the extracted personal bundle containing `START_HERE.md`, `member.json` and `Prepare-Commit.ps1`, use the command for this assignment:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Prepare-Commit.ps1 -Assignment 6 -RepositoryPath 'C:\path\to\Assignment-6-of-ITFP'
```

The helper selects a branch and stages your assigned files, adding common setup for member 1. It never commits or pushes. Inspect its output and staged diff, then follow the commit and PR steps below. `START_HERE.md` includes the exact commands for both assignments.

Inside the personal bundle, `Assignment6/Submission.zip` is the full runnable project, `Contribution.zip` is your overlay, and `Common.zip` is included for member 1. The manual sequence below uses the equivalent `team-4` package names.

## Manual setup

Set your member number from the order above and replace the package path:

```powershell
git clone https://github.com/Abenovv/Assignment-6-of-ITFP.git
Set-Location Assignment-6-of-ITFP
$memberNumber = 1
$teamSize = "4"
$member = "member$memberNumber"
$packageDir = "C:\path\to\packages\team-4"
git status --short
git config user.name
git config user.email
$remoteRefs = @(git ls-remote origin)
if ($LASTEXITCODE -ne 0) { throw "The remote check failed." }
git remote show origin
```

If the remote has refs, set `$baseBranch` below to its reported HEAD branch. If it has no refs, Yerkebulan starts `main`. Preserve any existing history and inspect existing same-named files before extracting a package.

```powershell
$baseBranch = "main"
if ($remoteRefs.Count -eq 0) {
    if ($memberNumber -ne 1) { throw "Wait for Yerkebulan's first push." }
    git switch -c main
} else {
    git switch $baseBranch
    git pull --ff-only origin $baseBranch
    git switch -c "assignment-6-member-$memberNumber"
}
```

If you already have a clone, skip cloning, set the variables and rerun the remote check. Start with a clean working tree and pull the previous member's merged change before creating your feature branch.

Extract the shared setup only for member 1, then your own contribution:

```powershell
if ($memberNumber -eq 1) {
    Expand-Archive -LiteralPath "$packageDir\common-bootstrap.zip" -DestinationPath . -Force
}
Expand-Archive -LiteralPath "$packageDir\member-$memberNumber-contribution.zip" -DestinationPath . -Force
$ownership = Get-Content handoff/ownership.json -Raw | ConvertFrom-Json
$ownedFiles = @($ownership.$teamSize.$member)
$ownedFiles
git diff --stat
```

Review and adapt your logic and tests. Use the complete submission ZIP for testing while the repository is still missing later members' files. From its extracted project root, run:

```powershell
dotnet build DailyPlanner.sln
dotnet run --project Tests/DailyPlanner.Tests.csproj
dotnet run --project DailyPlanner.csproj -- samples/tasks.json
```

After Khalid's change is merged, run the same checks from the integrated repository.

## Commit, push and review

In your repository clone, stage only the assigned paths. Member 1 includes shared setup in the first change:

```powershell
$stagedFiles = @($ownedFiles)
if ($memberNumber -eq 1) { $stagedFiles += @($ownership.common) }
git add -- $stagedFiles
git diff --cached --stat
git diff --cached
git commit
$currentBranch = git branch --show-current
git push -u origin $currentBranch
```

Write a message describing the actual logic or tests you reviewed and changed. Do not commit `MEMBER.md`, archives, `bin`, `obj` or a `.git` folder.

For the first push to an empty repository, Yerkebulan pushes `main` directly because no base branch exists yet. For every feature branch, open the repository on GitHub, choose **Pull requests → New pull request**, and compare your feature branch against the default branch. Have a teammate review it, resolve any comments and merge it. Tell the next person when the merge is complete.

## Submission

Refresh each person's complete Moodle ZIP from the final tested project. Keep the four names and IT-2511 in the README. Contribution ZIPs contain only part of the project and cannot run alone.

Two- and three-member alternatives use `$teamSize = "2"` or `"3"`, the matching package folder and `ownership.json` case. The current team uses case `4`.
