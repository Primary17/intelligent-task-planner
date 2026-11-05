import type { DayName } from '../api/types';

export const DAYS: { key: DayName; label: string; short: string }[] = [
  { key: 'Monday', label: 'Понеділок', short: 'Пн' },
  { key: 'Tuesday', label: 'Вівторок', short: 'Вт' },
  { key: 'Wednesday', label: 'Середа', short: 'Ср' },
  { key: 'Thursday', label: 'Четвер', short: 'Чт' },
  { key: 'Friday', label: 'Пʼятниця', short: 'Пт' },
  { key: 'Saturday', label: 'Субота', short: 'Сб' },
  { key: 'Sunday', label: 'Неділя', short: 'Нд' },
];

const pad = (n: number) => String(n).padStart(2, '0');

export function toDateKey(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function parseDateKey(key: string): Date {
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function addDays(date: Date, days: number): Date {
  const copy = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  copy.setDate(copy.getDate() + days);
  return copy;
}

export function startOfWeek(date: Date): Date {
  const offset = (date.getDay() + 6) % 7;
  return addDays(date, -offset);
}

export function dayNameOf(date: Date): DayName {
  return DAYS[(date.getDay() + 6) % 7].key;
}

export function timeToMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

export const shortTime = (time: string) => time.slice(0, 5);

export const toApiTime = (time: string) => (time.length === 5 ? `${time}:00` : time);

export function parseUtc(iso: string): Date {
  const hasZone = /(Z|[+-]\d{2}:?\d{2})$/.test(iso);
  return new Date(hasZone ? iso : `${iso}Z`);
}

export function toLocalInput(iso: string): string {
  const d = parseUtc(iso);
  return `${toDateKey(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export const fromLocalInput = (value: string): string => new Date(value).toISOString();

export function defaultDeadlineInput(daysAhead = 7): string {
  const d = addDays(new Date(), daysAhead);
  return `${toDateKey(d)}T23:59`;
}

export function formatDateTime(iso: string): string {
  return parseUtc(iso).toLocaleString('uk-UA', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

export function formatDayMonth(date: Date): string {
  return date.toLocaleDateString('uk-UA', { day: 'numeric', month: 'long' });
}

export function formatDateKey(key: string): string {
  return parseDateKey(key).toLocaleDateString('uk-UA', {
    weekday: 'short', day: 'numeric', month: 'long', year: 'numeric',
  });
}

export function formatMinutes(total: number): string {
  const hours = Math.floor(total / 60);
  const minutes = total % 60;
  if (hours === 0) return `${minutes} хв`;
  return minutes === 0 ? `${hours} год` : `${hours} год ${minutes} хв`;
}

export const hoursToMinutes = (hours: number) => Math.round(hours * 60);
export const minutesToHours = (minutes: number) => Math.round((minutes / 60) * 100) / 100;
