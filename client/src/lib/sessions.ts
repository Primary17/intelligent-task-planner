import { addDays, parseUtc } from './dates';

export interface DaySpan {
  start: number;
  end: number;
}

export function spanOnDay(startIso: string, endIso: string, day: Date): DaySpan | null {
  const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate()).getTime();
  const dayEnd = addDays(day, 1).getTime();
  const start = Math.max(parseUtc(startIso).getTime(), dayStart);
  const end = Math.min(parseUtc(endIso).getTime(), dayEnd);
  if (end <= start) return null;
  return {
    start: Math.round((start - dayStart) / 60000),
    end: Math.round((end - dayStart) / 60000),
  };
}
