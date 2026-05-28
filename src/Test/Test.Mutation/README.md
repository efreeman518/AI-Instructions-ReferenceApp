# Mutation Testing

This project is a focused Stryker.NET run target for TaskFlow domain logic. It demonstrates mutation testing against `TaskFlow.Domain.Model` without running the full solution test suite.

## Run

From repo root:

```powershell
rtk dotnet tool restore
rtk dotnet test src/Test/Test.Mutation/Test.Mutation.csproj
```

Then run Stryker from `src/Test/Test.Mutation`:

```powershell
rtk dotnet tool run dotnet-stryker
```

Stryker writes the HTML report under `src/Test/Test.Mutation/StrykerOutput/`.

## Scope

`stryker-config.json` mutates only:

- `TaskItem.cs`
- `TaskItemStatusTransitionRule.cs`

The sample tests assert domain boundaries, status transition rules, idempotent collection behavior, and failure messages. These are useful mutation-testing examples because weak assertions usually let comparison, boolean, collection, and string mutants survive.

References:

- https://stryker-mutator.io/docs/stryker-net/introduction/
- https://stryker-mutator.io/docs/stryker-net/getting-started/
- https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
