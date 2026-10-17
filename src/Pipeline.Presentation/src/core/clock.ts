export interface Clock {
  now(): number;
  toISOString(): string;
}

export const systemClock: Clock = {
  now: () => Date.now(),
  toISOString: () => new Date().toISOString(),
};
