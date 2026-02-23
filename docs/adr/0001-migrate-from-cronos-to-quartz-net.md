# ADR 0001: Migrate from Cronos to Quartz.NET

**Date**: 2026-02-22

**Status**: Accepted

## Context

CRONCH! uses [Cronos](https://github.com/HangfireIO/Cronos) to parse cron expressions and a hand-rolled scheduling loop (a single background `Thread` polling every 300ms) to trigger job executions. While functional, this custom scheduler carries maintenance burden and lacks the maturity of a purpose-built scheduling library. [Quartz.NET](https://www.quartz-scheduler.net/) is a well-established .NET scheduling engine that can replace both the cron parsing and the scheduling loop.

The existing execution pipeline uses **raw OS threads** (not the .NET ThreadPool): `JobExecutionService` spawns one `Thread` per running execution, each blocking on a child process, tracked in a `ConcurrentDictionary<ExecId, Thread>`. This model must be preserved — there is no ThreadPool starvation risk today, and refactoring toward async/await is out of scope.

### Current Threading Model

```
┌─────────────────────────────────────────────────────┐
│  JobSchedulingService                               │
│  1 dedicated Thread (IsBackground = true)           │
│  Polls every 300ms via Thread.Sleep                 │
│  Uses Cronos to compute next occurrences            │
│  Fires jobs by calling ExecuteJob()                 │
└──────────────────────┬──────────────────────────────┘
                       │ ExecuteJob()
                       ▼
┌─────────────────────────────────────────────────────┐
│  JobExecutionService                                │
│  1 dedicated Thread per running execution           │
│  Spawned via new Thread(() => PerformExecution())   │
│  Each thread blocks on the child process            │
│  Tracked in ConcurrentDictionary<ExecId, Thread>    │
└─────────────────────────────────────────────────────┘
```

## Decision

Replace Cronos and the custom scheduling loop with Quartz.NET, using a thin `IJob` bridge that preserves the existing execution model.

### Scheduling Architecture

Quartz.NET introduces its own internal thread pool but the execution threads remain unchanged:

```
┌─────────────────────────────────────────────────────┐
│  Quartz Scheduler (hosted service)                  │
│  Internal DefaultThreadPool (default: 10 threads)   │
│  Manages triggers, fires IJob.Execute() on pool thd │
└──────────────────────┬──────────────────────────────┘
                       │ IJob.Execute()
                       ▼
┌─────────────────────────────────────────────────────┐
│  CronchQuartzJob : IJob                             │
│  Thin fire-and-forget dispatcher                    │
│  Calls ExecuteJob() and returns immediately (<1ms)  │
│  Quartz pool thread is released instantly            │
└──────────────────────┬──────────────────────────────┘
                       │ ExecuteJob()
                       ▼
┌─────────────────────────────────────────────────────┐
│  JobExecutionService (UNCHANGED)                    │
│  1 dedicated Thread per running execution           │
│  Spawned via new Thread(() => PerformExecution())   │
│  Each thread blocks on the child process            │
│  Tracked in ConcurrentDictionary<ExecId, Thread>    │
└─────────────────────────────────────────────────────┘
```

`CronchQuartzJob.Execute()` does nothing except call `JobExecutionService.ExecuteJob()`, which spawns a raw `Thread` and returns. The Quartz pool thread is occupied for less than a millisecond. With the default pool size of 10, this can dispatch hundreds of jobs per second — far beyond any realistic scheduling load. The pool size is configurable if needed:

```csharp
q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 25);
```

### Job Store: RAMJobStore (In-Memory)

The source of truth for job definitions is `config.xml`, not Quartz. On each startup, triggers are rebuilt from config. Quartz acts purely as a timer, so a persistent job store (SQL, etc.) is unnecessary.

### Concurrency Control: Stays in JobExecutionService

Quartz only offers `[DisallowConcurrentExecution]` — a binary on/off flag per job type. CRONCH! supports a configurable `Parallelism` count per job with a `MarkParallelSkipAs` policy (ignore, indeterminate, warning, error). This logic lives in `JobExecutionService.ExecuteJob()` and is untouched by the migration. The `IJob` bridge does not participate in concurrency control.

### Async Model: Fire-and-Forget

The `IJob.Execute()` bridge is synchronous fire-and-forget. It dispatches to `ExecuteJob()` (which spawns a raw `Thread`) and returns immediately. The existing raw-Thread model in `JobExecutionService` is preserved as-is.

### CronExpressionDescriptor: Retained

[CronExpressionDescriptor](https://github.com/bradymholt/cron-expression-descriptor) is kept for human-readable cron descriptions in the UI. It is compatible with Quartz.NET cron syntax but must be configured for Quartz's day-of-week convention (1=SUN instead of 0=SUN).

### Cron Expression Format Migration

Cronos and Quartz both use 7-field cron expressions (with seconds), but differ in key ways:

| Aspect | Cronos | Quartz |
|--------|--------|--------|
| Day-of-week values | 0–6 (SUN–SAT) | 1–7 (SUN–SAT) |
| Day-of-week/month wildcard | Both can be `*` | One must be `?` when the other is specified |
| Year field | Optional 7th field | Optional 7th field |

A migration utility converts stored Cronos-format expressions to Quartz-format on startup.

### Config Namespace Versioning (v1 → v2)

The configuration XML uses a versioned namespace to distinguish expression formats:
- **v1**: `urn:indubitable-software:cronch:v1` (Cronos cron expressions)
- **v2**: `urn:indubitable-software:cronch:v2` (Quartz cron expressions)

Migration behavior on startup:

1. Attempt to load config as v2. If successful, proceed normally.
2. If no config exists, proceed with empty config (new install).
3. Attempt to load config as v1. If successful:
   a. Convert all `CronSchedule` values from Cronos format to Quartz format (best-effort).
   b. Rewrite the config file as v2.
   c. Proceed normally.
4. If neither version loads, fail with an error.

This is a **one-way upgrade** with no dual-version runtime support and no downgrade path.

## Consequences

### Positive

- Eliminates custom scheduling loop (polling thread, SortedSet, lock flags) in favor of a mature, well-tested scheduler.
- Quartz's trigger model provides reliable next-fire-time computation via `ITrigger.GetNextFireTimeUtc()`.
- The execution pipeline (`JobExecutionService`, `ExecutionEngine`) is completely unchanged — no risk to the core execution model.
- Quartz's hosted service integrates cleanly with .NET generic host lifecycle, removing manual `StartSchedulingRuns()` / `StopSchedulingRuns()` calls.

### Negative

- Cron expression format change requires a one-time migration of stored config. Users should back up `config.xml` before upgrading.
- Quartz's `?` wildcard requirement (day-of-week vs day-of-month) is less intuitive than Cronos's `*`/`*` — UI hints must be updated.
- Adds a dependency on Quartz.NET (~3 packages) where previously only the lightweight Cronos was needed.


