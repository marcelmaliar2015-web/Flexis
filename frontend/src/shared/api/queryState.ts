export function isQueryLoading(data: unknown, isPending: boolean): boolean {
  return isPending && data === undefined;
}

export function queryCount(data: unknown, isPending: boolean, count: number | undefined): number | null {
  if (isQueryLoading(data, isPending)) {
    return null;
  }

  return count ?? 0;
}
