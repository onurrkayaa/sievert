# Sievert

A command line tool that looks at a .NET repository and estimates how risky each commit is.

## The problem

When you review a commit, you only see the diff. The code usually looks fine, because
people don't write obviously broken code on purpose. But part of the risk isn't in the
code at all - it's in the history of the file you are touching. A file that has been
fixed five times in the last two months, or that a lot of different people keep editing,
is more likely to break again. That information is sitting in git, it's just not in front
of you while you are reviewing.

## Approach

Two sources of signal, combined into one score:

- **Roslyn** parses the C# code and looks for .NET specific patterns that tend to cause
  bugs (things like async usage, disposal, null handling).
- **Git history** gives change signals for the files in the commit: how often they change,
  how many different authors touch them, how many past fixes they were part of.

The output is a risk score with a short explanation of why it came out that way. The
explanation matters more than the number - a score you can't question isn't useful.

## Project structure

```
Sievert.slnx                  solution file (new .slnx format, .NET 10)
Directory.Build.props         shared build settings for all projects
global.json                   pins the SDK version
src/
  Sievert.Core/               shared models and helpers, depends on nothing
  Sievert.Analysis/           the actual analysis: Roslyn + git history
  Sievert.Cli/                console app, this is what you run
tests/
  Sievert.Tests/              xUnit tests
samples/Patients/             sample repositories to test against
docs/adr/                     short notes on why things were decided this way
```

## Getting started

You need the .NET 10 SDK. The exact version is pinned in `global.json`.

```bash
git clone <repo-url>
cd sievert
dotnet build
dotnet test
dotnet run --project src/Sievert.Cli
```

Right now there are two commands. `sievert scan <path>` reads the C# files under a path
and prints the types and methods it found as a tree, with a summary at the end.
`sievert check <path>` runs the rules instead and prints each finding as a diagnostic
card; it exits with 1 if it finds anything at or above the `--fail-on` level (warning by
default). Both commands take `--json`. There are three rules so far:

| Code | Severity | What it looks for |
|---|---|---|
| SV001 | error | `async void` methods that aren't event handlers |
| SV002 | warning | blocking on a task with `.Result`, `.Wait()` or `.GetAwaiter().GetResult()` |
| SV003 | warning | a call whose returned task is dropped on the floor |

SV002 and SV003 both decide what is a task by looking at names, because there is no
semantic model yet. That produces false positives, which is a trade I took on purpose -
`docs/sinirliliklar.md` says what goes wrong in each direction.

Both commands also take `--exclude <pattern>`, and you can pass it more than once. `*`
matches inside one path segment and `**` matches zero or more segments, so patterns look
like this:

```bash
dotnet run --project src/Sievert.Cli -- check . --exclude 'samples/**' --exclude '**/*.Designer.cs'
```

Quote the pattern. Without quotes your shell expands it before the tool sees it, and
`samples/**` turns into `samples/Patients`, which matches nothing. A pattern that cannot
be parsed is a usage error and exits 2, rather than being skipped silently. The summary
always prints how many files were excluded, so you can tell a working pattern from a typo.

`bin`, `obj`, `.git` and `node_modules` are never scanned at all, so you do not need a
pattern for them. They are not counted in the excluded number either.

CI runs `check` on this repository itself, right after the tests, so the tool has to pass
its own rules:

```bash
dotnet run --project src/Sievert.Cli -- check .
```

There is no flag on that line because this repository has its own `sievert.json` at the
root, and the tool reads it the same way it would read yours. `samples/` is excluded there
because those files are broken on purpose - they are test data the test project reads.

## Configuration

`sievert.json` is optional. If there is one in the directory you are scanning it gets read;
if there isn't, every rule runs and nothing is excluded. `--config <path>` points at a
different file, and in that case the file has to exist. `--config` is resolved against your
current working directory, not against the directory being scanned.

```json
{
  "rules": [
    { "code": "SV001", "enabled": true, "severity": "warning" }
  ],
  "exclude": ["samples/**"]
}
```

The file only turns rules on and off and overrides how serious their findings are. What a
rule actually looks for stays in the code - see `docs/adr/0009-kural-katalogu-ve-yapilandirma.md`
for why. `severity` is applied before `--fail-on` is checked, so setting a rule to `info`
really does stop it from breaking the build.

Rules you don't mention keep running with their defaults, so adding a new rule later
doesn't get silently disabled by an old config file. A rule code the tool doesn't know, or
a property name it doesn't know, is an error and exits 2 - a typo shouldn't quietly turn a
rule off. `exclude` here and `--exclude` on the command line are added together; neither
replaces the other.

The summary prints which rules ran and which ones the config turned off, how many files
were excluded, and how many directories were never entered at all:

```
Ozet
  Taranan dosya  : 61
  Dislanan dosya : 8
  Atlanan klasor : 9
  Etkin kural    : SV001, SV002, SV003
  Kapali kural   : yok
  Bulgu          : 0
```

`--json` carries the same information, with the skipped directories listed by path.

Exit codes are the same for both commands:

| Code | Meaning |
|---|---|
| 0 | ran fine, nothing at or above the `--fail-on` level |
| 1 | ran fine, found something at or above that level |
| 2 | the tool could not run: path not found, bad flag, unexpected error |

## Roadmap

0. Project scaffolding, CI, ADRs — done
1. Roslyn syntax tree traversal: read a C# file as structure, not text — done
2. First detector: SV001 async void — done
3. Detector catalogue: 6 .NET-specific defect patterns behind an IRule
   plugin interface, configured from JSON — 3 of 6 written
4. Git history mining with LibGit2Sharp: churn, ownership, past fixes,
   simplified SZZ labelling, stored in PostgreSQL via EF Core
5. Risk engine: weighted baseline, then ML.NET classifier, then
   calibration (Brier, ECE, reliability diagram) — the research core
6. ASP.NET Core Web API + Blazor dashboard
7. GitHub Action bot that comments risk on pull requests
8. Test suite, Docker Compose, evaluation on real open-source C# repos

## Status

Early development, v0.1.0. Stages 0, 1 and 2 are done. The project builds with CI, Roslyn
parsing reads a file as structure, and the first rule SV001 runs behind the `check`
command. SV001 was measured on two real repositories (Polly and ShareX) and a 20-line
sample of its output was checked by hand: precision came out 8/10, with two false
positives and one false negative that all trace back to the same cause. The numbers and
what I plan to do about them are in `docs/raporlar/asama2-kapanis.md`. Stage 3 has
started: SV001 now also treats `+= MethodName` in the same file or the same partial class
as evidence that a method is an event handler (ADR 0008), there is an `--exclude` flag, CI
checks this repository with it, and rules live in a catalogue that an optional
`sievert.json` can turn on and off (ADR 0009). There are three rules now: SV001, SV002 and
SV003. SV002 and SV003 have not been measured on a real repository yet - SV001 is the only
one with numbers behind it. Nothing from stage 4 onwards (git history, risk scoring, API,
dashboard) has been written.
