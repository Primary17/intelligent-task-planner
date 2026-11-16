import { describe, expect, it } from 'vitest';
import type { GeneratedSession, ReplanScheduleResponse } from '../api/types';
import { addDays } from './dates';
import { MAX_PLANNING_DAYS, planningHorizon, scheduleFits, summarizeGenerate, summarizeReplan, weekHorizon, type Names } from './schedule';

const names: Names = {
  taskTitle: (id) => ({ a: 'Lab 1', b: 'Essay' }[id]),
  sessionTitle: (id) => ({ s1: 'Lab 1', s2: 'Essay' }[id]),
};

const session = (id: string, minutes: number): GeneratedSession => ({
  id,
  studyTaskId: 'a',
  startAtUtc: '2030-01-07T08:00:00Z',
  endAtUtc: '2030-01-07T09:00:00Z',
  plannedMinutes: minutes,
  status: 'Planned',
  isLocked: false,
});

describe('weekHorizon', () => {
  const monday = new Date(2030, 0, 7);

  it('covers the whole week when it is in the future', () => {
    const h = weekHorizon(monday, new Date(2030, 0, 1, 12));
    expect(h).toEqual({ from: monday.toISOString(), to: new Date(2030, 0, 14).toISOString() });
  });

  it('starts at now for the current week', () => {
    const now = new Date(2030, 0, 9, 15, 30);
    expect(weekHorizon(monday, now)).toEqual({ from: now.toISOString(), to: new Date(2030, 0, 14).toISOString() });
  });

  it('returns null for a week that is already over', () => {
    expect(weekHorizon(monday, new Date(2030, 0, 14, 0, 0))).toBeNull();
    expect(weekHorizon(monday, new Date(2030, 0, 20))).toBeNull();
  });
});

describe('planningHorizon', () => {
  const monday = new Date(2030, 0, 7);

  it('extends through the latest open-task deadline', () => {
    const h = planningHorizon(monday, new Date(2030, 0, 1, 12), [
      '2030-01-10T18:00:00Z',
      '2030-01-28T20:00:00Z',
    ]);
    expect(h?.to).toBe(new Date('2030-01-28T20:00:00Z').toISOString());
  });

  it('caps the range at MAX_PLANNING_DAYS from the start', () => {
    const now = new Date(2030, 0, 1, 12);
    const far = new Date(2030, 6, 1).toISOString();
    const h = planningHorizon(monday, now, [far]);
    expect(h?.to).toBe(addDays(monday, MAX_PLANNING_DAYS).toISOString());
  });
});

describe('summarizeGenerate', () => {
  it('counts created sessions and minutes', () => {
    const summary = summarizeGenerate(
      { from: '', to: '', sessions: [session('x', 60), session('y', 90)], unscheduled: [], conflicts: [] },
      names,
    );
    expect(summary.createdCount).toBe(2);
    expect(summary.createdMinutes).toBe(150);
    expect(scheduleFits(summary)).toBe(true);
  });

  it('reports the missing minutes per task, largest first', () => {
    const summary = summarizeGenerate(
      {
        from: '', to: '', sessions: [],
        unscheduled: [
          { studyTaskId: 'a', remainingMinutes: 180, scheduledMinutes: 120 },
          { studyTaskId: 'b', remainingMinutes: 300, scheduledMinutes: 0 },
          { studyTaskId: 'zzz', remainingMinutes: 60, scheduledMinutes: 60 },
        ],
        conflicts: [],
      },
      names,
    );
    expect(summary.unscheduled.map((u) => [u.title, u.missingMinutes])).toEqual([['Essay', 300], ['Lab 1', 60]]);
    expect(scheduleFits(summary)).toBe(false);
  });

  it('falls back to a generic name for unknown tasks', () => {
    const summary = summarizeGenerate(
      { from: '', to: '', sessions: [], unscheduled: [{ studyTaskId: 'q', remainingMinutes: 30, scheduledMinutes: 0 }], conflicts: [] },
      names,
    );
    expect(summary.unscheduled[0].title).toBe('Невідоме завдання');
  });

  it('maps conflicts to session titles', () => {
    const summary = summarizeGenerate(
      {
        from: '', to: '', sessions: [], unscheduled: [],
        conflicts: [{ firstSessionId: 's1', secondSessionId: 's9', overlapStartUtc: 'a', overlapEndUtc: 'b' }],
      },
      names,
    );
    expect(summary.conflicts).toEqual([{ firstTitle: 'Lab 1', secondTitle: 'Сесія', startUtc: 'a', endUtc: 'b' }]);
    expect(scheduleFits(summary)).toBe(false);
  });
});

describe('summarizeReplan', () => {
  it('counts stayed, created and removed sessions', () => {
    const response: ReplanScheduleResponse = {
      from: '', to: '',
      stayed: [session('a', 30)],
      created: [session('b', 60), session('c', 45)],
      removed: [session('d', 60), session('e', 60), session('f', 60)],
      unscheduled: [],
      conflicts: [],
    };
    const summary = summarizeReplan(response, names);
    expect(summary).toMatchObject({ kind: 'replan', createdCount: 2, createdMinutes: 105, removedCount: 3, stayedCount: 1 });
  });
});
