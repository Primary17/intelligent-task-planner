# Scheduling rules

The planner (`SchedulePlanner`) decides where new study sessions go. It does not talk to HTTP or the database. Callers pass availability, tasks, and sessions that already exist.

## Sort order

Unfinished tasks with remaining minutes are placed in this order:

1. Earlier `DeadlineUtc`.
2. Higher `TaskPriority` (`Critical` before `Low`).
3. Higher complexity.
4. More remaining minutes.
5. Task id, so the order is stable.

A task with status `Completed` is skipped. An earlier deadline is placed before a later task even when the later task has a higher priority.

## Remaining work

For each task:

```
covered = frozen sessions of this task
remaining = max(0, EstimatedMinutes - max(ActualMinutes, covered))
```

A session is frozen when it is `Completed` or `IsLocked`. Completed coverage is `ActualMinutes` on the session, or `PlannedMinutes` when actual time is missing. A locked session that is not completed covers `PlannedMinutes`.

`ActualMinutes` on the task and frozen sessions are not added together. The larger of the two counts, so the same work is not subtracted twice.

`Planned` and `Missed` sessions that are not locked do not block time and do not reduce the remainder. The caller decides what to do with them before saving a new plan. `POST /api/schedule/generate` applies the rule in [Saving a plan](#saving-a-plan).

## Free time

The horizon is `[StartUtc, EndUtc)`. Weekly windows are wall-clock times in `ScheduleHorizon.TimeZone`. For each local date in the horizon:

- `Unavailable` removes that day.
- `CustomHours` replaces the weekly windows for that date.
- Otherwise the weekly windows for that weekday are used.

Intervals are converted to UTC, clipped to the horizon, and merged when they touch or overlap. Frozen sessions are then cut out of the free intervals. Overlapping frozen sessions are also returned as conflicts. The new plan never overlaps itself or a frozen session.

An invalid local time (a DST gap) is skipped. An ambiguous local time uses the earlier offset.

## Splitting and placement

Each task is filled greedily from the earliest free slot that starts before its deadline.

- A session is at most `MaxSessionMinutes` (default 120). A six-hour task becomes several blocks.
- A session is at least `MinSessionMinutes` (default 30), except when the leftover work itself is shorter and the slot can hold it.
- A slot shorter than the minimum is left for a later task.
- Nothing is placed at or after the deadline. The part of a slot after the deadline stays free.

If the free time runs out, the result lists that task under `Unscheduled` with `RemainingMinutes` and how many of them were `ScheduledMinutes`. Tasks that fit completely are omitted from that list. Frozen sessions are not copied into `Sessions`; they stay where they are.

## Saving a plan

`POST /api/schedule/generate` loads the signed-in user's windows, exceptions, unfinished tasks, and sessions, then writes the plan. Another user's tasks are never read or written. New rows have status `Planned` and `IsLocked` false.

The horizon is either `from` and `to` (UTC, half-open `[from, to)`, at most 92 days) or `days` (1–92), which means `[UtcNow, UtcNow + days)`. `timeZoneId` is the zone for weekly windows. Omit it to use UTC.

Idempotency inside the horizon: unlocked `Planned` sessions of unfinished tasks that overlap `[from, to)` are deleted and replaced. A second call with the same inputs does not add another copy of that work. `Completed`, `Missed`, and `IsLocked` sessions stay where they are. Unlocked `Planned` sessions that lie fully outside the horizon also stay, and their minutes are subtracted from the work still to place, so a later horizon does not schedule those minutes again.

The response lists the sessions created by this call, tasks that still have unplaced minutes (`unscheduled.remainingMinutes` is the work this call tried to place, `scheduledMinutes` is how much of it was written), and overlaps between frozen sessions (`conflicts`).
