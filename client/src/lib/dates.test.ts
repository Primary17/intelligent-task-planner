import { describe, expect, it } from 'vitest';
import { buildQuery } from '../api/endpoints';
import { problemToMessage } from '../api/http';
import {
  addDays, dayNameOf, formatMinutes, hoursToMinutes, parseUtc, startOfWeek, timeToMinutes, toApiTime, toDateKey,
} from './dates';

describe('dates', () => {
  it('starts the week on Monday', () => {
    const sunday = new Date(2030, 0, 13);
    expect(toDateKey(startOfWeek(sunday))).toBe('2030-01-07');
    expect(toDateKey(startOfWeek(new Date(2030, 0, 7)))).toBe('2030-01-07');
  });

  it('maps JS weekday to the API day name', () => {
    expect(dayNameOf(new Date(2030, 0, 7))).toBe('Monday');
    expect(dayNameOf(new Date(2030, 0, 13))).toBe('Sunday');
  });

  it('adds days across month boundaries without mutating the input', () => {
    const start = new Date(2030, 0, 30);
    expect(toDateKey(addDays(start, 3))).toBe('2030-02-02');
    expect(toDateKey(start)).toBe('2030-01-30');
  });

  it('treats timestamps without a zone as UTC', () => {
    expect(parseUtc('2030-01-07T12:00:00').toISOString()).toBe('2030-01-07T12:00:00.000Z');
    expect(parseUtc('2030-01-07T12:00:00Z').toISOString()).toBe('2030-01-07T12:00:00.000Z');
    expect(parseUtc('2030-01-07T14:00:00+02:00').toISOString()).toBe('2030-01-07T12:00:00.000Z');
  });

  it('converts times', () => {
    expect(timeToMinutes('17:30:00')).toBe(1050);
    expect(toApiTime('17:30')).toBe('17:30:00');
    expect(toApiTime('17:30:00')).toBe('17:30:00');
  });

  it('formats durations', () => {
    expect(formatMinutes(45)).toBe('45 хв');
    expect(formatMinutes(120)).toBe('2 год');
    expect(formatMinutes(150)).toBe('2 год 30 хв');
    expect(hoursToMinutes(1.25)).toBe(75);
  });
});

describe('api helpers', () => {
  it('joins validation errors into one message', () => {
    expect(problemToMessage({ errors: { Title: ['Required.'], Color: ['Bad color.'] } }, 'x')).toBe('Required. Bad color.');
  });

  it('prefers detail over title and falls back when empty', () => {
    expect(problemToMessage({ title: 'Business validation failed', detail: 'Name taken.' }, 'x')).toBe('Name taken.');
    expect(problemToMessage(null, 'fallback')).toBe('fallback');
  });

  it('builds query strings and skips empty values', () => {
    expect(buildQuery({ a: '1', b: undefined, c: '' })).toBe('?a=1');
    expect(buildQuery({})).toBe('');
  });
});
