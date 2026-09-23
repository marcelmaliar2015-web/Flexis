const COMMON_TIME_ZONE_IDS = [
  "UTC",
  "America/New_York",
  "America/Chicago",
  "America/Denver",
  "America/Los_Angeles",
  "America/Phoenix",
  "America/Toronto",
  "America/Vancouver",
  "America/Mexico_City",
  "America/Sao_Paulo",
  "Europe/London",
  "Europe/Paris",
  "Europe/Berlin",
  "Europe/Madrid",
  "Europe/Amsterdam",
  "Europe/Zurich",
  "Asia/Dubai",
  "Asia/Kolkata",
  "Asia/Singapore",
  "Asia/Shanghai",
  "Asia/Tokyo",
  "Asia/Seoul",
  "Australia/Sydney",
  "Pacific/Auckland",
] as const;

export type TimeZoneOption = {
  id: string;
  label: string;
  group: "Suggested" | "All";
};

function listSupportedTimeZoneIds(): string[] {
  const supported = Intl.supportedValuesOf?.("timeZone");
  if (supported && supported.length > 0) {
    return [...supported];
  }

  return [...COMMON_TIME_ZONE_IDS];
}

export function browserTimeZoneId(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

export function resolveTimeZoneId(preferred: string | null | undefined): string {
  const trimmed = preferred?.trim() ?? "";
  if (trimmed.length > 0 && isValidTimeZoneId(trimmed)) {
    return trimmed;
  }

  return browserTimeZoneId();
}

export function isValidTimeZoneId(timeZoneId: string): boolean {
  try {
    Intl.DateTimeFormat(undefined, { timeZone: timeZoneId }).format(new Date());
    return true;
  } catch {
    return false;
  }
}

export function formatTimeZoneOffset(timeZoneId: string, at: Date = new Date()): string {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: timeZoneId,
    timeZoneName: "shortOffset",
  }).formatToParts(at);
  const offset = parts.find((part) => part.type === "timeZoneName")?.value ?? "GMT";
  return offset.replace("GMT", "UTC");
}

export function formatTimeZoneAbbr(timeZoneId: string, at: Date = new Date()): string {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: timeZoneId,
    timeZoneName: "short",
  }).formatToParts(at);
  return parts.find((part) => part.type === "timeZoneName")?.value ?? timeZoneId;
}

export function formatTimeZoneCity(timeZoneId: string): string {
  if (timeZoneId === "UTC") {
    return "UTC";
  }

  const slash = timeZoneId.lastIndexOf("/");
  const raw = slash >= 0 ? timeZoneId.slice(slash + 1) : timeZoneId;
  return raw.replaceAll("_", " ");
}

export function formatTimeZoneLabel(timeZoneId: string, at: Date = new Date()): string {
  const city = formatTimeZoneCity(timeZoneId);
  const offset = formatTimeZoneOffset(timeZoneId, at);
  return `${city} (${offset})`;
}

export function listTimeZoneOptions(at: Date = new Date()): TimeZoneOption[] {
  const allIds = listSupportedTimeZoneIds();
  const commonSet = new Set<string>(COMMON_TIME_ZONE_IDS);
  const suggested = COMMON_TIME_ZONE_IDS.filter((id) => allIds.includes(id) || id === "UTC").map(
    (id) =>
      ({
        id,
        label: formatTimeZoneLabel(id, at),
        group: "Suggested" as const,
      }) satisfies TimeZoneOption,
  );

  const rest = allIds
    .filter((id) => !commonSet.has(id))
    .map(
      (id) =>
        ({
          id,
          label: formatTimeZoneLabel(id, at),
          group: "All" as const,
        }) satisfies TimeZoneOption,
    )
    .sort((a, b) => a.label.localeCompare(b.label));

  return [...suggested, ...rest];
}
