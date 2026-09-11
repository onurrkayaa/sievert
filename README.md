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
samples/Hastalar/             sample repositories to test against
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

Right now the CLI only prints a banner. There is nothing to analyse yet.

## Roadmap

- [x] **Stage 0** - Project skeleton: solution, projects, CI, docs
- [ ] **Stage 1** - Read git history with LibGit2Sharp
- [ ] **Stage 2** - Parse C# files with Roslyn and find bug patterns
- [ ] **Stage 3** - Turn both signals into one risk score with an explanation
- [ ] **Stage 4** - Store results in PostgreSQL with EF Core
- [ ] **Stage 5** - Train an ML.NET model on past bug fixes instead of fixed weights
- [ ] **Stage 6** - Better CLI output and report formats
- [ ] **Stage 7** - Run it in CI and comment the score on pull requests
- [ ] **Stage 8** - Documentation and a first release

## Status

Early development, v0.1.0. Stage 0 is done, which means the project builds and the tests
run - not that it does anything useful yet.
