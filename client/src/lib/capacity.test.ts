import { describe, expect, it } from 'vitest';
import type { CapacityReport, TaskCapacity } from '../api/types';
import { hasCapacityProblems, summarizeCapacity } from './capacity';

const row = (title: string, status: TaskCapacity['status'], shortfallMinutes = 0): TaskCapacity => ({
  taskId: title,
  title,
  disciplineId: 'd',
  disciplineName: 'OS',
  deadlineUtc: '2030-01-08T10:00:00Z',
  remainingMinutes: 60,
  plannedMinutes: 0,
  unplannedMinutes: 60,
  availableMinutes: 0,
  requiredMinutes: 60,
  shortfallMinutes,
  status,
});

describe('summarizeCapacity', () => {
  it('reports nothing for an empty or missing report', () => {
    expect(hasCapacityProblems(summarizeCapacity(null))).toBe(false);
    expect(hasCapacityProblems(summarizeCapacity({ generatedAtUtc: '', totalShortfallMinutes: 0, tasks: [] }))).toBe(false);
  });

  it('splits shortfall and overdue tasks and keeps the first shortfall', () => {
    const report: CapacityReport = {
      generatedAtUtc: '',
      totalShortfallMinutes: 120,
      tasks: [row('Old', 'Overdue', 60), row('A', 'Ok'), row('B', 'Shortfall', 90), row('C', 'Shortfall', 120)],
    };

    const summary = summarizeCapacity(report);

    expect(summary.overdue.map((t) => t.title)).toEqual(['Old']);
    expect(summary.shortfallTasks.map((t) => t.title)).toEqual(['B', 'C']);
    expect(summary.firstShortfall?.title).toBe('B');
    expect(summary.shortfallMinutes).toBe(120);
    expect(hasCapacityProblems(summary)).toBe(true);
  });
});
