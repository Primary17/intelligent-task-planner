import { describe, expect, it } from 'vitest';
import { spanOnDay } from './sessions';

const local = (y: number, m: number, d: number, h: number, min = 0) =>
  new Date(y, m - 1, d, h, min).toISOString();

describe('spanOnDay', () => {
  const day = new Date(2030, 0, 7);

  it('returns minutes from local midnight', () => {
    expect(spanOnDay(local(2030, 1, 7, 9), local(2030, 1, 7, 10, 30), day)).toEqual({ start: 540, end: 630 });
  });

  it('returns null when the session is on another day', () => {
    expect(spanOnDay(local(2030, 1, 8, 9), local(2030, 1, 8, 10), day)).toBeNull();
  });

  it('clips a session that crosses midnight to each day', () => {
    const start = local(2030, 1, 7, 23);
    const end = local(2030, 1, 8, 1);
    expect(spanOnDay(start, end, day)).toEqual({ start: 1380, end: 1440 });
    expect(spanOnDay(start, end, new Date(2030, 0, 8))).toEqual({ start: 0, end: 60 });
  });
});
