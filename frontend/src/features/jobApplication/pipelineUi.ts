import { ApiError } from "@/shared/api/client";
import type { JobPipelineBoard, JobPipelineForwardResult, JobPipelineUpdateResult } from "@/shared/types/pipeline";

export function listingsNotice(result: JobPipelineUpdateResult, all: boolean): string {
  if (result.added === 0 && result.skipped === 0 && result.banned === 0) {
    return all
      ? "No listings to add from those source locations."
      : "No listings to add from that source location.";
  }
  const parts: string[] = [];
  if (result.added > 0) {
    parts.push(`Added ${result.added}.`);
  } else if (result.skipped > 0 || result.banned > 0) {
    parts.push(all ? "No new listings on the profile sheets." : "No new listings on this profile.");
  }
  if (result.skipped > 0) {
    parts.push(`Skipped ${result.skipped} already on this profile.`);
  }
  if (result.banned > 0) {
    parts.push(`Skipped ${result.banned} banned.`);
  }
  return parts.join(" ");
}

export function forwardNotice(result: JobPipelineForwardResult): string {
  return `Archived the current sheet as ${result.archivedSheetName}. New empty ${result.mainSheetName} tab.`;
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Request failed.";
}

export function sourceChoiceValue(sourceId: string, sheetId: number): string {
  return `${sourceId}:${sheetId}`;
}

export function parseSourceChoice(value: string): { sourceId: string; locationSheetId: number } | null {
  const separator = value.lastIndexOf(":");
  if (separator <= 0 || separator === value.length - 1) {
    return null;
  }

  const locationSheetId = Number(value.slice(separator + 1));
  if (!Number.isInteger(locationSheetId)) {
    return null;
  }

  return { sourceId: value.slice(0, separator), locationSheetId };
}

export function sourceChoicesFromBoard(board: JobPipelineBoard): { value: string; label: string }[] {
  return board.sources.flatMap((source) =>
    source.locations.map((location) => ({
      value: sourceChoiceValue(source.id, location.sheetId),
      label: `${source.title} · ${location.name}`,
    })),
  );
}

export function profileTitle(board: JobPipelineBoard, profileId: string): string {
  return board.profiles.find((profile) => profile.id === profileId)?.title ?? profileId;
}

export function sourceLabel(board: JobPipelineBoard, sourceId: string, locationSheetId: number, locationName: string): string {
  const source = board.sources.find((item) => item.id === sourceId);
  if (!source) {
    return locationName;
  }

  const location = source.locations.find((item) => item.sheetId === locationSheetId);
  return `${source.title} · ${location?.name ?? locationName}`;
}
