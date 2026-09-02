import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useRef, useState } from "react";
import { userFacingError } from "@/shared/api/errors";
import {
  applyJobPipelineEntry,
  deleteAllJobPipelineEntries,
  forwardJobPipelineEntry,
  jobPipelineQueryKey,
} from "@/shared/api/pipeline";
import type { JobPipelineBoard, JobPipelineUpdateResult } from "@/shared/types/pipeline";
import { profileTitle, sourceLabel } from "@/features/jobApplication/pipelineUi";
import { refreshJobApplicationWorkspace } from "@/features/jobApplication/refreshWorkspace";

export type PipelineBulkAction = "update" | "forward" | "delete";

export type PipelineBulkSession = {
  action: PipelineBulkAction;
  phase: "running" | "done";
  current: number;
  total: number;
  message: string;
  detail: string;
};

function entryLabel(board: JobPipelineBoard, entry: JobPipelineBoard["entries"][number]): string {
  return `${profileTitle(board, entry.profileId)} · ${sourceLabel(
    board,
    entry.sourceId,
    entry.locationSheetId,
    entry.locationName,
  )}`;
}

export function usePipelineBulkRun() {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<PipelineBulkSession | null>(null);
  const [error, setError] = useState<string | null>(null);
  const busyRef = useRef(false);

  const finish = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: jobPipelineQueryKey });
    await refreshJobApplicationWorkspace(queryClient);
    busyRef.current = false;
  }, [queryClient]);

  const startUpdateAll = useCallback(
    async (board: JobPipelineBoard): Promise<JobPipelineUpdateResult | null> => {
      if (busyRef.current) {
        return null;
      }

      busyRef.current = true;
      setError(null);
      const entries = board.entries;
      const total = entries.length;
      const totals: JobPipelineUpdateResult = { added: 0, skipped: 0, banned: 0 };

      setSession({
        action: "update",
        phase: "running",
        current: 0,
        total,
        message: "Updating all pipeline rows…",
        detail: "",
      });

      try {
        for (let index = 0; index < entries.length; index += 1) {
          const entry = entries[index];
          setSession({
            action: "update",
            phase: "running",
            current: index,
            total,
            message: `Updating row ${index + 1} of ${total}`,
            detail: entryLabel(board, entry),
          });

          const result = await applyJobPipelineEntry(entry.id);
          totals.added += result.added;
          totals.skipped += result.skipped;
          totals.banned += result.banned;
        }

        setSession({
          action: "update",
          phase: "done",
          current: total,
          total,
          message: "Update All finished",
          detail: "",
        });
        return totals;
      } catch (caught) {
        setError(userFacingError(caught));
        setSession(null);
        return null;
      } finally {
        await finish();
        window.setTimeout(() => setSession(null), 1200);
      }
    },
    [finish],
  );

  const startForwardAll = useCallback(
    async (board: JobPipelineBoard): Promise<number | null> => {
      if (busyRef.current) {
        return null;
      }

      busyRef.current = true;
      setError(null);
      const targets: JobPipelineBoard["entries"] = [];
      const seenProfiles = new Set<string>();
      for (const entry of board.entries) {
        if (seenProfiles.has(entry.profileId)) {
          continue;
        }

        seenProfiles.add(entry.profileId);
        targets.push(entry);
      }

      const total = targets.length;
      setSession({
        action: "forward",
        phase: "running",
        current: 0,
        total,
        message: "Forwarding all profile sheets…",
        detail: "",
      });

      try {
        for (let index = 0; index < targets.length; index += 1) {
          const entry = targets[index];
          setSession({
            action: "forward",
            phase: "running",
            current: index,
            total,
            message: `Forwarding profile ${index + 1} of ${total}`,
            detail: profileTitle(board, entry.profileId),
          });

          await forwardJobPipelineEntry(entry.id);
        }

        setSession({
          action: "forward",
          phase: "done",
          current: total,
          total,
          message: "Forward All finished",
          detail: "",
        });
        return total;
      } catch (caught) {
        setError(userFacingError(caught));
        setSession(null);
        return null;
      } finally {
        await finish();
        window.setTimeout(() => setSession(null), 1200);
      }
    },
    [finish],
  );

  const startDeleteAll = useCallback(
    async (entryCount: number): Promise<boolean> => {
      if (busyRef.current) {
        return false;
      }

      busyRef.current = true;
      setError(null);
      setSession({
        action: "delete",
        phase: "running",
        current: 0,
        total: 1,
        message: "Removing all pipeline entries…",
        detail:
          entryCount === 1
            ? "Deleting 1 pipeline row"
            : `Deleting ${entryCount} pipeline rows`,
      });

      try {
        await deleteAllJobPipelineEntries();
        setSession({
          action: "delete",
          phase: "done",
          current: 1,
          total: 1,
          message: "Delete All finished",
          detail: "",
        });
        return true;
      } catch (caught) {
        setError(userFacingError(caught));
        setSession(null);
        return false;
      } finally {
        await finish();
        window.setTimeout(() => setSession(null), 1200);
      }
    },
    [finish],
  );

  const running = session?.phase === "running";

  return {
    session,
    error,
    running,
    startUpdateAll,
    startForwardAll,
    startDeleteAll,
    clearError: () => setError(null),
  };
}
