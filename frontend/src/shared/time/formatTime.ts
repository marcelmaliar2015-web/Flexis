export function formatDateTime(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(toDate(value));
}

export function formatDateTimeSeconds(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
  }).format(toDate(value));
}

export function formatDateMedium(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    dateStyle: "medium",
  }).format(toDate(value));
}

export function formatDateTimeMedium(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    dateStyle: "medium",
    timeStyle: "short",
  }).format(toDate(value));
}

export function formatClockTime(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
  }).format(toDate(value));
}

export function formatClockTimeShort(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    hour: "numeric",
    minute: "2-digit",
  }).format(toDate(value));
}

export function formatWeekdayShort(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    weekday: "short",
  }).format(toDate(value));
}

export function formatHourLabel(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    month: "short",
    day: "numeric",
    hour: "numeric",
  }).format(toDate(value));
}

export function formatDayLabel(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    month: "short",
    day: "numeric",
  }).format(toDate(value));
}

export function formatMonthLabel(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    month: "short",
    year: "numeric",
  }).format(toDate(value));
}

export function formatFullDate(value: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    weekday: "long",
    month: "long",
    day: "numeric",
    year: "numeric",
  }).format(toDate(value));
}

function toDate(value: string | number | Date): Date {
  return value instanceof Date ? value : new Date(value);
}
