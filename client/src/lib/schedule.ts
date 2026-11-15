import { addDays } from './dates';

export interface Horizon {
  from: string;
  to: string;
}

// The planner does not skip the past, so the horizon starts at "now" for the current week.
export function weekHorizon(weekStart: Date, now: Date): Horizon | null {
  const start = new Date(Math.max(weekStart.getTime(), now.getTime()));
  const end = addDays(weekStart, 7);
  return end > start ? { from: start.toISOString(), to: end.toISOString() } : null;
}

export const browserTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone || undefined;
